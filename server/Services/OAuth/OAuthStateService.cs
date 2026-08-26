using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Caching.Distributed;
using server.Data.Entities;

namespace server.Services.OAuth;

/// <summary>
/// vytvari, uklada a jednorazove overuje oauth state spolecne s navratovym frontend originem
/// </summary>
/// <param name="cache">distribuovana cache pouzita jako serverove uloziste kratkodobeho state</param>
internal sealed class OAuthStateService(IDistributedCache cache) {
	private const string StateCookieName = "educhemlanparty_oauth_state";
	private static readonly TimeSpan StateLifetime = TimeSpan.FromMinutes(10);

	/// <summary>
	/// vytvori nahodny state, pkce verifier, pkce challenge a nonce pro nove oauth flow
	/// </summary>
	/// <returns>vsechny bezpecnostni hodnoty potrebne pro zahajeni flow</returns>
	internal static Models.StateParameters CreateParameters() {
		var state = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
		var codeVerifier = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
		var codeChallenge = WebEncoders.Base64UrlEncode(SHA256.HashData(Encoding.UTF8.GetBytes(codeVerifier)));
		var nonce = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
		return new Models.StateParameters(state, codeVerifier, codeChallenge, nonce);
	}

	/// <summary>
	/// nacte a normalizuje povoleny frontend origin z konfigurace aplikace
	/// </summary>
	/// <returns>schema a authority povoleneho frontend originu, nebo null pri neplatne konfiguraci</returns>
	internal string? GetFrontendOrigin() {
		// origin se nacita jenom z web_url, request hlavicky ho nesmi ovlivnit
		if (!Program.ENV.TryGetValue("WEB_URL", out var webUrl) || !Uri.TryCreate(webUrl, UriKind.Absolute, out var uri)) return null;
		if (uri.Scheme is not ("http" or "https") || !string.IsNullOrEmpty(uri.UserInfo) || !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment)) return null;
		return uri.GetLeftPart(UriPartial.Authority);
	}

	/// <summary>
	/// ulozi state payload do distribuovane cache a state hodnotu do zabezpecene cookie
	/// </summary>
	/// <param name="request">aktualni request poskytujici response pro zapis cookie</param>
	/// <param name="state">nahodna state hodnota pouzita jako cache klic a cookie hodnota</param>
	/// <param name="payload">serverova data rozpracovaneho oauth flow</param>
	/// <param name="ct">token pro zruseni asynchronni operace</param>
	/// <returns>ukol reprezentujici dokonceni zapisu do cache</returns>
	internal async Task StoreAsync(HttpRequest request, string state, Models.StatePayload payload, CancellationToken ct) {
		await cache.SetStringAsync(
			GetStateCacheKey(state),
			JsonSerializer.Serialize(payload),
			new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = StateLifetime },
			ct
		);

		request.HttpContext.Response.Cookies.Append(StateCookieName, state, new CookieOptions {
			HttpOnly = true,
			IsEssential = true,
			SameSite = SameSiteMode.Lax,
			Secure = IsHttps(payload.ReturnOrigin),
			MaxAge = StateLifetime,
			Path = "/api/v1",
		});
	}

	/// <summary>
	/// porovna callback state s cookie, jednorazove ho odstrani a vrati odpovidajici payload
	/// </summary>
	/// <param name="request">callback request obsahujici state cookie</param>
	/// <param name="provider">provider, pro ktereho musi byt state vytvoren</param>
	/// <param name="state">state hodnota vracena providerem</param>
	/// <param name="ct">token pro zruseni asynchronni operace</param>
	/// <returns>overeny payload, nebo null pri chybejicim, neshodnem, expirovanem ci cizim state</returns>
	internal async Task<Models.StatePayload?> ConsumeAsync(HttpRequest request, OAuthProvider provider, string? state, CancellationToken ct) {
		if (string.IsNullOrWhiteSpace(state)) return null;
		if (!request.Cookies.TryGetValue(StateCookieName, out var cookieState) ||
			!CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(state), Encoding.UTF8.GetBytes(cookieState))) {
			return null;
		}

		// stav se maze pred zpracovanim callbacku, aby sel pouzit jenom jednou
		var serializedState = await cache.GetStringAsync(GetStateCacheKey(state), ct);
		await cache.RemoveAsync(GetStateCacheKey(state), ct);
		request.HttpContext.Response.Cookies.Delete(StateCookieName, new CookieOptions { Path = "/api/v1" });
		if (string.IsNullOrWhiteSpace(serializedState)) return null;

		var payload = JsonSerializer.Deserialize<Models.StatePayload>(serializedState);
		return payload?.Provider == provider ? payload : null;
	}

	/// <summary>
	/// overi, zda zadany absolutni origin pouziva https schema
	/// </summary>
	/// <param name="origin">origin urceny ke kontrole</param>
	/// <returns>true pouze pro platny absolutni https origin</returns>
	internal static bool IsHttps(string origin) => Uri.TryCreate(origin, UriKind.Absolute, out var uri) && uri.Scheme == "https";

	/// <summary>
	/// sestavi namespaced cache klic pro oauth state
	/// </summary>
	/// <param name="state">nahodna state hodnota</param>
	/// <returns>cache klic oddeleny od ostatnich typu zaznamu</returns>
	private static string GetStateCacheKey(string state) => $"oauth-state:{state}";

	/// <summary>
	/// sdruzuje modely pouzivane pri vytvareni a ukladani oauth state
	/// </summary>
	internal static class Models {
		/// <summary>
		/// uchovava serverovou cast rozpracovaneho oauth flow v distribuovane cache
		/// </summary>
		/// <param name="AccountId">id prihlaseneho uctu pro connect flow</param>
		/// <param name="Provider">provider, pro ktereho byl state vytvoren</param>
		/// <param name="Flow">typ rozpracovaneho login nebo connect flow</param>
		/// <param name="CallbackUri">callback url pouzita pri zahajeni flow</param>
		/// <param name="ReturnOrigin">overeny frontend origin pro konecne presmerovani</param>
		/// <param name="CodeVerifier">tajna pkce hodnota pouzita pri vymene code za token</param>
		/// <param name="Nonce">hodnota pouzita pro overeni identity tokenu</param>
		internal sealed record StatePayload(
			Guid? AccountId,
			OAuthProvider Provider,
			OAuthFlow Flow,
			string CallbackUri,
			string ReturnOrigin,
			string CodeVerifier,
			string Nonce
		);

		/// <summary>
		/// sdruzuje nahodne bezpecnostni hodnoty vytvorene pro nove oauth flow
		/// </summary>
		/// <param name="State">jednorazova hodnota pro sparovani callbacku</param>
		/// <param name="CodeVerifier">tajna pkce hodnota ulozena na backendu</param>
		/// <param name="CodeChallenge">hash pkce verifieru odeslany providerovi</param>
		/// <param name="Nonce">jednorazova hodnota pro kontrolu identity tokenu</param>
		internal sealed record StateParameters(string State, string CodeVerifier, string CodeChallenge, string Nonce);
	}
}