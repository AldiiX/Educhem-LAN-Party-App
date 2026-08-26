using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.WebUtilities;
using server.Data.Entities;
using static server.Services.OAuth.ExternalAuthProviderBase.Models;
using static server.Services.OAuth.OAuthStateService.Models;

namespace server.Services.OAuth;

/// <summary>
/// implementuje steam openid autentizaci a nacita verejny profil pres steam web api
/// </summary>
/// <param name="httpClientFactory">factory poskytujici http client pro steam openid a web api requesty</param>
/// <param name="logger">logger pouzity pro zaznam selhani steam komunikace</param>
internal sealed class SteamOpenIdProvider(IHttpClientFactory httpClientFactory, ILogger<SteamOpenIdProvider> logger)
	: ExternalAuthProviderBase {
	private const string OpenIdEndpoint = "https://steamcommunity.com/openid/login";
	private const string ClaimedIdPrefix = "https://steamcommunity.com/openid/id/";
	private const string ProfileEndpoint = "https://api.steampowered.com/ISteamUser/GetPlayerSummaries/v2/";

	/// <inheritdoc />
	internal override OAuthProvider Provider => OAuthProvider.Steam;

	/// <inheritdoc />
	protected override string RouteSegment => "steam";

	/// <inheritdoc />
	internal override bool RequiresHttps => false;

	/// <inheritdoc />
	internal override bool IsConfigured => GetWebApiKey() != null;

	/// <summary>
	/// vytvori sdileny http client pro steam request
	/// </summary>
	private HttpClient HttpClient => httpClientFactory.CreateClient(HttpClientName);

	/// <inheritdoc />
	internal override string BuildCallbackUri(string frontendOrigin, string state) =>
		QueryHelpers.AddQueryString($"{frontendOrigin}/api/v1/{RouteSegment}/callback", "state", state);

	/// <inheritdoc />
	internal override Uri? CreateAuthorizationUri(AuthorizationContext context) => !IsConfigured
		? null
		: new Uri(QueryHelpers.AddQueryString(OpenIdEndpoint, new Dictionary<string, string?> {
			["openid.ns"] = "http://specs.openid.net/auth/2.0",
			["openid.mode"] = "checkid_setup",
			["openid.return_to"] = context.CallbackUri,
			["openid.realm"] = $"{context.FrontendOrigin}/",
			["openid.identity"] = "http://specs.openid.net/auth/2.0/identifier_select",
			["openid.claimed_id"] = "http://specs.openid.net/auth/2.0/identifier_select",
		}));

	/// <summary>
	/// overi steam openid callback a nacte profil identity potvrzene steam endpointem
	/// </summary>
	/// <param name="request">callback request obsahujici openid parametry</param>
	/// <param name="state">overeny oauth state s ocekavanou return url</param>
	/// <param name="code">nepouzity authorization code parametr spolecneho provider kontraktu</param>
	/// <param name="error">nepouzity error parametr spolecneho provider kontraktu</param>
	/// <param name="ct">token pro zruseni asynchronni operace</param>
	/// <returns>normalizovany vysledek obsahujici overene steam id a verejny profil</returns>
	internal override async Task<AuthorizationResult> CompleteAuthorizationAsync(HttpRequest request, StatePayload state, string? code, string? error, CancellationToken ct) {
		var mode = request.Query["openid.mode"].ToString();
		if (mode == "cancel") return new AuthorizationResult(AuthorizationStatus.Cancelled);
		if (mode != "id_res") return new AuthorizationResult(AuthorizationStatus.Failed);

		var steamId = await VerifyResponseAsync(request, state.CallbackUri, ct);
		if (steamId == null) return new AuthorizationResult(AuthorizationStatus.Failed);

		var profile = await GetProfileAsync(steamId, ct);
		return new AuthorizationResult(
			AuthorizationStatus.Succeeded,
			steamId,
			profile,
			null,
			profile != null
		);
	}

	/// <summary>
	/// overi ulozene steam spojeni nactenim aktualniho profilu stejne identity
	/// </summary>
	/// <param name="connection">ulozene steam spojeni s provider id</param>
	/// <param name="ct">token pro zruseni asynchronni operace</param>
	/// <returns>platny stav s profilem nebo stav docasne nedostupne sluzby</returns>
	internal override async Task<ConnectionValidationResult> ValidateConnectionAsync(OAuthConnection connection, CancellationToken ct) {
		var profile = await GetProfileAsync(connection.ProviderUserId, ct);
		// neuspesne nacteni profilu nedokazuje neplatnost identity, proto spojeni zustava zachovane
		return profile == null || profile.UserId != connection.ProviderUserId
			? new ConnectionValidationResult(ConnectionValidationStatus.Unavailable)
			: new ConnectionValidationResult(ConnectionValidationStatus.Valid, profile);
	}

	/// <summary>
	/// potvrdi openid callback zpet u steamu a vytahne steam id z overene claimed identity
	/// </summary>
	/// <param name="request">callback request obsahujici openid parametry</param>
	/// <param name="expectedReturnTo">callback url ulozena pri zahajeni flow</param>
	/// <param name="ct">token pro zruseni asynchronni operace</param>
	/// <returns>overene steam id, nebo null pri jakekoliv neshode ci chybe overeni</returns>
	private async Task<string?> VerifyResponseAsync(HttpRequest request, string expectedReturnTo, CancellationToken ct) {
		var values = request.Query
			.Where(item => item.Key.StartsWith("openid.", StringComparison.Ordinal))
			.ToDictionary(item => item.Key, item => item.Value.ToString(), StringComparer.Ordinal);
		if (!values.TryGetValue("openid.op_endpoint", out var endpoint) || endpoint != OpenIdEndpoint) return null;
		if (!values.TryGetValue("openid.return_to", out var returnTo) || returnTo != expectedReturnTo) return null;
		if (!values.TryGetValue("openid.claimed_id", out var claimedId) || !values.TryGetValue("openid.identity", out var identity) || identity != claimedId) return null;
		if (!claimedId.StartsWith(ClaimedIdPrefix, StringComparison.Ordinal)) return null;

		var steamId = claimedId[ClaimedIdPrefix.Length..];
		if (!ulong.TryParse(steamId, out var parsedSteamId) || parsedSteamId == 0 || claimedId != $"{ClaimedIdPrefix}{parsedSteamId}") return null;

		values["openid.mode"] = "check_authentication";
		using var verificationRequest = new HttpRequestMessage(HttpMethod.Post, OpenIdEndpoint) {
			Content = new FormUrlEncodedContent(values),
		};
		try {
			using var response = await HttpClient.SendAsync(verificationRequest, ct);
			if (!response.IsSuccessStatusCode) return null;
			var body = await response.Content.ReadAsStringAsync(ct);
			return body.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
				.Any(line => line.Equals("is_valid:true", StringComparison.Ordinal))
				? steamId
				: null;
		} catch (HttpRequestException exception) {
			logger.LogWarning(exception, "Steam OpenID verification failed");
			return null;
		} catch (OperationCanceledException exception) when (!ct.IsCancellationRequested) {
			logger.LogWarning(exception, "Steam OpenID verification timed out");
			return null;
		}
	}

	/// <summary>
	/// nacte verejny steam profil pro zadane steam id
	/// </summary>
	/// <param name="steamId">overene steam id pozadovaneho uzivatele</param>
	/// <param name="ct">token pro zruseni asynchronni operace</param>
	/// <returns>normalizovany profil, nebo null pri chybe konfigurace, requestu ci mapovani</returns>
	private async Task<Profile?> GetProfileAsync(string steamId, CancellationToken ct) {
		var apiKey = GetWebApiKey();
		if (apiKey == null) return null;

		var endpoint = QueryHelpers.AddQueryString(ProfileEndpoint, "steamids", steamId);
		using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
		request.Headers.TryAddWithoutValidation("x-webapi-key", apiKey);
		request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

		try {
			using var response = await HttpClient.SendAsync(request, ct);
			if (!response.IsSuccessStatusCode) return null;
			var result = await response.Content.ReadFromJsonAsync<SteamPlayerSummariesResponse>(cancellationToken: ct);
			var user = result?.Response?.Players.FirstOrDefault(item => item.SteamId == steamId);
			if (user == null) return null;

			var username = string.IsNullOrWhiteSpace(user.PersonaName) ? "Steam účet" : user.PersonaName;
			var profileUrl = string.IsNullOrWhiteSpace(user.ProfileUrl) ? $"https://steamcommunity.com/profiles/{steamId}/" : user.ProfileUrl;
			var avatarUrl = user.AvatarFull ?? user.AvatarMedium ?? user.Avatar;
			return new Profile(steamId, username, avatarUrl, profileUrl);
		} catch (HttpRequestException exception) {
			logger.LogWarning(exception, "Steam profile request failed");
			return null;
		} catch (JsonException exception) {
			logger.LogWarning(exception, "Steam profile response was invalid");
			return null;
		} catch (OperationCanceledException exception) when (!ct.IsCancellationRequested) {
			logger.LogWarning(exception, "Steam profile request timed out");
			return null;
		}
	}

	/// <summary>
	/// nacte steam web api key z environment konfigurace
	/// </summary>
	/// <returns>neprazdny api key, nebo null pri chybejici konfiguraci</returns>
	private static string? GetWebApiKey() => Program.ENV.TryGetValue("STEAM_WEB_API_KEY", out var apiKey) && !string.IsNullOrWhiteSpace(apiKey)
		? apiKey
		: null;

	private sealed class SteamPlayerSummariesResponse {
		[JsonPropertyName("response")]
		public SteamPlayerSummariesBody? Response { get; init; }
	}

	private sealed class SteamPlayerSummariesBody {
		[JsonPropertyName("players")]
		public List<SteamUser> Players { get; init; } = [];
	}

	private sealed class SteamUser {
		[JsonPropertyName("steamid")]
		public string? SteamId { get; init; }

		[JsonPropertyName("personaname")]
		public string? PersonaName { get; init; }

		[JsonPropertyName("profileurl")]
		public string? ProfileUrl { get; init; }

		[JsonPropertyName("avatar")]
		public string? Avatar { get; init; }

		[JsonPropertyName("avatarmedium")]
		public string? AvatarMedium { get; init; }

		[JsonPropertyName("avatarfull")]
		public string? AvatarFull { get; init; }
	}
}