using Microsoft.AspNetCore.Mvc;
using server.Data.Entities;
using server.Dto.Mappers;
using server.Services;

namespace server.Controllers;

[ApiController]
[Route("api/v1/{provider}")]
public sealed class OAuthControllerV1(IAuthService auth, IOAuthService oauth) : Controller {
	[HttpGet("login")]
	public async Task<IActionResult> Login(string provider, CancellationToken ct = default) {
		if (!TryGetProvider(provider, out var oauthProvider)) return NotFound();
		var authorizationUrl = await oauth.CreateAuthorizationUrlAsync(null, oauthProvider, OAuthFlow.Login, Request, ct);
		return authorizationUrl == null ? Problem(statusCode: StatusCodes.Status503ServiceUnavailable) : Redirect(authorizationUrl.ToString());
	}

	[HttpGet("connect")]
	public async Task<IActionResult> Connect(string provider, CancellationToken ct = default) {
		if (!TryGetProvider(provider, out var oauthProvider)) return NotFound();
		var account = await auth.ReAuthAsync(ct);
		if (account == null) return Unauthorized();

		var authorizationUrl = await oauth.CreateAuthorizationUrlAsync(account.Id, oauthProvider, OAuthFlow.Connect, Request, ct);
		return authorizationUrl == null ? Problem(statusCode: StatusCodes.Status503ServiceUnavailable) : Redirect(authorizationUrl.ToString());
	}

	[HttpGet("callback")]
	public async Task<IActionResult> Callback(string provider, [FromQuery] string? state, [FromQuery] string? code, [FromQuery] string? error, CancellationToken ct = default) {
		if (!TryGetProvider(provider, out var oauthProvider)) return NotFound();
		var completion = await oauth.CompleteAuthorizationAsync(Request, oauthProvider, state, code, error, ct);
		if (completion.Kind == OAuthCompletionKind.InvalidState || completion.ReturnOrigin == null) return BadRequest("Neplatný OAuth stav.");

		var origin = completion.ReturnOrigin;
		var parameter = oauthProvider switch {
			OAuthProvider.Discord => "discord",
			OAuthProvider.GitHub => "github",
			OAuthProvider.Google => "google",
			OAuthProvider.Apple => "apple",
			OAuthProvider.Steam => "steam",
			_ => throw new InvalidOperationException("Nepodporovany OAuth poskytovatel."),
		};
		switch (completion.Kind) {
			case OAuthCompletionKind.LoginSucceeded:
				if (completion.AccountId == null || await auth.SignInAsAsync(completion.AccountId.Value, ct) == null) return Redirect(BuildRedirect(origin, $"/app/login?{parameter}=error"));
				return Redirect(BuildRedirect(origin, "/app"));
			case OAuthCompletionKind.LoginNotLinked:
				return Redirect(BuildRedirect(origin, $"/app/login?{parameter}=not-linked"));
			case OAuthCompletionKind.Connected:
				return Redirect(BuildRedirect(origin, $"/app/account?{parameter}=linked"));
			case OAuthCompletionKind.AlreadyLinked:
				return Redirect(BuildRedirect(origin, $"/app/account?{parameter}=already-linked"));
			case OAuthCompletionKind.Cancelled:
				return Redirect(BuildRedirect(origin, $"/app/login?{parameter}=cancelled"));
			default:
				return Redirect(BuildRedirect(origin, $"/app/login?{parameter}=error"));
		}
	}

	[HttpDelete("connection")]
	public async Task<IActionResult> Disconnect(string provider, CancellationToken ct = default) {
		if (!TryGetProvider(provider, out var oauthProvider)) return NotFound();
		var account = await auth.ReAuthAsync(ct);
		if (account == null) return Unauthorized();

		var updated = await oauth.DisconnectAsync(account.Id, oauthProvider, ct);
		return updated == null ? NotFound() : Ok(updated.ToDto());
	}

	private static bool TryGetProvider(string value, out OAuthProvider provider) {
		provider = value.ToLowerInvariant() switch {
			"discord" => OAuthProvider.Discord,
			"github" => OAuthProvider.GitHub,
			"google" => OAuthProvider.Google,
			"apple" => OAuthProvider.Apple,
			"steam" => OAuthProvider.Steam,
			_ => default,
		};
		return value.Equals("discord", StringComparison.OrdinalIgnoreCase)
			|| value.Equals("github", StringComparison.OrdinalIgnoreCase)
			|| value.Equals("google", StringComparison.OrdinalIgnoreCase)
			|| value.Equals("apple", StringComparison.OrdinalIgnoreCase)
			|| value.Equals("steam", StringComparison.OrdinalIgnoreCase);
	}

	private static string BuildRedirect(string origin, string pathAndQuery) => new Uri(new Uri(origin), pathAndQuery).ToString();
}
