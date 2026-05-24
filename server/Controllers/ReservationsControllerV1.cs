using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using server.Data;
using server.Dto.Mappers;
using server.Infrastructure;
using server.Services;

namespace server.Controllers;

[ApiController]
[Route("api/v1/reservations")]
public sealed class ReservationsControllerV1(
	ReservationCacheService reservationCache,
	IAppSettingsService appSettings
) : Controller {
	#if !DEBUG
	[HttpGet]
	public IActionResult Index() => new NotFoundObjectResult(new { Message = "Rezervace probíhají přes socket, ne přes API.", Success = false });
	#endif

	#if DEBUG
	[HttpGet]
	public async Task<IActionResult> Index() {
		var reservations = await reservationCache.GetReservationsAsync(true);
		return new JsonResult(reservations);
	}
	#endif
	
	[HttpGet("computers-and-rooms"), HttpGet("rooms-and-computers")]
	public async Task<IActionResult> GetComputersAndRooms() {
		return new JsonResult(await reservationCache.GetRoomsAndComputersAsync());
	}

	[HttpGet("status")]
	public async Task<IActionResult> Status(CancellationToken ct) {
		var cache = await reservationCache.GetStatusAsync();
		var reservationsEnabled = await appSettings.AreReservationsEnabledRightNowAsync(ct);
		var reservationsStatus = await appSettings.GetReservationsStatusAsync(ct);
		var reservationsEnabledFrom = DateTime.SpecifyKind(
			await appSettings.GetReservationsEnabledFromAsync(ct),
			DateTimeKind.Utc
		);
		var reservationsEnabledTo = DateTime.SpecifyKind(
			await appSettings.GetReservationsEnabledToAsync(ct),
			DateTimeKind.Utc
		);

		return Ok(new {
			maxCapacity = cache.MaxCapacity,
			capacityUsed = cache.CapacityUsed,
			capacityUsedPercentage = cache.CapacityUsedPercentage,
			accountsWithEnabledReservations = cache.AccountsWithEnabledReservations,
			accountsWithEnabledReservationsPercentage = cache.AccountsWithEnabledReservationsPercentage,
			reservationsEnabled,
			reservationsStatus = reservationsStatus.ToString(),
			serverNow = DateTime.UtcNow,
			reservationsEnabledFrom,
			reservationsEnabledTo,
			message = reservationsEnabled
				? "Rezervace jsou otevřené."
				: "Rezervace jsou momentálně uzavřené."
		});
	}
}
