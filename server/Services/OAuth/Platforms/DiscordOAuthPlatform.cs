using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using AspNet.Security.OAuth.Discord;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using server.Data.Entities;

namespace server.Services.OAuth.Platforms;

/// <summary>
/// implementuje integraci pro platformu Discord
/// </summary>
internal sealed class DiscordOAuthPlatform(
	IDataProtectionProvider dataProtectionProvider,
	IHttpClientFactory httpClientFactory,
	ILogger<DiscordOAuthPlatform> logger
) : IOAuthPlatform {
	private readonly IDataProtector tokenProtector = dataProtectionProvider.CreateProtector("discord-oauth-tokens");
	private HttpClient HttpClient => httpClientFactory.CreateClient("oauth-external");

	/// <inheritdoc />
	public OAuthProvider Provider => OAuthProvider.Discord;

	/// <inheritdoc />
	public string Scheme => DiscordAuthenticationDefaults.AuthenticationScheme;

	/// <inheritdoc />
	public bool IsConfigured => HasEnv("DISCORD_CLIENT_ID") && HasEnv("DISCORD_CLIENT_SECRET");

	/// <summary>
	/// registruje discord providera do ASP.NET Core authentication builderu
	/// </summary>
	/// <param name="builder">authentication builder pro registraci handleru</param>
	public static void ConfigureAuthentication(AuthenticationBuilder builder) {
		if (!Program.ENV.TryGetValue("DISCORD_CLIENT_ID", out var clientId) || string.IsNullOrWhiteSpace(clientId) ||
			!Program.ENV.TryGetValue("DISCORD_CLIENT_SECRET", out var clientSecret) || string.IsNullOrWhiteSpace(clientSecret)) {
			return;
		}

		builder.AddDiscord(options => {
			options.ClientId = clientId;
			options.ClientSecret = clientSecret;
			options.CallbackPath = "/api/v1/discord/callback";
			options.SignInScheme = "ExternalCookie";
			options.SaveTokens = true;
			options.Scope.Add("identify");
			options.Events.OnRemoteFailure = OAuthEventsHelper.HandleRemoteFailure;
		});
	}

	/// <inheritdoc />
	public ExtractedOAuthProfile ExtractProfile(ClaimsPrincipal principal, AuthenticationProperties properties) {
		var providerUserId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";
		var username = principal.FindFirst(ClaimTypes.Name)?.Value
			?? principal.FindFirst("urn:discord:username")?.Value
			?? "Discord účet";

		string? avatarUrl = null;
		var avatarHash = principal.FindFirst("urn:discord:avatar:hash")?.Value;
		if (!string.IsNullOrWhiteSpace(avatarHash) && !string.IsNullOrWhiteSpace(providerUserId)) {
			var ext = avatarHash.StartsWith("a_", StringComparison.Ordinal) ? "gif" : "png";
			avatarUrl = $"https://cdn.discordapp.com/avatars/{providerUserId}/{avatarHash}.{ext}?size=256";
		}

		var tokens = properties.GetTokens()?.ToList() ?? [];
		var accessToken = tokens.FirstOrDefault(t => t.Name == "access_token")?.Value;
		var refreshToken = tokens.FirstOrDefault(t => t.Name == "refresh_token")?.Value;
		var expiresAtStr = tokens.FirstOrDefault(t => t.Name == "expires_at")?.Value;
		DateTime? expiresAtUtc = DateTime.TryParse(expiresAtStr, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var exp)
			? exp.ToUniversalTime()
			: null;

		return new ExtractedOAuthProfile(providerUserId, username, avatarUrl, null, accessToken, refreshToken, expiresAtUtc);
	}

	/// <inheritdoc />
	public async Task<PlatformValidationResult> ValidateConnectionAsync(OAuthConnection connection, CancellationToken ct) {
		if (!IsConfigured) return new PlatformValidationResult(PlatformValidationStatus.Unavailable);

		var accessToken = UnprotectToken(connection.AccessToken);
		var refreshToken = UnprotectToken(connection.RefreshToken);
		if (string.IsNullOrWhiteSpace(accessToken) || string.IsNullOrWhiteSpace(refreshToken)) {
			return new PlatformValidationResult(PlatformValidationStatus.Invalid);
		}

		var usedRefresh = false;
		if ((connection.AccessTokenExpiresAtUtc ?? DateTime.MinValue) <= DateTime.UtcNow.AddMinutes(1)) {
			var refreshResult = await RefreshTokenAsync(refreshToken, ct);
			if (refreshResult == null || string.IsNullOrWhiteSpace(refreshResult.AccessToken)) {
				return new PlatformValidationResult(PlatformValidationStatus.Invalid);
			}
			accessToken = refreshResult.AccessToken;
			refreshToken = refreshResult.RefreshToken ?? refreshToken;
			ApplyTokens(connection, refreshResult);
			usedRefresh = true;
		}

		var discordUser = await FetchUserAsync(accessToken, ct);
		if (discordUser == null && !usedRefresh) {
			var refreshResult = await RefreshTokenAsync(refreshToken, ct);
			if (refreshResult != null && !string.IsNullOrWhiteSpace(refreshResult.AccessToken)) {
				accessToken = refreshResult.AccessToken;
				ApplyTokens(connection, refreshResult);
				discordUser = await FetchUserAsync(accessToken, ct);
			}
		}

		if (discordUser == null || discordUser.Id != connection.ProviderUserId) {
			return new PlatformValidationResult(discordUser != null ? PlatformValidationStatus.Invalid : PlatformValidationStatus.Unavailable);
		}

		string? avatarUrl = null;
		if (!string.IsNullOrWhiteSpace(discordUser.Avatar)) {
			var ext = discordUser.Avatar.StartsWith("a_", StringComparison.Ordinal) ? "gif" : "png";
			avatarUrl = $"https://cdn.discordapp.com/avatars/{discordUser.Id}/{discordUser.Avatar}.{ext}?size=256";
		}

		return new PlatformValidationResult(
			PlatformValidationStatus.Valid,
			discordUser.Username ?? connection.Username,
			avatarUrl
		);
	}

	/// <inheritdoc />
	public async Task RevokeConnectionAsync(OAuthConnection connection, CancellationToken ct) {
		var refreshToken = UnprotectToken(connection.RefreshToken);
		if (string.IsNullOrWhiteSpace(refreshToken) || !IsConfigured) return;

		using var request = new HttpRequestMessage(HttpMethod.Post, "https://discord.com/api/oauth2/token/revoke") {
			Content = new FormUrlEncodedContent(new Dictionary<string, string> {
				["token"] = refreshToken,
				["token_type_hint"] = "refresh_token",
			}),
		};
		request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{Program.ENV["DISCORD_CLIENT_ID"]}:{Program.ENV["DISCORD_CLIENT_SECRET"]}")));

		try {
			using var response = await HttpClient.SendAsync(request, ct);
		} catch (HttpRequestException ex) {
			logger.LogWarning(ex, "Discord token revocation failed");
		}
	}

	/// <summary>
	/// zasifruje a ulozi pristupove a refresh tokeny do entity spojeni
	/// </summary>
	/// <param name="connection">spojeni, do ktereho se tokeny ukladaji</param>
	/// <param name="accessToken">nezasifrovany access token</param>
	/// <param name="refreshToken">nezasifrovany refresh token</param>
	/// <param name="expiresAtUtc">expirace access tokenu v utc</param>
	public void ApplyTokens(OAuthConnection connection, string? accessToken, string? refreshToken, DateTime? expiresAtUtc) {
		if (!string.IsNullOrWhiteSpace(accessToken)) connection.AccessToken = tokenProtector.Protect(accessToken);
		if (!string.IsNullOrWhiteSpace(refreshToken)) connection.RefreshToken = tokenProtector.Protect(refreshToken);
		connection.AccessTokenExpiresAtUtc = expiresAtUtc;
	}

	/// <summary>
	/// zasifruje a ulozi tokeny ziskane z token response
	/// </summary>
	/// <param name="connection">spojeni, do ktereho se tokeny ukladaji</param>
	/// <param name="tokens">token response ziskana z discord api</param>
	private void ApplyTokens(OAuthConnection connection, DiscordTokenResponse tokens) {
		if (!string.IsNullOrWhiteSpace(tokens.AccessToken)) connection.AccessToken = tokenProtector.Protect(tokens.AccessToken);
		if (!string.IsNullOrWhiteSpace(tokens.RefreshToken)) connection.RefreshToken = tokenProtector.Protect(tokens.RefreshToken);
		if (tokens.ExpiresIn.HasValue) connection.AccessTokenExpiresAtUtc = DateTime.UtcNow.AddSeconds(tokens.ExpiresIn.Value);
	}

	/// <summary>
	/// desifruje ulozeny token pomoci data protection
	/// </summary>
	/// <param name="protectedToken">sifrovany token z databaze</param>
	/// <returns>puvodni nezasifrovany token, nebo null pri chybe</returns>
	private string? UnprotectToken(string? protectedToken) {
		if (string.IsNullOrWhiteSpace(protectedToken)) return null;
		try {
			return tokenProtector.Unprotect(protectedToken);
		} catch (CryptographicException ex) {
			logger.LogWarning(ex, "Failed to decrypt Discord OAuth token");
			return null;
		}
	}

	/// <summary>
	/// vymeni refresh token za novou sadu tokenu pres discord oauth token endpoint
	/// </summary>
	/// <param name="refreshToken">desifrovany refresh token</param>
	/// <param name="ct">token pro zruseni asynchronni operace</param>
	/// <returns>token response, nebo null pri chybe</returns>
	private async Task<DiscordTokenResponse?> RefreshTokenAsync(string refreshToken, CancellationToken ct) {
		if (!IsConfigured) return null;

		using var request = new HttpRequestMessage(HttpMethod.Post, "https://discord.com/api/oauth2/token") {
			Content = new FormUrlEncodedContent(new Dictionary<string, string> {
				["grant_type"] = "refresh_token",
				["refresh_token"] = refreshToken,
			}),
		};
		request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{Program.ENV["DISCORD_CLIENT_ID"]}:{Program.ENV["DISCORD_CLIENT_SECRET"]}")));
		request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

		try {
			using var response = await HttpClient.SendAsync(request, ct);
			if (!response.IsSuccessStatusCode) return null;
			return await response.Content.ReadFromJsonAsync<DiscordTokenResponse>(cancellationToken: ct);
		} catch (HttpRequestException ex) {
			logger.LogWarning(ex, "Discord token refresh failed");
			return null;
		}
	}

	/// <summary>
	/// stahne profil prihlaseneho discord uzivatele pres users/@me endpoint
	/// </summary>
	/// <param name="accessToken">platny bearer access token</param>
	/// <param name="ct">token pro zruseni asynchronni operace</param>
	/// <returns>discord user profil, nebo null pri chybe</returns>
	private async Task<DiscordUser?> FetchUserAsync(string accessToken, CancellationToken ct) {
		using var request = new HttpRequestMessage(HttpMethod.Get, "https://discord.com/api/v10/users/@me");
		request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
		request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

		try {
			using var response = await HttpClient.SendAsync(request, ct);
			if (!response.IsSuccessStatusCode) return null;
			return await response.Content.ReadFromJsonAsync<DiscordUser>(cancellationToken: ct);
		} catch (HttpRequestException ex) {
			logger.LogWarning(ex, "Discord user fetch failed");
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
	/// mapuje token response z discord token endpointu
	/// </summary>
	private sealed class DiscordTokenResponse {
		[JsonPropertyName("access_token")]
		public string? AccessToken { get; init; }

		[JsonPropertyName("refresh_token")]
		public string? RefreshToken { get; init; }

		[JsonPropertyName("expires_in")]
		public int? ExpiresIn { get; init; }
	}

	/// <summary>
	/// mapuje odpoved z discord users/@me endpointu
	/// </summary>
	private sealed class DiscordUser {
		[JsonPropertyName("id")]
		public string? Id { get; init; }

		[JsonPropertyName("username")]
		public string? Username { get; init; }

		[JsonPropertyName("avatar")]
		public string? Avatar { get; init; }
	}
}
