using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json.Serialization;
using AspNet.Security.OpenId.Steam;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.WebUtilities;
using server.Data.Entities;

namespace server.Services.OAuth.Platforms;

/// <summary>
/// implementuje integraci pro platformu Steam pres OpenID 2.0 a Steam Web API
/// </summary>
internal sealed class SteamOAuthPlatform(
	IHttpClientFactory httpClientFactory,
	ILogger<SteamOAuthPlatform> logger
) : IOAuthPlatform {
	private HttpClient HttpClient => httpClientFactory.CreateClient("oauth-external");

	/// <inheritdoc />
	public OAuthProvider Provider => OAuthProvider.Steam;

	/// <inheritdoc />
	public string Scheme => SteamAuthenticationDefaults.AuthenticationScheme;

	/// <inheritdoc />
	public bool IsConfigured => HasEnv("STEAM_WEB_API_KEY");

	/// <summary>
	/// registruje steam openid providera do ASP.NET Core authentication builderu
	/// </summary>
	/// <param name="builder">authentication builder pro registraci handleru</param>
	public static void ConfigureAuthentication(AuthenticationBuilder builder) {
		if (!Program.ENV.TryGetValue("STEAM_WEB_API_KEY", out var steamKey) || string.IsNullOrWhiteSpace(steamKey)) {
			return;
		}

		builder.AddSteam(options => {
			options.ApplicationKey = steamKey;
			options.CallbackPath = "/api/v1/steam/callback";
			options.SignInScheme = "ExternalCookie";
			options.Events.OnRemoteFailure = OAuthEventsHelper.HandleRemoteFailure;
		});
	}

	/// <inheritdoc />
	public ExtractedOAuthProfile ExtractProfile(ClaimsPrincipal principal, AuthenticationProperties properties) {
		var rawId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";
		var providerUserId = rawId.Contains("/openid/id/") ? rawId[(rawId.LastIndexOf('/') + 1)..] : rawId;
		var username = principal.FindFirst(ClaimTypes.Name)?.Value ?? "Steam účet";
		var avatarUrl = principal.FindFirst("urn:steam:avatarfull")?.Value
			?? principal.FindFirst("urn:steam:avatarmedium")?.Value
			?? principal.FindFirst("urn:steam:avatar")?.Value;
		var profileUrl = principal.FindFirst("urn:steam:profileurl")?.Value
			?? $"https://steamcommunity.com/profiles/{providerUserId}/";

		return new ExtractedOAuthProfile(providerUserId, username, avatarUrl, profileUrl);
	}

	/// <inheritdoc />
	public async Task<PlatformValidationResult> ValidateConnectionAsync(OAuthConnection connection, CancellationToken ct) {
		if (!IsConfigured) return new PlatformValidationResult(PlatformValidationStatus.Unavailable);

		var player = await FetchPlayerAsync(connection.ProviderUserId, ct);
		if (player == null) return new PlatformValidationResult(PlatformValidationStatus.Unavailable);

		var username = string.IsNullOrWhiteSpace(player.PersonaName) ? connection.Username : player.PersonaName;
		var profileUrl = string.IsNullOrWhiteSpace(player.ProfileUrl) ? connection.ProfileUrl : player.ProfileUrl;
		var avatar = player.AvatarFull ?? player.AvatarMedium ?? player.Avatar;

		return new PlatformValidationResult(
			PlatformValidationStatus.Valid,
			username,
			avatar,
			profileUrl
		);
	}

	/// <inheritdoc />
	public Task RevokeConnectionAsync(OAuthConnection connection, CancellationToken ct) => Task.CompletedTask;

	/// <summary>
	/// stahne aktualni verejny profil hrace pres steam web api
	/// </summary>
	/// <param name="steamId">steamid64 hrace</param>
	/// <param name="ct">token pro zruseni asynchronni operace</param>
	/// <returns>profil hrace, nebo null pri chybe</returns>
	private async Task<SteamUser?> FetchPlayerAsync(string steamId, CancellationToken ct) {
		if (!IsConfigured) return null;

		var endpoint = QueryHelpers.AddQueryString("https://api.steampowered.com/ISteamUser/GetPlayerSummaries/v2/", "steamids", steamId);
		using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
		request.Headers.TryAddWithoutValidation("x-webapi-key", Program.ENV["STEAM_WEB_API_KEY"]);
		request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

		try {
			using var response = await HttpClient.SendAsync(request, ct);
			if (!response.IsSuccessStatusCode) return null;
			var result = await response.Content.ReadFromJsonAsync<SteamPlayerSummariesResponse>(cancellationToken: ct);
			return result?.Response?.Players.FirstOrDefault(item => item.SteamId == steamId);
		} catch (HttpRequestException ex) {
			logger.LogWarning(ex, "Steam player fetch failed");
			return null;
		}
	}

	/// <summary>
	/// overuje pritomnost neprazdne promenne v konfiguraci prostredi
	/// </summary>
	/// <param name="key">nazev environment promenne</param>
	/// <returns>true pokud promenna existuje a neni prazdna</returns>
	private static bool HasEnv(string key) => Program.ENV.TryGetValue(key, out var val) && !string.IsNullOrWhiteSpace(val);

	/// <summary>
	/// mapuje obalku odpovedi steam web api
	/// </summary>
	private sealed class SteamPlayerSummariesResponse {
		[JsonPropertyName("response")]
		public SteamPlayerSummariesBody? Response { get; init; }
	}

	/// <summary>
	/// mapuje pole hracu ve steam web api odpovedi
	/// </summary>
	private sealed class SteamPlayerSummariesBody {
		[JsonPropertyName("players")]
		public List<SteamUser> Players { get; init; } = [];
	}

	/// <summary>
	/// mapuje data hrace ze steam web api
	/// </summary>
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