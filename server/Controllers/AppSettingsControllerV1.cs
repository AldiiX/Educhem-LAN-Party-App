using Microsoft.AspNetCore.Mvc;
using server.Data.Entities;
using server.Dto;
using server.Dto.Requests;
using server.Services;

namespace server.Controllers;

[ApiController]
[Route("api/v1/appsettings")]
public sealed class AppSettingsControllerV1(
    IAuthService auth,
    IAppSettingsService settings
) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<AppSettingsItemDto>> Get(CancellationToken ct)
    {
        var acc = await auth.ReAuthFromContextOrNullAsync(ct);

        if (acc == null || acc.AccountType < AccountType.SuperAdmin)
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
        
        
        return Ok(new AppSettingsItemDto
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

        if (acc == null || acc.AccountType < AccountType.SuperAdmin)
        {
            return Unauthorized(new
            {
                success = false,
                message = "Nelze upravit nastavení aplikace, pokud nejsi přihlášený, nebo nemáš dostatečná práva."
            });
        }

        if (request.ChatEnabled.HasValue)
        {
            await settings.SetChatEnabledAsync(request.ChatEnabled.Value, ct);
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

            await settings.SetReservationsStatusAsync(status, ct);
        }

        if (request.ReservationsEnabledFrom.HasValue)
        {
            await settings.SetReservationsEnabledFromAsync(request.ReservationsEnabledFrom.Value, ct);
        }

        if (request.ReservationsEnabledTo.HasValue)
        {
            await settings.SetReservationsEnabledToAsync(request.ReservationsEnabledTo.Value, ct);
        }

        return NoContent();
    }
}