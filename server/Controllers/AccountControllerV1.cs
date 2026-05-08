using Microsoft.AspNetCore.Mvc;
using server.Dto.Mappers;
using server.Dto.Requests;
using server.Services;

namespace server.Controllers;

[ApiController]
[Route("api/v1/account")]
public class AccountControllerV1(IAuthService auth) : Controller {

	[HttpGet]
	public async Task<IActionResult> GetMyAccount(CancellationToken ct = default) {
		var acc = await auth.ReAuthAsync(ct);
		if(acc == null) return new UnauthorizedResult();

		return Ok(acc.ToDto());
	}

	[HttpPost("login")]
	public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct = default) {
		var acc = await auth.LoginAsync(request.Email, request.PasswordPlain, ct);
		if (acc == null) return Unauthorized();

		return new OkObjectResult(acc.ToDto());
	}
}