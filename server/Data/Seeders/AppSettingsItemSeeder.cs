using Microsoft.EntityFrameworkCore;
using server.Data.Entities;
using System.Globalization;

namespace server.Data.Seeders;

public static class AppSettingsItemSeeder
{
    private const string DateFormat = "O";

    public static async Task SeedAsync(WebApplication app)
    {
        using var scope = app.Services.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await db.Database.MigrateAsync();

        var now = DateTime.UtcNow;

        var defaults = new Dictionary<string, string>
        {
            ["ChatEnabled"] = "false",
            ["ReservationsStatus"] = "Closed",
            ["ReservationsEnabledFrom"] = now.ToString(DateFormat, CultureInfo.InvariantCulture),
            ["ReservationsEnabledTo"] = now.AddDays(7).ToString(DateFormat, CultureInfo.InvariantCulture),
            ["ReservationsEnabledRightNow"] = "false",
            ["AttendanceEnabled"] = "false",
            ["ProblemReportsEnabled"] = "false"
        };

        foreach (var item in defaults)
        {
            var exists = await db.AppSettings
                .AnyAsync(x => x.Property == item.Key);

            if (!exists)
            {
                db.AppSettings.Add(new AppSettingsItem
                {
                    Property = item.Key,
                    Value = item.Value
                });
            }
        }

        await db.SaveChangesAsync();
    }
}
