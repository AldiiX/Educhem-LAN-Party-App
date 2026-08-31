using dotenv.net;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
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
using server.Services;
using server.Services.OAuth;
using StackExchange.Redis;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;

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

        var config = new ConfigurationOptions {
            EndPoints = { $"{rhost}:{rport}" },
            AbortOnConnectFail = false
        };

        if (rpassword != null!) {
            config.Password = rpassword;
        }

        var redis = await ConnectionMultiplexer.ConnectAsync(config);
        builder.Services.AddSingleton<IConnectionMultiplexer>(redis);

        builder.Services.AddControllersWithViews();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddSignalR(options => {
            options.KeepAliveInterval = TimeSpan.FromSeconds(10);
            options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
            options.HandshakeTimeout = TimeSpan.FromSeconds(15);
        });
        builder.Services.AddDataProtection()
            .PersistKeysToStackExchangeRedis(redis, "DataProtection-Keys")
            .SetApplicationName("EduchemLANPartyApp");

        builder.Services.AddSingleton<IDistributedCache>(sp =>
            new RedisCache(new RedisCacheOptions {
                ConfigurationOptions = ConfigurationOptions.Parse(redis.Configuration),
                InstanceName = "EduchemLANParty_session"
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

        builder.Services.AddControllers();
        builder.Services.AddHttpContextAccessor();
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
            ForwardedHeaders = ForwardedHeaders.All,
        };
        forwardedOptions.KnownIPNetworks.Clear();
        forwardedOptions.KnownProxies.Clear();
        Application.UseForwardedHeaders(forwardedOptions);

        Application.UseDefaultFiles();
        Application.MapStaticAssets();
        Application.UseCors();
        Application.UseAuthentication();
		Application.UseMiddleware<AntiforgeryValidationMiddleware>();
        Application.UseAuthorization();

        // pridani X-Powered-By
        Application.Use(async (context, next) => {
            context.Response.Headers.Append("X-Powered-By", "ASP.NET");
            await next.Invoke();
        });

        Application.MapControllers();
        Application.MapHub<ReservationsHub>("/hubs/reservations");

        //app.MapFallbackToFile("/index.html");

        await Application.RunAsync();
    }

	private static string GetFrontendOrigin() {
		if (!ENV.TryGetValue("WEB_URL", out var webUrl) || !Uri.TryCreate(webUrl, UriKind.Absolute, out var uri)
			|| uri.Scheme is not ("http" or "https") || !string.IsNullOrEmpty(uri.UserInfo)
			|| !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment)) {
			throw new InvalidOperationException("WEB_URL musi obsahovat platnou HTTP(S) originu.");
		}

		return uri.GetLeftPart(UriPartial.Authority);
	}

	private static bool HasAccountType(ClaimsPrincipal principal, AccountType requiredType) {
		var value = principal.FindFirstValue(ClaimTypes.Role);
		return Enum.TryParse<AccountType>(value, out var accountType) && accountType >= requiredType;
	}
}
