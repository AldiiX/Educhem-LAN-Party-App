using Microsoft.AspNetCore.Mvc;
using server.Dto.Mappers;
using server.Services;

namespace server.Controllers;

[ApiController]
[Route("api/v1/discord")]
public sealed class DiscordOAuthControllerV1(IAuthService auth, IDiscordOAuthService discordOAuth) : Controller {
	[HttpGet("login")]
	public async Task<IActionResult> Login(CancellationToken ct = default) {
		var authorizationUrl = await discordOAuth.CreateAuthorizationUrlAsync(null, DiscordOAuthFlow.Login, Request, ct);
		return authorizationUrl == null ? Problem(statusCode: StatusCodes.Status503ServiceUnavailable) : Redirect(authorizationUrl.ToString());
	}

	[HttpGet("connect")]
	public async Task<IActionResult> Connect(CancellationToken ct = default) {
		var account = await auth.ReAuthAsync(ct);
		if (account == null) return Unauthorized();

		var authorizationUrl = await discordOAuth.CreateAuthorizationUrlAsync(account.Id, DiscordOAuthFlow.Connect, Request, ct);
		return authorizationUrl == null ? Problem(statusCode: StatusCodes.Status503ServiceUnavailable) : Redirect(authorizationUrl.ToString());
	}

	[HttpGet("callback")]
	public async Task<IActionResult> Callback([FromQuery] string? state, [FromQuery] string? code, [FromQuery] string? error, CancellationToken ct = default) {
		var completion = await discordOAuth.CompleteAuthorizationAsync(Request, state, code, error, ct);
		if (completion.Kind == DiscordOAuthCompletionKind.InvalidState) return BadRequest("Neplatný Discord OAuth stav.");

		var origin = completion.ReturnOrigin ?? $"{Request.Scheme}://{Request.Host}";
		switch (completion.Kind) {
			case DiscordOAuthCompletionKind.LoginSucceeded:
				if (completion.AccountId == null || await auth.SignInAsAsync(completion.AccountId.Value, ct) == null) return Redirect(BuildRedirect(origin, "/app/login?discord=error"));
				return Redirect(BuildRedirect(origin, "/app"));
			case DiscordOAuthCompletionKind.LoginNotLinked:
				return Redirect(BuildRedirect(origin, "/app/login?discord=not-linked"));
			case DiscordOAuthCompletionKind.Connected:
				return Redirect(BuildRedirect(origin, "/app/account?discord=linked"));
			case DiscordOAuthCompletionKind.AlreadyLinked:
				return Redirect(BuildRedirect(origin, "/app/account?discord=already-linked"));
			case DiscordOAuthCompletionKind.Cancelled:
				return Redirect(BuildRedirect(origin, "/app/login?discord=cancelled"));
			default:
				return Redirect(BuildRedirect(origin, "/app/login?discord=error"));
		}
	}

	[HttpDelete("connection")]
	public async Task<IActionResult> Disconnect(CancellationToken ct = default) {
		var account = await auth.ReAuthAsync(ct);
		if (account == null) return Unauthorized();

		var updated = await discordOAuth.DisconnectAsync(account.Id, true, ct);
		return updated == null ? NotFound() : Ok(updated.ToDto());
	}

	private static string BuildRedirect(string origin, string pathAndQuery) => new Uri(new Uri(origin), pathAndQuery).ToString();
}
