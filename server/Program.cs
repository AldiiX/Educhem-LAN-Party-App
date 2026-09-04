using dotenv.net;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Npgsql.NameTranslation;
using server.Data;
using server.Data.Entities;
using server.Data.Seeders;
using server.Hubs;
using server.Infrastructure;
using server.Infrastructure.HubRateLimiting;
using server.Services;
using server.Services.OAuth;
using StackExchange.Redis;
using Microsoft.IdentityModel.Tokens;
using System.Net;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Coravel;
using Microsoft.AspNetCore.RateLimiting;

namespace server;

public static class Program {

    public static WebApplication Application { get; private set; } = null!;
    public static IDictionary<string, string> ENV { get; private set; } = DotEnv.Read();

    #if DEBUG
        public static readonly bool DevelopmentMode = true;
    #else
        public static readonly bool DevelopmentMode = false;
    #endif

    public static async Task Main(string[] args) {
        ENV = DotEnv.Read();
        var builder = WebApplication.CreateBuilder(args);
		var jwtConfiguration = JwtAuthConfiguration.FromEnvironment(ENV.AsReadOnly());
		var frontendOrigin = GetFrontendOrigin();

        #if RELEASE
            builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning);
        #endif

        // pripojeni k redisu
        var rhost = ENV["REDIS_IP"];
        var rport = ENV["REDIS_PORT"];
        var rpassword = ENV["REDIS_PASSWORD"];

        var rawPrefix = (ENV.TryGetValue("REDIS_KEY_PREFIX", out var prefixVal) && !string.IsNullOrWhiteSpace(prefixVal)
            ? prefixVal
            : (ENV.TryGetValue("REDIS_PREFIX", out var legacyPrefix) && !string.IsNullOrWhiteSpace(legacyPrefix) ? legacyPrefix : "edulp")).Trim();
        var redisPrefix = rawPrefix.EndsWith(':') ? rawPrefix : $"{rawPrefix}:";

        var config = new ConfigurationOptions {
            EndPoints = { $"{rhost}:{rport}" },
            AbortOnConnectFail = false,
            ChannelPrefix = RedisChannel.Literal(redisPrefix)
        };

        if (rpassword != null!) {
            config.Password = rpassword;
        }

        if ((ENV.TryGetValue("REDIS_DATABASE", out var dbVal) || ENV.TryGetValue("REDIS_DB", out dbVal))
            && int.TryParse(dbVal, out var defaultDb) && defaultDb >= 0) {
            config.DefaultDatabase = defaultDb;
        }

        var redis = await ConnectionMultiplexer.ConnectAsync(config);
        builder.Services.AddSingleton<IConnectionMultiplexer>(redis);

        builder.Services.AddControllers();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddSingleton<HubRateLimitManager>();
        builder.Services.AddSingleton<HubRateLimitFilter>();
        builder.Services.AddSignalR(options => {
            options.KeepAliveInterval = TimeSpan.FromSeconds(10);
            options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
            options.HandshakeTimeout = TimeSpan.FromSeconds(15);
            options.AddFilter<HubRateLimitFilter>();
        });
        builder.Services.AddDataProtection()
            .PersistKeysToStackExchangeRedis(redis, $"{redisPrefix}DataProtection-Keys")
            .AddKeyManagementOptions(options => {
                options.XmlEncryptor = new DataProtectionKeyEncryptor(jwtConfiguration);
            })
            .SetApplicationName("EduchemLANPartyApp");

        builder.Services.AddSingleton<IDistributedCache>(sp =>
            new RedisCache(new RedisCacheOptions {
                ConfigurationOptions = ConfigurationOptions.Parse(redis.Configuration),
                InstanceName = $"{redisPrefix}cache:"
            })
        );

        builder.Services.AddDbContextPool<AppDbContext>(opt => {
            opt.UseNpgsql(
                $"Host={ENV["PSQL_DB_HOST"]};Port={ENV["PSQL_DB_PORT"]};Database={ENV["PSQL_DB_NAME"]};Username={ENV["PSQL_DB_USER"]};Password={ENV["PSQL_DB_PASSWORD"]}",
                o => o
                        .MapEnum<Gender>("AccountGender", "public", new NpgsqlNullNameTranslator())
                        .MapEnum<AccountType>("AccountType", "public", new NpgsqlNullNameTranslator())
            );
        });

        builder.Configuration
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

		builder.Services.AddSingleton(jwtConfiguration);
		builder.Services.AddAntiforgery(options => {
			options.HeaderName = "X-XSRF-TOKEN";
			options.Cookie.Name = AuthCookieNames.Antiforgery;
			options.Cookie.HttpOnly = true;
			options.Cookie.SameSite = SameSiteMode.Lax;
			options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
		});

