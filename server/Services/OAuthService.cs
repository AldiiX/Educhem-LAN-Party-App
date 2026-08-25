using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using server.Data;
using server.Data.Entities;

namespace server.Services;

public sealed class OAuthService(
	AppDbContext db,
	HttpClient httpClient,
	IDistributedCache cache,
	IDataProtectionProvider dataProtectionProvider,
	ILogger<OAuthService> logger,
	IDbLoggerService dbLogger
) : IOAuthService {
	private const string StateCookieName = "educhemlanparty_oauth_state";
	private const string SteamOpenIdEndpoint = "https://steamcommunity.com/openid/login";
	private const string SteamClaimedIdPrefix = "https://steamcommunity.com/openid/id/";
	private const string SteamProfileEndpoint = "https://api.steampowered.com/ISteamUser/GetPlayerSummaries/v2/";
	private static readonly TimeSpan StateLifetime = TimeSpan.FromMinutes(10);
	private static readonly TimeSpan ValidationInterval = TimeSpan.FromMinutes(15);
	private readonly IDataProtector discordTokenProtector = dataProtectionProvider.CreateProtector("discord-oauth-tokens");

	#region Spolecny OAuth flow

	public async Task<Uri?> CreateAuthorizationUrlAsync(Guid? accountId, OAuthProvider provider, OAuthFlow flow, HttpRequest request, CancellationToken ct = default) {
		var frontendOrigin = GetFrontendOrigin();
		if (frontendOrigin == null) return null;
		if (flow == OAuthFlow.Connect && accountId == null) return null;
		var routeSegment = GetRouteSegment(provider);
		if (routeSegment == null) return null;

		var config = provider == OAuthProvider.Steam ? null : GetProviderConfig(provider);
		if (provider != OAuthProvider.Steam && config == null) return null;
		if (provider == OAuthProvider.Steam && GetSteamWebApiKey() == null) return null;

		var state = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
		var codeVerifier = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
		var codeChallenge = WebEncoders.Base64UrlEncode(SHA256.HashData(Encoding.UTF8.GetBytes(codeVerifier)));
		var callbackUri = $"{frontendOrigin}/api/v1/{routeSegment}/callback";
		if (provider == OAuthProvider.Steam) callbackUri = QueryHelpers.AddQueryString(callbackUri, "state", state);
		var payload = new OAuthState(accountId, provider, flow, callbackUri, frontendOrigin, codeVerifier);
		await cache.SetStringAsync(GetStateCacheKey(state), JsonSerializer.Serialize(payload), new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = StateLifetime }, ct);

		request.HttpContext.Response.Cookies.Append(StateCookieName, state, new CookieOptions {
			HttpOnly = true,
			IsEssential = true,
			SameSite = SameSiteMode.Lax,
			Secure = IsHttps(frontendOrigin),
			MaxAge = StateLifetime,
			Path = "/api/v1",
		});

		if (provider == OAuthProvider.Steam) {
			return new Uri(QueryHelpers.AddQueryString(SteamOpenIdEndpoint, new Dictionary<string, string?> {
				["openid.ns"] = "http://specs.openid.net/auth/2.0",
				["openid.mode"] = "checkid_setup",
				["openid.return_to"] = callbackUri,
				["openid.realm"] = $"{frontendOrigin}/",
				["openid.identity"] = "http://specs.openid.net/auth/2.0/identifier_select",
				["openid.claimed_id"] = "http://specs.openid.net/auth/2.0/identifier_select",
			}));
		}

		return new Uri(QueryHelpers.AddQueryString(config!.AuthorizationEndpoint, new Dictionary<string, string?> {
			["client_id"] = config.ClientId,
			["response_type"] = "code",
			["redirect_uri"] = callbackUri,
			["state"] = state,
			["scope"] = config.Scope,
			["code_challenge"] = codeChallenge,
			["code_challenge_method"] = "S256",
		}));
	}

	public async Task<OAuthCompletion> CompleteAuthorizationAsync(HttpRequest request, OAuthProvider provider, string? state, string? code, string? error, CancellationToken ct = default) {
		if (string.IsNullOrWhiteSpace(state)) return new OAuthCompletion(OAuthCompletionKind.InvalidState);
		if (!request.Cookies.TryGetValue(StateCookieName, out var cookieState) || !CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(state), Encoding.UTF8.GetBytes(cookieState))) {
			return new OAuthCompletion(OAuthCompletionKind.InvalidState);
		}

		var serializedState = await cache.GetStringAsync(GetStateCacheKey(state), ct);
		await cache.RemoveAsync(GetStateCacheKey(state), ct);
		request.HttpContext.Response.Cookies.Delete(StateCookieName, new CookieOptions { Path = "/api/v1" });
		if (string.IsNullOrWhiteSpace(serializedState)) return new OAuthCompletion(OAuthCompletionKind.InvalidState);

		var oauthState = JsonSerializer.Deserialize<OAuthState>(serializedState);
		if (oauthState == null || oauthState.Provider != provider) return new OAuthCompletion(OAuthCompletionKind.InvalidState);
		if (provider == OAuthProvider.Steam) return await CompleteSteamAuthorizationAsync(request, oauthState, ct);
		if (!string.IsNullOrWhiteSpace(error) || string.IsNullOrWhiteSpace(code)) return new OAuthCompletion(OAuthCompletionKind.Cancelled, oauthState.ReturnOrigin);

		var config = GetProviderConfig(provider);
		if (config == null) return new OAuthCompletion(OAuthCompletionKind.Failed, oauthState.ReturnOrigin);
		var tokenResult = await RequestTokenAsync(config, new Dictionary<string, string> {
			["grant_type"] = "authorization_code",
			["code"] = code,
			["redirect_uri"] = oauthState.CallbackUri,
			["code_verifier"] = oauthState.CodeVerifier,
		}, ct);
		if (tokenResult.Tokens?.AccessToken is not { Length: > 0 } accessToken) return new OAuthCompletion(OAuthCompletionKind.Failed, oauthState.ReturnOrigin);
		var profileResult = await GetProfileAsync(config, accessToken!, ct);
		if (profileResult.Profile == null) return new OAuthCompletion(OAuthCompletionKind.Failed, oauthState.ReturnOrigin);
		var profile = profileResult.Profile;

		if (oauthState.Flow == OAuthFlow.Login) {
			var connection = await db.OAuthConnections
				.Include(item => item.Account)
				.FirstOrDefaultAsync(item => item.Provider == provider && item.ProviderUserId == profile.UserId, ct);
			if (connection == null) return new OAuthCompletion(OAuthCompletionKind.LoginNotLinked, oauthState.ReturnOrigin);

			ApplyConnection(connection.Account, connection, profile, tokenResult.Tokens);
			await db.SaveChangesAsync(ct);
			return new OAuthCompletion(OAuthCompletionKind.LoginSucceeded, oauthState.ReturnOrigin, connection.AccountId);
		}

		if (oauthState.AccountId == null) return new OAuthCompletion(OAuthCompletionKind.InvalidState, oauthState.ReturnOrigin);
		var account = await db.Accounts
			.Include(item => item.OAuthConnections)
			.FirstOrDefaultAsync(item => item.Id == oauthState.AccountId.Value, ct);
		if (account == null) return new OAuthCompletion(OAuthCompletionKind.Failed, oauthState.ReturnOrigin);

		var alreadyConnected = await db.OAuthConnections.AsNoTracking()
			.FirstOrDefaultAsync(item => item.Provider == provider && item.ProviderUserId == profile.UserId && item.AccountId != account.Id, ct);
		if (alreadyConnected != null) return new OAuthCompletion(OAuthCompletionKind.AlreadyLinked, oauthState.ReturnOrigin);

		var connectionForAccount = account.OAuthConnections.FirstOrDefault(item => item.Provider == provider);
		var isNewConnection = connectionForAccount == null;
		if (connectionForAccount == null) {
			connectionForAccount = new OAuthConnection {
				AccountId = account.Id,
				Account = account,
				Provider = provider,
				ProviderUserId = profile.UserId,
				Username = profile.Username,
			};
			db.OAuthConnections.Add(connectionForAccount);
		}

		ApplyConnection(account, connectionForAccount, profile, tokenResult.Tokens);
		await db.SaveChangesAsync(ct);
		if (isNewConnection) await dbLogger.LogInfoAsync($"Účet {FormatAccount(account)} propojil platformu {provider} jako {profile.Username}.", "platform-connect", ct);
		return new OAuthCompletion(OAuthCompletionKind.Connected, oauthState.ReturnOrigin, account.Id);
	}

	public async Task<Account?> DisconnectAsync(Guid accountId, OAuthProvider provider, CancellationToken ct = default) {
		var connection = await db.OAuthConnections
			.Include(item => item.Account)
			.FirstOrDefaultAsync(item => item.AccountId == accountId && item.Provider == provider, ct);
		if (connection == null) return await db.Accounts.FirstOrDefaultAsync(item => item.Id == accountId, ct);

		if (provider == OAuthProvider.Discord) {
			var refreshToken = UnprotectDiscordToken(connection.RefreshToken);
			if (!string.IsNullOrWhiteSpace(refreshToken)) await RevokeDiscordTokenAsync(refreshToken, ct);
		}

		await RemoveConnectionAsync(connection, false, ct);
		return connection.Account;
	}

	#endregion

	#region Discord kontrola propojeni

	public async Task EnsureDiscordConnectionAsync(Guid accountId, bool forceValidation, CancellationToken ct = default) {
		var connection = await db.OAuthConnections
			.Include(item => item.Account)
			.FirstOrDefaultAsync(item => item.AccountId == accountId && item.Provider == OAuthProvider.Discord, ct);
		if (connection == null || GetProviderConfig(OAuthProvider.Discord) == null) return;

		var nowUtc = DateTime.UtcNow;
		if (!forceValidation && connection.LastValidatedUtc is { } lastValidated && lastValidated >= nowUtc - ValidationInterval) return;

		var accessToken = UnprotectDiscordToken(connection.AccessToken);
		var refreshToken = UnprotectDiscordToken(connection.RefreshToken);
		if (string.IsNullOrWhiteSpace(accessToken) || string.IsNullOrWhiteSpace(refreshToken)) {
			await RemoveConnectionAsync(connection, true, ct);
			return;
		}

		var usedRefresh = false;
		if ((connection.AccessTokenExpiresAtUtc ?? DateTime.MinValue) <= nowUtc.AddMinutes(1)) {
			var refreshResult = await RefreshDiscordTokenAsync(refreshToken, ct);
			if (refreshResult.Tokens == null) {
				if (refreshResult.Invalid) await RemoveConnectionAsync(connection, true, ct);
				return;
			}
			ApplyDiscordTokens(connection, refreshResult.Tokens);
			accessToken = refreshResult.Tokens.AccessToken;
			usedRefresh = true;
		}

		var config = GetProviderConfig(OAuthProvider.Discord)!;
		var profileResult = await GetProfileAsync(config, accessToken!, ct);
		if (profileResult.TokenInvalid && !usedRefresh) {
			var refreshResult = await RefreshDiscordTokenAsync(refreshToken, ct);
			if (refreshResult.Tokens != null) {
				ApplyDiscordTokens(connection, refreshResult.Tokens);
				profileResult = await GetProfileAsync(config, refreshResult.Tokens.AccessToken!, ct);
			} else {
				if (refreshResult.Invalid) await RemoveConnectionAsync(connection, true, ct);
				return;
			}
		}

		if (profileResult.TokenInvalid || (profileResult.Profile != null && profileResult.Profile.UserId != connection.ProviderUserId)) {
			await RemoveConnectionAsync(connection, true, ct);
			return;
		}
		if (profileResult.Profile == null) return;

		ApplyConnection(connection.Account, connection, profileResult.Profile, null);
		connection.LastValidatedUtc = nowUtc;
		await db.SaveChangesAsync(ct);
	}

	#endregion

	#region Steam OpenID a kontrola propojeni

	private async Task<OAuthCompletion> CompleteSteamAuthorizationAsync(HttpRequest request, OAuthState oauthState, CancellationToken ct) {
		var mode = request.Query["openid.mode"].ToString();
		if (mode == "cancel") return new OAuthCompletion(OAuthCompletionKind.Cancelled, oauthState.ReturnOrigin);
		if (mode != "id_res") return new OAuthCompletion(OAuthCompletionKind.Failed, oauthState.ReturnOrigin);

		var steamId = await VerifySteamOpenIdResponseAsync(request, oauthState.CallbackUri, ct);
		if (steamId == null) return new OAuthCompletion(OAuthCompletionKind.Failed, oauthState.ReturnOrigin);
		var profileResult = await GetSteamProfileAsync(steamId, ct);
		var profile = profileResult.Profile;

		if (oauthState.Flow == OAuthFlow.Login) {
			var connection = await db.OAuthConnections
				.Include(item => item.Account)
				.FirstOrDefaultAsync(item => item.Provider == OAuthProvider.Steam && item.ProviderUserId == steamId, ct);
			if (connection == null) return new OAuthCompletion(OAuthCompletionKind.LoginNotLinked, oauthState.ReturnOrigin);

			if (profile != null) {
				ApplyConnection(connection.Account, connection, profile, null);
				connection.LastValidatedUtc = DateTime.UtcNow;
				await db.SaveChangesAsync(ct);
			}
			return new OAuthCompletion(OAuthCompletionKind.LoginSucceeded, oauthState.ReturnOrigin, connection.AccountId);
		}

		if (oauthState.AccountId == null) return new OAuthCompletion(OAuthCompletionKind.InvalidState, oauthState.ReturnOrigin);
		if (profile == null) return new OAuthCompletion(OAuthCompletionKind.Failed, oauthState.ReturnOrigin);

		var account = await db.Accounts
			.Include(item => item.OAuthConnections)
			.FirstOrDefaultAsync(item => item.Id == oauthState.AccountId.Value, ct);
		if (account == null) return new OAuthCompletion(OAuthCompletionKind.Failed, oauthState.ReturnOrigin);

		var alreadyConnected = await db.OAuthConnections.AsNoTracking()
			.FirstOrDefaultAsync(item => item.Provider == OAuthProvider.Steam && item.ProviderUserId == steamId && item.AccountId != account.Id, ct);
		if (alreadyConnected != null) return new OAuthCompletion(OAuthCompletionKind.AlreadyLinked, oauthState.ReturnOrigin);

		var connectionForAccount = account.OAuthConnections.FirstOrDefault(item => item.Provider == OAuthProvider.Steam);
		var isNewConnection = connectionForAccount == null;
		if (connectionForAccount == null) {
			connectionForAccount = new OAuthConnection {
				AccountId = account.Id,
				Account = account,
				Provider = OAuthProvider.Steam,
				ProviderUserId = steamId,
				Username = profile.Username,
			};
			db.OAuthConnections.Add(connectionForAccount);
		}

		ApplyConnection(account, connectionForAccount, profile, null);
		connectionForAccount.LastValidatedUtc = DateTime.UtcNow;
		await db.SaveChangesAsync(ct);
		if (isNewConnection) await dbLogger.LogInfoAsync($"Účet {FormatAccount(account)} propojil platformu Steam jako {profile.Username}.", "platform-connect", ct);
		return new OAuthCompletion(OAuthCompletionKind.Connected, oauthState.ReturnOrigin, account.Id);
	}

	private async Task<string?> VerifySteamOpenIdResponseAsync(HttpRequest request, string expectedReturnTo, CancellationToken ct) {
		var values = request.Query
			.Where(item => item.Key.StartsWith("openid.", StringComparison.Ordinal))
			.ToDictionary(item => item.Key, item => item.Value.ToString(), StringComparer.Ordinal);
		if (!values.TryGetValue("openid.op_endpoint", out var endpoint) || endpoint != SteamOpenIdEndpoint) return null;
		if (!values.TryGetValue("openid.return_to", out var returnTo) || returnTo != expectedReturnTo) return null;
		if (!values.TryGetValue("openid.claimed_id", out var claimedId) || !values.TryGetValue("openid.identity", out var identity) || identity != claimedId) return null;
		if (!claimedId.StartsWith(SteamClaimedIdPrefix, StringComparison.Ordinal)) return null;

		var steamId = claimedId[SteamClaimedIdPrefix.Length..];
		if (!ulong.TryParse(steamId, out var parsedSteamId) || parsedSteamId == 0 || claimedId != $"{SteamClaimedIdPrefix}{parsedSteamId}") return null;

		values["openid.mode"] = "check_authentication";
		using var verificationRequest = new HttpRequestMessage(HttpMethod.Post, SteamOpenIdEndpoint) {
			Content = new FormUrlEncodedContent(values),
		};
		try {
			using var response = await httpClient.SendAsync(verificationRequest, ct);
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

	public async Task EnsureSteamConnectionAsync(Guid accountId, bool forceValidation, CancellationToken ct = default) {
		var connection = await db.OAuthConnections
			.Include(item => item.Account)
			.FirstOrDefaultAsync(item => item.AccountId == accountId && item.Provider == OAuthProvider.Steam, ct);
		if (connection == null || GetSteamWebApiKey() == null) return;

		var nowUtc = DateTime.UtcNow;
		if (!forceValidation && connection.LastValidatedUtc is { } lastValidated && lastValidated >= nowUtc - ValidationInterval) return;

		var profileResult = await GetSteamProfileAsync(connection.ProviderUserId, ct);
		if (profileResult.Profile == null || profileResult.Profile.UserId != connection.ProviderUserId) return;

		ApplyConnection(connection.Account, connection, profileResult.Profile, null);
		connection.LastValidatedUtc = nowUtc;
		await db.SaveChangesAsync(ct);
	}

	private async Task<OAuthProfileResult> GetSteamProfileAsync(string steamId, CancellationToken ct) {
		var apiKey = GetSteamWebApiKey();
		if (apiKey == null) return new OAuthProfileResult(null, false);

		var endpoint = QueryHelpers.AddQueryString(SteamProfileEndpoint, "steamids", steamId);
		using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
		request.Headers.TryAddWithoutValidation("x-webapi-key", apiKey);
		request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

		try {
			using var response = await httpClient.SendAsync(request, ct);
			if (!response.IsSuccessStatusCode) return new OAuthProfileResult(null, false);
			var result = await response.Content.ReadFromJsonAsync<SteamPlayerSummariesResponse>(cancellationToken: ct);
			var user = result?.Response?.Players.FirstOrDefault(item => item.SteamId == steamId);
			if (user == null) return new OAuthProfileResult(null, false);

			var username = string.IsNullOrWhiteSpace(user.PersonaName) ? "Steam účet" : user.PersonaName;
			var profileUrl = string.IsNullOrWhiteSpace(user.ProfileUrl) ? $"https://steamcommunity.com/profiles/{steamId}/" : user.ProfileUrl;
			var avatarUrl = user.AvatarFull ?? user.AvatarMedium ?? user.Avatar;
			return new OAuthProfileResult(new OAuthProfile(steamId, username, avatarUrl, profileUrl), false);
		} catch (HttpRequestException exception) {
			logger.LogWarning(exception, "Steam profile request failed");
			return new OAuthProfileResult(null, false);
		} catch (JsonException exception) {
			logger.LogWarning(exception, "Steam profile response was invalid");
			return new OAuthProfileResult(null, false);
		} catch (OperationCanceledException exception) when (!ct.IsCancellationRequested) {
			logger.LogWarning(exception, "Steam profile request timed out");
			return new OAuthProfileResult(null, false);
		}
	}

	#endregion

	#region Synchronizace avataru

	public async Task<Account?> SetAvatarSyncPlatformAsync(Guid accountId, OAuthProvider? platform, CancellationToken ct = default) {
		var account = await db.Accounts
			.Include(item => item.OAuthConnections)
			.FirstOrDefaultAsync(item => item.Id == accountId, ct);
		if (account == null) return null;

		account.AvatarSyncPlatform = platform;
		account.AvatarUrl = null;
		if (platform is { } provider && provider != OAuthProvider.Discord) {
			account.AvatarUrl = account.OAuthConnections.FirstOrDefault(item => item.Provider == provider)?.AvatarUrl;
		}
		await db.SaveChangesAsync(ct);

		if (platform != OAuthProvider.Discord || !account.OAuthConnections.Any(item => item.Provider == OAuthProvider.Discord)) return account;
		await EnsureDiscordConnectionAsync(account.Id, true, ct);
		return await db.Accounts.FirstOrDefaultAsync(item => item.Id == accountId, ct);
	}

	#endregion

	#region Spolecne OAuth requesty

	private async Task<OAuthTokenResult> RequestTokenAsync(ProviderConfig config, Dictionary<string, string> content, CancellationToken ct) {
		switch (config.Provider) {
			case OAuthProvider.Discord: {
				using var request = new HttpRequestMessage(HttpMethod.Post, config.TokenEndpoint) {
					Content = new FormUrlEncodedContent(content),
				};
				request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
				request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{config.ClientId}:{config.ClientSecret}")));
				return await SendTokenRequestAsync(request, config.Provider, ct);
			}
			
			default: {
				content["client_id"] = config.ClientId;
				content["client_secret"] = config.ClientSecret;
				using var request = new HttpRequestMessage(HttpMethod.Post, config.TokenEndpoint) {
					Content = new FormUrlEncodedContent(content),
				};
				request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
				return await SendTokenRequestAsync(request, config.Provider, ct);
			}
		}
	}

	private async Task<OAuthTokenResult> SendTokenRequestAsync(HttpRequestMessage request, OAuthProvider provider, CancellationToken ct) {
		try {
			using var response = await httpClient.SendAsync(request, ct);
			if (!response.IsSuccessStatusCode) return new OAuthTokenResult(null, response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden);
			return new OAuthTokenResult(await response.Content.ReadFromJsonAsync<OAuthTokenResponse>(cancellationToken: ct), false);
		} catch (HttpRequestException exception) {
			logger.LogWarning(exception, "{Provider} token request failed", provider);
			return new OAuthTokenResult(null, false);
		}
	}

	private async Task<OAuthProfileResult> GetProfileAsync(ProviderConfig config, string accessToken, CancellationToken ct) {
		using var request = new HttpRequestMessage(HttpMethod.Get, config.UserEndpoint);
		request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
		request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

		try {
			using var response = await httpClient.SendAsync(request, ct);
			if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden) return new OAuthProfileResult(null, true);
			if (!response.IsSuccessStatusCode) return new OAuthProfileResult(null, false);
			var profile = config.Provider switch {
				OAuthProvider.Discord => ToProfile(await response.Content.ReadFromJsonAsync<DiscordUser>(cancellationToken: ct)),
				OAuthProvider.GitHub => ToProfile(await response.Content.ReadFromJsonAsync<GitHubUser>(cancellationToken: ct)),
				OAuthProvider.Google => ToProfile(await response.Content.ReadFromJsonAsync<GoogleUser>(cancellationToken: ct)),
				_ => null,
			};
			return new OAuthProfileResult(profile, false);
		} catch (HttpRequestException exception) {
			logger.LogWarning(exception, "{Provider} profile request failed", config.Provider);
			return new OAuthProfileResult(null, false);
		}
	}

	#endregion

	#region Discord tokeny

	private async Task<OAuthTokenResult> RefreshDiscordTokenAsync(string refreshToken, CancellationToken ct) {
		var config = GetProviderConfig(OAuthProvider.Discord);
		return config == null
			? new OAuthTokenResult(null, false)
			: await RequestTokenAsync(config, new Dictionary<string, string> {
				["grant_type"] = "refresh_token",
				["refresh_token"] = refreshToken,
			}, ct);
	}

	private async Task RevokeDiscordTokenAsync(string refreshToken, CancellationToken ct) {
		var config = GetProviderConfig(OAuthProvider.Discord);
		if (config == null) return;
		using var request = new HttpRequestMessage(HttpMethod.Post, "https://discord.com/api/oauth2/token/revoke") {
			Content = new FormUrlEncodedContent(new Dictionary<string, string> {
				["token"] = refreshToken,
				["token_type_hint"] = "refresh_token",
			}),
		};
		request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{config.ClientId}:{config.ClientSecret}")));

		try {
			await httpClient.SendAsync(request, ct);
		} catch (HttpRequestException exception) {
			logger.LogWarning(exception, "Discord token revocation failed");
		}
	}

	#endregion

	#region Spolecna data propojeni

	private async Task RemoveConnectionAsync(OAuthConnection connection, bool automatic, CancellationToken ct) {
		var account = connection.Account;
		var provider = connection.Provider;
		var username = connection.Username;
		db.OAuthConnections.Remove(connection);
		if (!automatic && account.AvatarSyncPlatform == provider) {
			account.AvatarSyncPlatform = null;
			account.AvatarUrl = null;
		}
		await db.SaveChangesAsync(ct);
		var mode = automatic ? "automaticky odpojena" : "odpojena";
		await dbLogger.LogInfoAsync($"Platforma {provider} ({username}) byla {mode} u účtu {FormatAccount(account)}.", "platform-disconnect", ct);
	}

	private void ApplyConnection(Account account, OAuthConnection connection, OAuthProfile profile, OAuthTokenResponse? tokens) {
		connection.ProviderUserId = profile.UserId;
		connection.Username = profile.Username;
		connection.ProfileUrl = profile.ProfileUrl;
		if (!string.IsNullOrWhiteSpace(profile.AvatarUrl)) {
			connection.AvatarUrl = profile.AvatarUrl;
			if (account.AvatarSyncPlatform == connection.Provider) account.AvatarUrl = profile.AvatarUrl;
		}
		if (connection.Provider == OAuthProvider.Discord && tokens != null) {
			ApplyDiscordTokens(connection, tokens);
			connection.LastValidatedUtc = DateTime.UtcNow;
		}
	}

	#endregion

	#region Discord profil

	private void ApplyDiscordTokens(OAuthConnection connection, OAuthTokenResponse tokens) {
		if (string.IsNullOrWhiteSpace(tokens.AccessToken) || string.IsNullOrWhiteSpace(tokens.RefreshToken) || tokens.ExpiresIn == null) return;
		connection.AccessToken = discordTokenProtector.Protect(tokens.AccessToken);
		connection.RefreshToken = discordTokenProtector.Protect(tokens.RefreshToken);
		connection.AccessTokenExpiresAtUtc = DateTime.UtcNow.AddSeconds(tokens.ExpiresIn.Value);
	}

	private string? UnprotectDiscordToken(string? protectedToken) {
		if (string.IsNullOrWhiteSpace(protectedToken)) return null;
		try {
			return discordTokenProtector.Unprotect(protectedToken);
		} catch (CryptographicException exception) {
			logger.LogWarning(exception, "Discord token could not be decrypted");
			return null;
		}
	}

	private static OAuthProfile? ToProfile(DiscordUser? user) => user == null || string.IsNullOrWhiteSpace(user.Id) || string.IsNullOrWhiteSpace(user.Username)
		? null
		: new OAuthProfile(user.Id, user.Username, GetDiscordAvatarUrl(user), null);

	private static string? GetDiscordAvatarUrl(DiscordUser user) {
		if (string.IsNullOrWhiteSpace(user.Avatar)) return null;
		var extension = user.Avatar.StartsWith("a_", StringComparison.Ordinal) ? "gif" : "png";
		return $"https://cdn.discordapp.com/avatars/{user.Id}/{user.Avatar}.{extension}?size=256";
	}

	#endregion

	#region GitHub profil

	private static OAuthProfile? ToProfile(GitHubUser? user) => user == null || string.IsNullOrWhiteSpace(user.Login)
		? null
		: new OAuthProfile(user.Id.ToString(), user.Login, user.AvatarUrl, user.HtmlUrl);

	#endregion

	#region Google profil

	private const int GoogleAvatarSize = 512;

	private static OAuthProfile? ToProfile(GoogleUser? user) => user == null || string.IsNullOrWhiteSpace(user.Sub)
		? null
		: new OAuthProfile(user.Sub, string.IsNullOrWhiteSpace(user.Name) ? "Google účet" : user.Name, GetGoogleAvatarUrl(user.Picture), null);

	private static string? GetGoogleAvatarUrl(string? pictureUrl) {
		if (string.IsNullOrWhiteSpace(pictureUrl) || !Uri.TryCreate(pictureUrl, UriKind.Absolute, out var uri)) return pictureUrl;
		if (uri.Scheme != "https" || !uri.Host.EndsWith(".googleusercontent.com", StringComparison.OrdinalIgnoreCase)) return pictureUrl;

		var baseUrl = uri.GetLeftPart(UriPartial.Path);
		var parameterIndex = baseUrl.LastIndexOf('=');
		if (parameterIndex > baseUrl.LastIndexOf('/')) baseUrl = baseUrl[..parameterIndex];
		return $"{baseUrl}=s{GoogleAvatarSize}-c";
	}

	#endregion

	#region Konfigurace a helpery

	private static string? GetRouteSegment(OAuthProvider provider) => provider switch {
		OAuthProvider.Discord => "discord",
		OAuthProvider.GitHub => "github",
		OAuthProvider.Google => "google",
		OAuthProvider.Steam => "steam",
		_ => null,
	};

	private static string? GetSteamWebApiKey() => Program.ENV.TryGetValue("STEAM_WEB_API_KEY", out var apiKey) && !string.IsNullOrWhiteSpace(apiKey)
		? apiKey
		: null;

	private static ProviderConfig? GetProviderConfig(OAuthProvider provider) {
		var prefix = provider switch {
			OAuthProvider.Discord => "DISCORD",
			OAuthProvider.GitHub => "GITHUB",
			OAuthProvider.Google => "GOOGLE",
			_ => null,
		};
		if (prefix == null || !Program.ENV.TryGetValue($"{prefix}_CLIENT_ID", out var clientId) || string.IsNullOrWhiteSpace(clientId)) return null;
		if (!Program.ENV.TryGetValue($"{prefix}_CLIENT_SECRET", out var clientSecret) || string.IsNullOrWhiteSpace(clientSecret)) return null;

		return provider switch {
			OAuthProvider.Discord => new ProviderConfig(provider, clientId, clientSecret, "discord", "https://discord.com/oauth2/authorize", "https://discord.com/api/oauth2/token", "https://discord.com/api/v10/users/@me", "identify"),
			OAuthProvider.GitHub => new ProviderConfig(provider, clientId, clientSecret, "github", "https://github.com/login/oauth/authorize", "https://github.com/login/oauth/access_token", "https://api.github.com/user", "read:user"),
			OAuthProvider.Google => new ProviderConfig(provider, clientId, clientSecret, "google", "https://accounts.google.com/o/oauth2/v2/auth", "https://oauth2.googleapis.com/token", "https://openidconnect.googleapis.com/v1/userinfo", "openid profile"),
			_ => null,
		};
	}

	private static string? GetFrontendOrigin() {
		if (!Program.ENV.TryGetValue("WEB_URL", out var webUrl) || !Uri.TryCreate(webUrl, UriKind.Absolute, out var uri)) return null;
		if (uri.Scheme is not ("http" or "https") || !string.IsNullOrEmpty(uri.UserInfo) || !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment)) return null;
		return uri.GetLeftPart(UriPartial.Authority);
	}

	private static bool IsHttps(string origin) => Uri.TryCreate(origin, UriKind.Absolute, out var uri) && uri.Scheme == "https";
	private static string GetStateCacheKey(string state) => $"oauth-state:{state}";
	private static string FormatAccount(Account account) => $"{account.FirstName} {account.LastName} ({account.Id})";

	#endregion

	#region Spolecne interni modely

	private sealed record ProviderConfig(OAuthProvider Provider, string ClientId, string ClientSecret, string RouteSegment, string AuthorizationEndpoint, string TokenEndpoint, string UserEndpoint, string Scope);
	private sealed record OAuthState(Guid? AccountId, OAuthProvider Provider, OAuthFlow Flow, string CallbackUri, string ReturnOrigin, string CodeVerifier);
	private sealed record OAuthProfile(string UserId, string Username, string? AvatarUrl, string? ProfileUrl);
	private sealed record OAuthTokenResult(OAuthTokenResponse? Tokens, bool Invalid);
	private sealed record OAuthProfileResult(OAuthProfile? Profile, bool TokenInvalid);

	private sealed class OAuthTokenResponse {
		[JsonPropertyName("access_token")]
		public string? AccessToken { get; init; }

		[JsonPropertyName("refresh_token")]
		public string? RefreshToken { get; init; }

		[JsonPropertyName("expires_in")]
		public int? ExpiresIn { get; init; }
	}

	#endregion

	#region Discord modely

	private sealed class DiscordUser {
		[JsonPropertyName("id")]
		public string? Id { get; init; }

		[JsonPropertyName("username")]
		public string? Username { get; init; }

		[JsonPropertyName("avatar")]
		public string? Avatar { get; init; }
	}

	#endregion

	#region GitHub modely

	private sealed class GitHubUser {
		[JsonPropertyName("id")]
		public long Id { get; init; }

		[JsonPropertyName("login")]
		public string? Login { get; init; }

		[JsonPropertyName("avatar_url")]
		public string? AvatarUrl { get; init; }

		[JsonPropertyName("html_url")]
		public string? HtmlUrl { get; init; }
	}

	#endregion

	#region Google modely

	private sealed class GoogleUser {
		[JsonPropertyName("sub")]
		public string? Sub { get; init; }

		[JsonPropertyName("name")]
		public string? Name { get; init; }

		[JsonPropertyName("picture")]
		public string? Picture { get; init; }
	}

	#endregion

	#region Steam modely

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

	#endregion
}
