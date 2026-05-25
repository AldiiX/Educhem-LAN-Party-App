using Microsoft.AspNetCore.Mvc;
using server.Data.Entities;
using server.Dto.Requests;
using server.Dto.Responses;
using server.Services;
using System.Globalization;

namespace server.Controllers;

[ApiController]
[Route("api/v1/appsettings")]
public sealed class AppSettingsControllerV1(
    IAuthService auth,
    IAppSettingsService settings,
    AppCacheService cache,
    IDbLoggerService dbLogger
) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult> Get(CancellationToken ct)
    {
        var acc = await auth.ReAuthFromContextOrNullAsync(ct);

        if (acc == null || acc.AccountType < AccountType.Admin)
        {
            return Unauthorized(new
            {
                success = false,
                message = "Nelze zobrazit nastavení aplikace, pokud nejsi přihlášený, nebo nemáš dostatečná práva."
            });
        }

        var reservationsStatus = await settings.GetReservationsStatusAsync(ct);
        var reservationsEnabledRightNow = await settings.SyncReservationsEnabledRightNowAsync(ct);
        var reservationsEnabledFrom = DateTime.SpecifyKind(
            await settings.GetReservationsEnabledFromAsync(ct),
            DateTimeKind.Utc
        );

        var reservationsEnabledTo = DateTime.SpecifyKind(
            await settings.GetReservationsEnabledToAsync(ct),
            DateTimeKind.Utc
        );

        return Ok(new AppSettingsResponse
        {
            ChatEnabled = await settings.GetChatEnabledAsync(ct),
            ServerNow = DateTime.UtcNow,
            ReservationsEnabledFrom = reservationsEnabledFrom,
            ReservationsEnabledTo = reservationsEnabledTo,
            ReservationsStatus = reservationsStatus.ToString(),
            ReservationsEnabledRightNow = reservationsEnabledRightNow
        });
    }

    [HttpPut]
    public async Task<IActionResult> Update(
        [FromBody] UpdateAppSettingsRequest request,
        CancellationToken ct)
    {
        var acc = await auth.ReAuthFromContextOrNullAsync(ct);

        if (acc == null || acc.AccountType < AccountType.Admin)
        {
            return Unauthorized(new
            {
                success = false,
                message = "Nelze upravit nastavení aplikace, pokud nejsi přihlášený, nebo nemáš dostatečná práva."
            });
        }

        var changes = new List<string>();

        if (request.ChatEnabled.HasValue)
        {
            var previousValue = await settings.GetChatEnabledAsync(ct);
            await settings.SetChatEnabledAsync(request.ChatEnabled.Value, ct);
            AddChange(changes, "ChatEnabled", previousValue, request.ChatEnabled.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.ReservationsStatus))
        {
            if (!Enum.TryParse<IAppSettingsService.ReservationStatusType>(
                    request.ReservationsStatus,
                    ignoreCase: true,
                    out var status))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Neplatný status rezervací."
                });
            }

            var previousValue = await settings.GetReservationsStatusAsync(ct);
            await settings.SetReservationsStatusAsync(status, ct);
            AddChange(changes, "ReservationsStatus", previousValue, status);
        }

        if (request.ReservationsEnabledFrom.HasValue)
        {
            var previousValue = await settings.GetReservationsEnabledFromAsync(ct);
            await settings.SetReservationsEnabledFromAsync(request.ReservationsEnabledFrom.Value, ct);
            AddChange(changes, "ReservationsEnabledFrom", previousValue, request.ReservationsEnabledFrom.Value);
        }

        if (request.ReservationsEnabledTo.HasValue)
        {
            var previousValue = await settings.GetReservationsEnabledToAsync(ct);
            await settings.SetReservationsEnabledToAsync(request.ReservationsEnabledTo.Value, ct);
            AddChange(changes, "ReservationsEnabledTo", previousValue, request.ReservationsEnabledTo.Value);
        }

        if (changes.Count > 0)
        {
            await dbLogger.LogInfoAsync(
                $"{UserNoun(acc)} {FormatAccount(acc)} {PastVerb(acc, "upravil", "upravila")} časy a nastavení rezervací: {string.Join("; ", changes)}.",
                "app-settings-edit",
                ct
            );
        }

        return NoContent();
    }

    [HttpPost("cache/clear")]
    public async Task<IActionResult> ClearCache(CancellationToken ct)
    {
        var acc = await auth.ReAuthFromContextOrNullAsync(ct);

        if (acc == null || acc.AccountType < AccountType.Admin)
        {
            return Unauthorized(new
            {
                success = false,
                message = "Nelze smazat cache aplikace, pokud nejsi přihlášený, nebo nemáš dostatečná práva."
            });
        }

        cache.Clear();

        await dbLogger.LogWarnAsync(
            $"{UserNoun(acc)} {FormatAccount(acc)} {PastVerb(acc, "vyčistil", "vyčistila")} aplikační cache.",
            "app-cache-clear",
            ct
        );

        return NoContent();
    }

    private static void AddChange<T>(ICollection<string> changes, string name, T previousValue, T nextValue)
    {
        var previous = FormatValue(previousValue);
        var next = FormatValue(nextValue);

        if (previous == next)
        {
            return;
        }

        changes.Add($"{FormatChangeName(name)}: {previous} -> {next}");
    }

    private static string FormatChangeName(string name)
    {
        return name switch
        {
            "ReservationsEnabledFrom" => "Začátek rezervací",
            "ReservationsEnabledTo" => "Konec rezervací",
            "ReservationsStatus" => "Stav rezervací",
            "ChatEnabled" => "Chat",
            _ => name
        };
    }

    private static string FormatValue<T>(T value)
    {
        return value switch
        {
            DateTime date => date.ToLocalTime().ToString("dd. MM. yyyy HH:mm:ss", CultureInfo.GetCultureInfo("cs-CZ")),
            null => "(null)",
            _ => value.ToString() ?? "(null)"
        };
    }

    private static string FormatAccount(Account account)
    {
        return $"{account.FirstName} {account.LastName} ({account.Email})";
    }

    private static string UserNoun(Account account)
    {
        return account.Gender == Gender.Female ? "Uživatelka" : "Uživatel";
    }

    private static string PastVerb(Account account, string masculine, string feminine)
    {
        return account.Gender == Gender.Female ? feminine : masculine;
    }
}