        var authBuilder = builder.Services.AddAuthentication(options => {
			options.DefaultAuthenticateScheme = AuthSchemes.AccessToken;
			options.DefaultChallengeScheme = AuthSchemes.AccessToken;
        })
		.AddJwtBearer(AuthSchemes.AccessToken, options => {
			options.MapInboundClaims = false;
			options.TokenValidationParameters = new TokenValidationParameters {
				ValidateIssuer = true,
				ValidIssuer = JwtAuthConfiguration.Issuer,
				ValidateAudience = true,
				ValidAudience = JwtAuthConfiguration.Audience,
				ValidateLifetime = true,
				ValidateIssuerSigningKey = true,
				IssuerSigningKey = jwtConfiguration.SigningKey,
				ClockSkew = TimeSpan.FromSeconds(15),
				NameClaimType = ClaimTypes.NameIdentifier,
				RoleClaimType = ClaimTypes.Role,
			};
			options.Events = new JwtBearerEvents {
				OnMessageReceived = context => {
					var authorization = context.Request.Headers.Authorization.ToString();
					if (!authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)) {
						context.Token = context.Request.Cookies[AuthCookieNames.Access];
					}
					return Task.CompletedTask;
				},
			};
		})
        .AddCookie(AuthSchemes.ExternalCookie, options => {
            options.Cookie.Name = "educhemlanparty_external";
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            options.ExpireTimeSpan = TimeSpan.FromMinutes(10);
        });

        builder.Services.AddHttpClient("oauth-external", client => {
            client.Timeout = TimeSpan.FromSeconds(10);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("EduchemLANPartyApp/4.1");
        });

        builder.Services.AddOAuthPlatforms(authBuilder);
		builder.Services.AddAuthorization(options => {
			options.AddPolicy(AuthPolicies.Teacher, policy => policy
				.RequireAuthenticatedUser()
				.RequireAssertion(context => HasAccountType(context.User, AccountType.Teacher)));
			options.AddPolicy(AuthPolicies.TeacherOrg, policy => policy
				.RequireAuthenticatedUser()
				.RequireAssertion(context => HasAccountType(context.User, AccountType.TeacherOrg)));
			options.AddPolicy(AuthPolicies.Admin, policy => policy
				.RequireAuthenticatedUser()
				.RequireAssertion(context => HasAccountType(context.User, AccountType.Admin)));
			options.AddPolicy(AuthPolicies.SuperAdmin, policy => policy
				.RequireAuthenticatedUser()
				.RequireAssertion(context => HasAccountType(context.User, AccountType.SuperAdmin)));
		});
        builder.Services.AddMemoryCache();

        builder.Services.AddSingleton<AppCacheService>();
        builder.Services.AddScoped<IAuthService, AuthService>();
		builder.Services.AddQueue();
		builder.Services.AddRateLimiter(options => {
			options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
			options.AddPolicy("email-change", context => RateLimitPartition.GetFixedWindowLimiter(
				context.User.FindFirstValue("sub") ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
				_ => new FixedWindowRateLimiterOptions { PermitLimit = 30, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
			options.AddPolicy("auth-login", context => RateLimitPartition.GetFixedWindowLimiter(
				context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
				_ => new FixedWindowRateLimiterOptions { PermitLimit = 60, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
			options.AddPolicy("auth-forgot-password", context => RateLimitPartition.GetFixedWindowLimiter(
				context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
				_ => new FixedWindowRateLimiterOptions { PermitLimit = 30, Window = TimeSpan.FromMinutes(15), QueueLimit = 0 }));
			options.AddPolicy("auth-change-password", context => RateLimitPartition.GetFixedWindowLimiter(
				context.User.FindFirstValue("sub") ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
				_ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromHours(1), QueueLimit = 0 }));
		});
        builder.Services.AddScoped<ReservationCacheService>();
        builder.Services.AddScoped<IDbLoggerService, DbLoggerService>();
        builder.Services.AddScoped<IAppSettingsService, AppSettingsService>();

        builder.Services.AddCors(options => {
            options.AddDefaultPolicy(policy => {
				policy.WithOrigins(frontendOrigin)
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials();
            });
        });

        Application = builder.Build();
        
        await AppSettingsItemSeeder.SeedAsync(Application);
        
        using (var scope = Application.Services.CreateScope()) {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            
            Application.Logger.LogInformation("NPGSQL provider: {}", db.Database.ProviderName);
        }

        var forwardedOptions = new ForwardedHeadersOptions {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
        };
        forwardedOptions.KnownIPNetworks.Clear();
        forwardedOptions.KnownProxies.Clear();
        forwardedOptions.KnownProxies.Add(IPAddress.Loopback);
        forwardedOptions.KnownProxies.Add(IPAddress.IPv6Loopback);
        Application.UseForwardedHeaders(forwardedOptions);

        Application.Use(async (context, next) => {
            context.Response.Headers["X-Frame-Options"] = "SAMEORIGIN";
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";
            context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            await next.Invoke();
        });

        Application.UseDefaultFiles();
        Application.MapStaticAssets();
        Application.UseCors();
        Application.UseAuthentication();
		Application.UseMiddleware<AntiforgeryValidationMiddleware>();
        Application.UseAuthorization();
		Application.UseRateLimiter();

        Application.MapControllers();
        Application.MapHub<ReservationsHub>("/hubs/reservations", options => options.CloseOnAuthenticationExpiration = true)
            .RequireAuthorization(policy => policy.RequireAssertion(context =>
                context.User.Identity?.IsAuthenticated == true
                || context.Resource is HttpContext httpContext
                    && httpContext.Request.Query["requireAuthentication"] != "true"));

        //app.MapFallbackToFile("/index.html");

        await Application.RunAsync();
    }

	private static string GetFrontendOrigin() => FrontendUrl.GetOrigin();

	private static bool HasAccountType(ClaimsPrincipal principal, AccountType requiredType) {
		var value = principal.FindFirstValue(ClaimTypes.Role);
		return Enum.TryParse<AccountType>(value, out var accountType) && accountType >= requiredType;
	}
}
