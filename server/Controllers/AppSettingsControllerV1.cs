using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using server.Dto.Requests;
using server.Dto.Responses;
using server.Services;
using server.Infrastructure;
using System.Globalization;

namespace server.Controllers;

[ApiController]
[Route("api/v1/appsettings")]
[Authorize(Policy = AuthPolicies.Admin)]
public sealed class AppSettingsControllerV1(
    IAuthService auth,
    IAppSettingsService settings,
    AppCacheService cache,
    IDbLoggerService dbLogger
) : ControllerBase {
    [HttpGet]
    public async Task<ActionResult> Get(CancellationToken ct) {
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

        return Ok(new AppSettingsResponse {
            ChatEnabled = await settings.GetChatEnabledAsync(ct),
            ServerNow = DateTime.UtcNow,
            ReservationsEnabledFrom = reservationsEnabledFrom,
            ReservationsEnabledTo = reservationsEnabledTo,
            ReservationsStatus = reservationsStatus.ToString(),
            ReservationsEnabledRightNow = reservationsEnabledRightNow,
            AttendanceEnabled = await settings.GetAttendanceEnabledAsync(ct),
            ProblemReportsEnabled = await settings.GetProblemReportsEnabledAsync(ct)
        });
    }

    [HttpPut]
    public async Task<IActionResult> Update(
        [FromBody] UpdateAppSettingsRequest request,
        CancellationToken ct) {
        var actorId = auth.GetCurrentAccountId();
        var changes = new List<string>();

        await UpdateFeatureSettingsAsync(request, changes, ct);

        if (!string.IsNullOrWhiteSpace(request.ReservationsStatus)) {
            if (!Enum.TryParse<IAppSettingsService.ReservationStatusType>(
                    request.ReservationsStatus,
                    ignoreCase: true,
                    out var status)) {
                return BadRequest(new {
                    success = false,
                    message = "Neplatný status rezervací."
                });
            }

            var previousValue = await settings.GetReservationsStatusAsync(ct);
            await settings.SetReservationsStatusAsync(status, ct);
            AddChange(changes, "ReservationsStatus", previousValue, status);
        }

        if (request.ReservationsEnabledFrom.HasValue) {
            var previousValue = await settings.GetReservationsEnabledFromAsync(ct);
            await settings.SetReservationsEnabledFromAsync(request.ReservationsEnabledFrom.Value, ct);
            AddChange(changes, "ReservationsEnabledFrom", previousValue, request.ReservationsEnabledFrom.Value);
        }

        if (request.ReservationsEnabledTo.HasValue) {
            var previousValue = await settings.GetReservationsEnabledToAsync(ct);
            await settings.SetReservationsEnabledToAsync(request.ReservationsEnabledTo.Value, ct);
            AddChange(changes, "ReservationsEnabledTo", previousValue, request.ReservationsEnabledTo.Value);
        }

        if (changes.Count > 0) {
            await dbLogger.LogInfoAsync(
                $"Uživatel ({actorId}) upravil časy a nastavení rezervací: {string.Join("; ", changes)}.",
                "app-settings-edit",
                actorId,
                "app_settings",
                ct
            );
        }

        return NoContent();
    }

    private async Task UpdateFeatureSettingsAsync(
        UpdateAppSettingsRequest request,
        ICollection<string> changes,
        CancellationToken ct) {
        if (request.ChatEnabled.HasValue) {
            var previousValue = await settings.GetChatEnabledAsync(ct);
            await settings.SetChatEnabledAsync(request.ChatEnabled.Value, ct);
            AddChange(changes, "ChatEnabled", previousValue, request.ChatEnabled.Value);
        }

        if (request.AttendanceEnabled.HasValue) {
            var previousValue = await settings.GetAttendanceEnabledAsync(ct);
            await settings.SetAttendanceEnabledAsync(request.AttendanceEnabled.Value, ct);
            AddChange(changes, "AttendanceEnabled", previousValue, request.AttendanceEnabled.Value);
        }

        if (request.ProblemReportsEnabled.HasValue) {
            var previousValue = await settings.GetProblemReportsEnabledAsync(ct);
            await settings.SetProblemReportsEnabledAsync(request.ProblemReportsEnabled.Value, ct);
            AddChange(changes, "ProblemReportsEnabled", previousValue, request.ProblemReportsEnabled.Value);
        }
    }

    [HttpPost("cache/clear")]
    public async Task<IActionResult> ClearCache(CancellationToken ct) {
        var actorId = auth.GetCurrentAccountId();
        var result = cache.Clear();

        await dbLogger.LogWarnAsync(
            $"Uživatel ({actorId}) vyčistil aplikační cache ({result.RemovedKeys} klíčů).",
            "app-cache-clear",
            actorId,
            "cache",
            ct
        );

        return Ok(result);
    }

    private static void AddChange<T>(ICollection<string> changes, string name, T previousValue, T nextValue) {
        var previous = FormatValue(previousValue);
        var next = FormatValue(nextValue);

        if (previous == next) {
            return;
        }

        changes.Add($"{FormatChangeName(name)}: {previous} -> {next}");
    }

    private static string FormatChangeName(string name) {
        return name switch {
            "ReservationsEnabledFrom" => "Začátek rezervací",
            "ReservationsEnabledTo" => "Konec rezervací",
            "ReservationsStatus" => "Stav rezervací",
            "ChatEnabled" => "Chat",
            "AttendanceEnabled" => "Dochazka",
            "ProblemReportsEnabled" => "Hlaseni problemu",
            _ => name
        };
    }

    private static string FormatValue<T>(T value) {
        return value switch {
            DateTime date => date.ToLocalTime().ToString("dd. MM. yyyy HH:mm:ss", CultureInfo.GetCultureInfo("cs-CZ")),
            null => "(null)",
            _ => value.ToString() ?? "(null)"
        };
    }
}
