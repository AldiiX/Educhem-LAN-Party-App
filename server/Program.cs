using dotenv.net;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Npgsql.NameTranslation;
using server.Data;
using server.Data.Entities;
using server.Hubs;
using server.Services;
using StackExchange.Redis;
using server.Data.Seeders;

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
        builder.Services.AddSignalR();
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
            // pripojeni na postgres pres npgsql provider
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

        builder.Services.AddSession(options => {
            options.IdleTimeout = TimeSpan.FromDays(365);
            //options.Cookie.IsEssential = true;
            options.Cookie.MaxAge = TimeSpan.FromDays(365);
            options.Cookie.Name = "educhemlanparty_session";
        });

        // Add services to the container.

        builder.Services.AddControllers();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddHttpClient();
        builder.Services.AddMemoryCache(); // pro pripad, ze bych chtel nekdy skalovat (asi ne) je lepsi vyuzit redis pokud mam multiistance app coz pro lanku asi mit stejne nikdy nebudu

        builder.Services.AddSingleton<AppCacheService>();
        builder.Services.AddScoped<IAuthService, AuthService>();
        builder.Services.AddScoped<ReservationCacheService>();
        builder.Services.AddScoped<IDbLoggerService, DbLoggerService>();
        builder.Services.AddScoped<IAppSettingsService, AppSettingsService>();

        Application = builder.Build();
        
        await AppSettingsItemSeeder.SeedAsync(Application);
        
        using (var scope = Application.Services.CreateScope()) {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            
            Application.Logger.LogInformation("NPGSQL provider: {}", db.Database.ProviderName);
        }

        Application.UseDefaultFiles();
        Application.MapStaticAssets();
        Application.UseSession();

        //app.UseHttpsRedirection();

        Application.UseAuthorization();
        Application.UseCors();

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
}
