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

	public OAuthProvider Provider => OAuthProvider.Steam;
	public string Scheme => SteamAuthenticationDefaults.AuthenticationScheme;
	public bool IsConfigured => HasEnv("STEAM_WEB_API_KEY");

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

	public Task RevokeConnectionAsync(OAuthConnection connection, CancellationToken ct) => Task.CompletedTask;

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

	private static bool HasEnv(string key) => Program.ENV.TryGetValue(key, out var val) && !string.IsNullOrWhiteSpace(val);

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
