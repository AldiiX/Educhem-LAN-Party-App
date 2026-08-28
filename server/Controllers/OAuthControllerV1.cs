using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using server.Data.Entities;
using server.Dto.Mappers;
using server.Services;

namespace server.Controllers;

/// <summary>
/// prevadi oauth http endpointy na jednotny service flow s vyuzitim standardnich ASP.NET Core provideru
/// po dokonceni callbacku mapuje normalizovany vysledek na prihlaseni, chybu nebo frontend presmerovani
/// </summary>
[ApiController]
[Route("api/v1/{provider}")]
public sealed class OAuthControllerV1(IAuthService auth, IOAuthService oauth) : Controller {
	private const string ExternalScheme = "ExternalCookie";

	/// <summary>
	/// zahaji login flow a presmeruje prohlizec na authorization url vybraneho providera
	/// </summary>
	[HttpGet("login")]
	public IActionResult Login(string provider) {
		if (!TryGetProvider(provider, out var oauthProvider)) return NotFound();
		if (!oauth.IsProviderConfigured(oauthProvider)) return Problem(statusCode: StatusCodes.Status503ServiceUnavailable);

		var origin = oauth.GetFrontendOrigin();
		if (origin == null) return Problem(statusCode: StatusCodes.Status503ServiceUnavailable);

		var props = new AuthenticationProperties {
			RedirectUri = $"/api/v1/{provider}/callback-complete",
			Items = {
				["flow"] = "login",
				["provider"] = provider,
				["origin"] = origin,
			}
		};

		return Challenge(props, oauth.GetAuthenticationScheme(oauthProvider));
	}

	/// <summary>
	/// po reautentizaci zahaji connect flow pro propojeni platformy s prihlasenym uctem
	/// </summary>
	[HttpGet("connect")]
	public async Task<IActionResult> Connect(string provider, CancellationToken ct = default) {
		if (!TryGetProvider(provider, out var oauthProvider)) return NotFound();
		if (!oauth.IsProviderConfigured(oauthProvider)) return Problem(statusCode: StatusCodes.Status503ServiceUnavailable);

		var account = await auth.ReAuthAsync(ct);
		if (account == null) return Unauthorized();

		var origin = oauth.GetFrontendOrigin();
		if (origin == null) return Problem(statusCode: StatusCodes.Status503ServiceUnavailable);

		var props = new AuthenticationProperties {
			RedirectUri = $"/api/v1/{provider}/callback-complete",
			Items = {
				["flow"] = "connect",
				["provider"] = provider,
				["accountId"] = account.Id.ToString(),
				["origin"] = origin,
			}
		};

		return Challenge(props, oauth.GetAuthenticationScheme(oauthProvider));
	}

	/// <summary>
	/// dokonci externi autentizaci po navratu z middleware a provede lokalni prihlaseni nebo propojeni
	/// </summary>
	[HttpGet("callback-complete")]
	public async Task<IActionResult> CallbackComplete(string provider, CancellationToken ct = default) {
		if (!TryGetProvider(provider, out var oauthProvider)) return NotFound();

		var authResult = await HttpContext.AuthenticateAsync(ExternalScheme);
		var origin = oauth.GetFrontendOrigin() ?? "/";
		var parameter = provider.ToLowerInvariant();
		var accountSettingsPath = "/app/account/settings";

		if (!authResult.Succeeded || authResult.Principal == null) {
			return Redirect(BuildRedirect(origin, $"/app/login?{parameter}=error"));
		}

		await HttpContext.SignOutAsync(ExternalScheme);

		var completion = await oauth.CompleteExternalAuthAsync(authResult, oauthProvider, ct);
		if (completion.ReturnOrigin != null) origin = completion.ReturnOrigin;

		switch (completion.Kind) {
			case OAuthCompletionKind.LoginSucceeded:
				if (completion.AccountId == null || await auth.SignInAsAsync(completion.AccountId.Value, ct) == null) {
					return Redirect(BuildRedirect(origin, $"/app/login?{parameter}=error"));
				}
				return Redirect(BuildRedirect(origin, "/app"));
			case OAuthCompletionKind.LoginNotLinked:
				return Redirect(BuildRedirect(origin, $"/app/login?{parameter}=not-linked"));
			case OAuthCompletionKind.Connected:
				return Redirect(BuildRedirect(origin, $"{accountSettingsPath}?{parameter}=linked"));
			case OAuthCompletionKind.AlreadyLinked:
				return Redirect(BuildRedirect(origin, $"{accountSettingsPath}?{parameter}=already-linked"));
			case OAuthCompletionKind.Cancelled:
				return Redirect(BuildRedirect(origin, completion.Flow == OAuthFlow.Connect
					? $"{accountSettingsPath}?{parameter}=cancelled"
					: $"/app/login?{parameter}=cancelled"));
			default:
				return Redirect(BuildRedirect(origin, completion.Flow == OAuthFlow.Connect
					? $"{accountSettingsPath}?{parameter}=error"
					: $"/app/login?{parameter}=error"));
		}
	}

	/// <summary>
	/// fallback pro prime volani callback endpointu mimo middleware
	/// </summary>
	[HttpGet("callback")]
	public IActionResult Callback(string provider) {
		var origin = oauth.GetFrontendOrigin() ?? "/";
		return Redirect(BuildRedirect(origin, $"/app/login?{provider.ToLowerInvariant()}=error"));
	}

	/// <summary>
	/// po reautentizaci odpoji platformu od prihlaseneho uctu a vrati aktualizovana data uctu
	/// </summary>
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
