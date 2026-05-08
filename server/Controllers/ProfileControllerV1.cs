using Microsoft.AspNetCore.Mvc;
using server.Data;
using server.Dto.Mappers;
using server.Infrastructure;
using server.Services;

namespace server.Controllers;

[ApiController]
[Route("api/v1/profile")]
public class ProfileControllerV1(AppDbContext db, IAuthService auth) : Controller {

	[HttpGet]
	public async Task<IActionResult> Get() {
		var me = await auth.ReAuthAsync();
		if(me == null) return new UnauthorizedResult();

		return Ok(me.ToProfileDto());
	}

	[HttpGet("{uuid:guid}")]
	public async Task<IActionResult> GetProfile([FromRoute] Guid uuid) {
		var profile = db.AccountsEf().FirstOrDefault(a => a.Id == uuid);
		if(profile == null) return NotFound();

		return Ok(profile.ToProfileDto());
	}
}