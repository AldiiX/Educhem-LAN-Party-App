using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.DataProtection;
using server.Data.Entities;
using static server.Services.OAuth.ExternalAuthProviderBase.Models;
using static server.Services.OAuth.OAuthProviderBase.Models;
using static server.Services.OAuth.OAuthStateService.Models;

namespace server.Services.OAuth;

/// <summary>
/// implementuje discord oauth flow vcetne sifrovaneho ulozeni tokenu, refresh procesu, validace profilu a revoke requestu
/// </summary>
/// <param name="httpClientFactory">factory poskytujici http client pro discord api</param>
/// <param name="dataProtectionProvider">provider vytvarejici protector pro sifrovani ulozenych tokenu</param>
/// <param name="logger">logger pouzity pro zaznam selhani discord komunikace a desifrovani</param>
internal sealed class DiscordOAuthProvider(
	IHttpClientFactory httpClientFactory,
	IDataProtectionProvider dataProtectionProvider,
	ILogger<DiscordOAuthProvider> logger
) : OAuthProviderBase(httpClientFactory, logger) {
	private readonly IDataProtector tokenProtector = dataProtectionProvider.CreateProtector("discord-oauth-tokens");

	/// <inheritdoc />
	internal override OAuthProvider Provider => OAuthProvider.Discord;

	/// <inheritdoc />
	protected override string RouteSegment => "discord";

	/// <inheritdoc />
	protected override bool UsesBasicClientAuthentication => true;

	/// <inheritdoc />
	protected override bool MarksConnectionValidated => true;

	/// <inheritdoc />
	protected override ProviderConfig? GetConfig() => GetEnvironmentConfig(
		"DISCORD",
		"https://discord.com/oauth2/authorize",
		"https://discord.com/api/oauth2/token",
		"https://discord.com/api/v10/users/@me",
		"identify"
	);

	/// <inheritdoc />
	protected override Task<ProfileResult> GetProfileAsync(ProviderConfig config, TokenResponse tokens, StatePayload state, CancellationToken ct) =>
		string.IsNullOrWhiteSpace(tokens.AccessToken)
			? Task.FromResult(new ProfileResult(null, true))
			: GetBearerProfileAsync<DiscordUser>(config, tokens.AccessToken, ToProfile, ct);

	/// <summary>
	/// overi platnost ulozenych discord tokenu, podle potreby je obnovi a nacte aktualni profil stejne identity
	/// </summary>
	/// <param name="connection">ulozene discord spojeni s provider id a sifrovanymi tokeny</param>
	/// <param name="ct">token pro zruseni asynchronni operace</param>
	/// <returns>stav validace s aktualnim profilem a pripadne obnovenymi tokeny</returns>
	internal override async Task<ConnectionValidationResult> ValidateConnectionAsync(OAuthConnection connection, CancellationToken ct) {
		var accessToken = UnprotectToken(connection.AccessToken);
		var refreshToken = UnprotectToken(connection.RefreshToken);
		if (string.IsNullOrWhiteSpace(accessToken) || string.IsNullOrWhiteSpace(refreshToken)) {
			return new ConnectionValidationResult(ConnectionValidationStatus.Invalid);
		}

		TokenResponse? refreshedTokens = null;
		var usedRefresh = false;
		if ((connection.AccessTokenExpiresAtUtc ?? DateTime.MinValue) <= DateTime.UtcNow.AddMinutes(1)) {
			var refreshResult = await RefreshTokenAsync(refreshToken, ct);
			if (refreshResult.Tokens == null) {
				return new ConnectionValidationResult(refreshResult.Invalid ? ConnectionValidationStatus.Invalid : ConnectionValidationStatus.Unavailable);
			}
			refreshedTokens = refreshResult.Tokens;
			accessToken = refreshedTokens.AccessToken;
			usedRefresh = true;
		}

		var config = GetConfig();
		if (config == null || string.IsNullOrWhiteSpace(accessToken)) return new ConnectionValidationResult(ConnectionValidationStatus.Unavailable);
		var profileResult = await GetBearerProfileAsync<DiscordUser>(config, accessToken, ToProfile, ct);
		if (profileResult.TokenInvalid && !usedRefresh) {
			var refreshResult = await RefreshTokenAsync(refreshToken, ct);
			if (refreshResult.Tokens == null) {
				return new ConnectionValidationResult(refreshResult.Invalid ? ConnectionValidationStatus.Invalid : ConnectionValidationStatus.Unavailable);
			}
			refreshedTokens = refreshResult.Tokens;
			if (string.IsNullOrWhiteSpace(refreshedTokens.AccessToken)) return new ConnectionValidationResult(ConnectionValidationStatus.Invalid);
			profileResult = await GetBearerProfileAsync<DiscordUser>(config, refreshedTokens.AccessToken, ToProfile, ct);
		}

		if (profileResult.TokenInvalid || (profileResult.Profile != null && profileResult.Profile.UserId != connection.ProviderUserId)) {
			return new ConnectionValidationResult(ConnectionValidationStatus.Invalid);
		}
		if (profileResult.Profile == null) return new ConnectionValidationResult(ConnectionValidationStatus.Unavailable);

		return new ConnectionValidationResult(ConnectionValidationStatus.Valid, profileResult.Profile, refreshedTokens);
	}

	/// <summary>
	/// odesle discord revoke request pro refresh token ulozeneho spojeni
	/// </summary>
	/// <param name="connection">spojeni obsahujici sifrovany refresh token</param>
	/// <param name="ct">token pro zruseni asynchronni operace</param>
	/// <returns>ukol reprezentujici dokonceni revoke requestu</returns>
	internal override async Task RevokeConnectionAsync(OAuthConnection connection, CancellationToken ct) {
		var refreshToken = UnprotectToken(connection.RefreshToken);
		var config = GetConfig();
		if (string.IsNullOrWhiteSpace(refreshToken) || config == null) return;

		using var request = new HttpRequestMessage(HttpMethod.Post, "https://discord.com/api/oauth2/token/revoke") {
			Content = new FormUrlEncodedContent(new Dictionary<string, string> {
				["token"] = refreshToken,
				["token_type_hint"] = "refresh_token",
			}),
		};
		request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{config.ClientId}:{config.ClientSecret}")));

		try {
			using var response = await HttpClient.SendAsync(request, ct);
		} catch (HttpRequestException exception) {
			logger.LogWarning(exception, "Discord token revocation failed");
		}
	}

	/// <summary>
	/// zasifruje nove discord tokeny a ulozi jejich expiraci do entity spojeni
	/// </summary>
	/// <param name="connection">spojeni, do ktereho se credentials ukladaji</param>
	/// <param name="credentials">discord token response s access tokenem, refresh tokenem a expiraci</param>
	internal override void ApplyCredentials(OAuthConnection connection, TokenResponse credentials) {
		if (string.IsNullOrWhiteSpace(credentials.AccessToken) || string.IsNullOrWhiteSpace(credentials.RefreshToken) || credentials.ExpiresIn == null) return;
		connection.AccessToken = tokenProtector.Protect(credentials.AccessToken);
		connection.RefreshToken = tokenProtector.Protect(credentials.RefreshToken);
		connection.AccessTokenExpiresAtUtc = DateTime.UtcNow.AddSeconds(credentials.ExpiresIn.Value);
	}

	/// <summary>
	/// vymeni discord refresh token za novou sadu tokenu
	/// </summary>
	/// <param name="refreshToken">desifrovany refresh token ulozeneho spojeni</param>
	/// <param name="ct">token pro zruseni asynchronni operace</param>
	/// <returns>nove tokeny a informaci o pripadne neplatnosti refresh tokenu</returns>
	private async Task<TokenResult> RefreshTokenAsync(string refreshToken, CancellationToken ct) {
		var config = GetConfig();
		return config == null
			? new TokenResult(null, false)
			: await RequestTokenAsync(config, new Dictionary<string, string> {
				["grant_type"] = "refresh_token",
				["refresh_token"] = refreshToken,
			}, ct);
	}

	/// <summary>
	/// desifruje token ulozeny pomoci data protection
	/// </summary>
	/// <param name="protectedToken">sifrovana hodnota tokenu z databaze</param>
	/// <returns>puvodni token, nebo null pri prazdne hodnote ci chybe desifrovani</returns>
	private string? UnprotectToken(string? protectedToken) {
		if (string.IsNullOrWhiteSpace(protectedToken)) return null;
		try {
			return tokenProtector.Unprotect(protectedToken);
		} catch (CryptographicException exception) {
			logger.LogWarning(exception, "Discord token could not be decrypted");
			return null;
		}
	}

	/// <summary>
	/// prevede discord user response na normalizovany oauth profil
	/// </summary>
	/// <param name="user">discord user response</param>
	/// <returns>normalizovany profil, nebo null pri chybejici identite</returns>
	private static Profile? ToProfile(DiscordUser? user) => user == null || string.IsNullOrWhiteSpace(user.Id) || string.IsNullOrWhiteSpace(user.Username)
		? null
		: new Profile(user.Id, user.Username, GetAvatarUrl(user), null);

	/// <summary>
	/// sestavi discord cdn url pro staticky nebo animovany avatar uzivatele
	/// </summary>
	/// <param name="user">discord user obsahujici id a avatar hash</param>
	/// <returns>avatar url ve velikosti 256 px, nebo null bez avatar hashe</returns>
	private static string? GetAvatarUrl(DiscordUser user) {
		if (string.IsNullOrWhiteSpace(user.Avatar)) return null;
		var extension = user.Avatar.StartsWith("a_", StringComparison.Ordinal) ? "gif" : "png";
		return $"https://cdn.discordapp.com/avatars/{user.Id}/{user.Avatar}.{extension}?size=256";
	}

	private sealed class DiscordUser {
		[JsonPropertyName("id")]
		public string? Id { get; init; }

		[JsonPropertyName("username")]
		public string? Username { get; init; }

		[JsonPropertyName("avatar")]
		public string? Avatar { get; init; }
	}
}