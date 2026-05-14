using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using server.Data;
using server.Dto.Mappers;
using server.Infrastructure;

namespace server.Controllers;

[ApiController]
[Route("api/v1/reservations")]
public sealed class ReservationsControllerV1(AppDbContext db) : Controller {

	#if !DEBUG
	[HttpGet]
	public IActionResult Index() => new NotFoundObjectResult(new { Message = "Rezervace probíhají přes socket, ne přes API.", Success = false });
	#endif

	#if DEBUG
	[HttpGet]
	public async Task<IActionResult> Index() {
		var reservations = db.ReservationsEf().ToList().Select(r => r.ToDto());
		return new JsonResult(reservations);
	}
	#endif

	[HttpGet("computers-and-rooms"), HttpGet("rooms-and-computers")]
	public async Task<IActionResult> GetComputersAndRooms() {
		var computers = db.ComputersEf().AsNoTracking().ToList().Select(c => c.ToDto());
		var rooms = db.RoomsEf().AsNoTracking().ToList().Select(r => r.ToDto());
		var result = new {
			Computers = computers,
			Rooms = rooms
		};
		return new JsonResult(result);
	}

	[HttpGet("status")]
	public async Task<IActionResult> Status() {
		var reservations = db.ReservationsEf().AsNoTracking().ToList();
		var computers = db.ComputersEf().AsNoTracking().ToList();
		var rooms = db.RoomsEf().AsNoTracking().ToList();
		var maxCapacity = computers.Count + rooms.Sum(r => r.Capacity);
		var accounts = db.Accounts.AsNoTracking().ToList();

		return new JsonResult(new {
			maxCapacity,
			accountsWithEnabledReservations = accounts.Count(a => a.EnableReservations),
			accountsWithEnabledReservationsPercentage = accounts.Count == 0 ? 0 : Math.Min(Math.Round((double)accounts.Count(a => a.EnableReservations) / accounts.Count * 100), 100),
			capacityUsed = reservations.Count,
			capacityUsedPercentage = maxCapacity == 0 ? 0 : Math.Min(Math.Round((double)reservations.Count / maxCapacity * 100), 100),
		});
	}
}