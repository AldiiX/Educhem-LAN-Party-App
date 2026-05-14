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
	ReservationCacheService reservationCache
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
	public async Task<IActionResult> Status() {
		return new JsonResult(await reservationCache.GetStatusAsync());
	}
}
