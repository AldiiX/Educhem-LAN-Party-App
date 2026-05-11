using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
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
}