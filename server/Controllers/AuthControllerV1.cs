using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using server.Dto.Mappers;
using server.Dto.Requests;
using server.Infrastructure;
using server.Services;

namespace server.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthControllerV1(IAuthService auth, IAntiforgery antiforgery) : ControllerBase {
	[AllowAnonymous]
	[HttpGet("csrf")]
	public IActionResult Csrf() {
		var tokens = antiforgery.GetAndStoreTokens(HttpContext);
		if (tokens.RequestToken == null) return Problem(statusCode: StatusCodes.Status500InternalServerError);

		Response.Cookies.Append(AuthCookieNames.Csrf, tokens.RequestToken, new CookieOptions {
			HttpOnly = false,
			Secure = !Program.DevelopmentMode,
			SameSite = SameSiteMode.Lax,
			Path = "/",
			IsEssential = true,
		});
		return NoContent();
	}

	[AllowAnonymous]
	[HttpPost("login")]
	[EnableRateLimiting("auth-login")]
	public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct = default) {
		var result = await auth.LoginAsync(request.Email, request.PasswordPlain, request.RememberMe, ct);
		return result.Status switch {
			LoginStatus.Success => Ok(result.Account!.ToDto()),
			LoginStatus.LockedOut => StatusCode(StatusCodes.Status429TooManyRequests, result.LockoutMessage ?? "Příliš mnoho neúspěšných pokusů, zkuste to za chvíli."),
			_ => Unauthorized("Nesprávný e-mail nebo heslo.")
		};
	}

	[AllowAnonymous]
	[HttpPost("refresh")]
	public async Task<IActionResult> Refresh(CancellationToken ct = default) {
		return await auth.RefreshAsync(ct) ? NoContent() : Unauthorized();
	}

	[AllowAnonymous]
	[HttpPost("logout")]
	public async Task<IActionResult> Logout(CancellationToken ct = default) {
		await auth.LogoutAsync(ct);
		return NoContent();
	}
}
