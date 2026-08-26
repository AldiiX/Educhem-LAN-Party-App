using Microsoft.AspNetCore.Mvc;
using server.Data.Entities;
using server.Dto.Mappers;
using server.Services;

namespace server.Controllers;

/// <summary>
/// prevadi oauth http endpointy na jednotny service flow a neresi protokol ani odpovedi konkretniho provideru
/// po dokonceni callbacku mapuje normalizovany vysledek na prihlaseni, chybu nebo bezpecne frontend presmerovani
/// </summary>
/// <param name="auth">authentication service pouzity pro reautentizaci a vytvoreni lokalni session</param>
/// <param name="oauth">oauth service pouzity pro zahajeni, dokonceni a odpojeni provider flow</param>
[ApiController]
[Route("api/v1/{provider}")]
public sealed class OAuthControllerV1(IAuthService auth, IOAuthService oauth) : Controller {
	/// <summary>
	/// zahaji login flow a presmeruje prohlizec na authorization url vybraneho providera
	/// </summary>
	/// <param name="provider">route segment identifikujici povoleneho providera</param>
	/// <param name="ct">token pro zruseni asynchronni operace</param>
	/// <returns>redirect na providera, not found pro neznamy route segment nebo service unavailable pri chybejici konfiguraci</returns>
	[HttpGet("login")]
	public async Task<IActionResult> Login(string provider, CancellationToken ct = default) {
		if (!TryGetProvider(provider, out var oauthProvider)) return NotFound();
		var authorizationUrl = await oauth.CreateAuthorizationUrlAsync(null, oauthProvider, OAuthFlow.Login, Request, ct);
		return authorizationUrl == null ? Problem(statusCode: StatusCodes.Status503ServiceUnavailable) : Redirect(authorizationUrl.ToString());
	}

	/// <summary>
	/// po reautentizaci zahaji connect flow pro propojeni platformy s prihlasenym uctem
	/// </summary>
	/// <param name="provider">route segment identifikujici povoleneho providera</param>
	/// <param name="ct">token pro zruseni asynchronni operace</param>
	/// <returns>redirect na providera, unauthorized bez platne session nebo chybovy http vysledek</returns>
	[HttpGet("connect")]
	public async Task<IActionResult> Connect(string provider, CancellationToken ct = default) {
		if (!TryGetProvider(provider, out var oauthProvider)) return NotFound();
		var account = await auth.ReAuthAsync(ct);
		if (account == null) return Unauthorized();

		var authorizationUrl = await oauth.CreateAuthorizationUrlAsync(account.Id, oauthProvider, OAuthFlow.Connect, Request, ct);
		return authorizationUrl == null ? Problem(statusCode: StatusCodes.Status503ServiceUnavailable) : Redirect(authorizationUrl.ToString());
	}

	/// <summary>
	/// zpracuje callback providera a prevede normalizovany vysledek na lokalni prihlaseni nebo frontend presmerovani
	/// </summary>
	/// <param name="provider">route segment providera, ktery callback odeslal</param>
	/// <param name="state">jednorazovy state pouzity pro kontrolu puvodu a navaznosti flow</param>
	/// <param name="code">authorization code vraceny oauth providerem</param>
	/// <param name="error">chyba nebo informace o zruseni vracena providerem</param>
	/// <param name="ct">token pro zruseni asynchronni operace</param>
	/// <returns>lokalni prihlaseni, bad request pro neplatny state nebo redirect s vysledkem flow</returns>
	[HttpGet("callback")]
	public async Task<IActionResult> Callback(string provider, [FromQuery] string? state, [FromQuery] string? code, [FromQuery] string? error, CancellationToken ct = default) {
		if (!TryGetProvider(provider, out var oauthProvider)) return NotFound();
		var completion = await oauth.CompleteAuthorizationAsync(Request, oauthProvider, state, code, error, ct);
		if (completion.Kind == OAuthCompletionKind.InvalidState || completion.ReturnOrigin == null) return BadRequest("Neplatný OAuth stav.");

		// cilovy origin pochazi z overeneho state a nesklada se z request hlavicek callbacku
		var origin = completion.ReturnOrigin;
		var parameter = oauthProvider switch {
			OAuthProvider.Discord => "discord",
			OAuthProvider.GitHub => "github",
			OAuthProvider.Google => "google",
			OAuthProvider.Apple => "apple",
			OAuthProvider.Steam => "steam",
			_ => throw new InvalidOperationException("Nepodporovany OAuth poskytovatel."),
		};
		var accountSettingsPath = "/app/account/settings";
		switch (completion.Kind) {
			case OAuthCompletionKind.LoginSucceeded:
				if (completion.AccountId == null || await auth.SignInAsAsync(completion.AccountId.Value, ct) == null) return Redirect(BuildRedirect(origin, $"/app/login?{parameter}=error"));
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
	/// po reautentizaci odpoji platformu od prihlaseneho uctu a vrati aktualizovana data uctu
	/// </summary>
	/// <param name="provider">route segment odpojovaneho providera</param>
	/// <param name="ct">token pro zruseni asynchronni operace</param>
	/// <returns>aktualizovany ucet, unauthorized bez platne session nebo not found pri chybejicim uctu</returns>
	[HttpDelete("connection")]
	public async Task<IActionResult> Disconnect(string provider, CancellationToken ct = default) {
		if (!TryGetProvider(provider, out var oauthProvider)) return NotFound();
		var account = await auth.ReAuthAsync(ct);
		if (account == null) return Unauthorized();

		var updated = await oauth.DisconnectAsync(account.Id, oauthProvider, ct);
		return updated == null ? NotFound() : Ok(updated.ToDto());
	}

	/// <summary>
	/// mapuje route segment pouze na explicitne povolene oauth providery
	/// </summary>
	/// <param name="value">hodnota provider segmentu z route</param>
	/// <param name="provider">vystupni enum hodnota rozpoznaneho providera</param>
	/// <returns>true pro podporovany route segment, jinak false</returns>
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

	/// <summary>
	/// sklada absolutni redirect pouze nad originem ulozenym v overenem oauth state
	/// </summary>
	/// <param name="origin">overeny frontend origin bez uzivatelsky rizene cesty</param>
	/// <param name="pathAndQuery">aplikacni cesta a query s vysledkem flow</param>
	/// <returns>absolutni redirect url smerujici na povoleny frontend origin</returns>
	private static string BuildRedirect(string origin, string pathAndQuery) => new Uri(new Uri(origin), pathAndQuery).ToString();
}
