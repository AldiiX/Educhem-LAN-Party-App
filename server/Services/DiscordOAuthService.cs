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

public sealed class DiscordOAuthService(
	AppDbContext db,
	HttpClient httpClient,
	IDistributedCache cache,
	IDataProtectionProvider dataProtectionProvider,
	ILogger<DiscordOAuthService> logger,
	IDbLoggerService dbLogger
) : IDiscordOAuthService {
	private const string StateCookieName = "educhemlanparty_discord_oauth_state";
	private static readonly TimeSpan StateLifetime = TimeSpan.FromMinutes(10);
	private static readonly TimeSpan ValidationInterval = TimeSpan.FromMinutes(15);
	private readonly IDataProtector tokenProtector = dataProtectionProvider.CreateProtector("discord-oauth-tokens");

	public async Task<Uri?> CreateAuthorizationUrlAsync(Guid? accountId, DiscordOAuthFlow flow, HttpRequest request, CancellationToken ct = default) {
		var clientId = GetEnvironmentValue("DISCORD_CLIENT_ID");
		var clientSecret = GetEnvironmentValue("DISCORD_CLIENT_SECRET");
		if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret)) return null;
		if (flow == DiscordOAuthFlow.Connect && accountId == null) return null;

		var state = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
		var frontendOrigin = GetFrontendOrigin(request);
		var callbackUri = $"{frontendOrigin}/api/v1/discord/callback";
		var payload = new DiscordOAuthState(accountId, flow, callbackUri, frontendOrigin);
		var options = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = StateLifetime };
		await cache.SetStringAsync(GetStateCacheKey(state), JsonSerializer.Serialize(payload), options, ct);

		request.HttpContext.Response.Cookies.Append(StateCookieName, state, new CookieOptions {
			HttpOnly = true,
			IsEssential = true,
			SameSite = SameSiteMode.Lax,
			Secure = IsHttps(request),
			MaxAge = StateLifetime,
			Path = "/api/v1/discord",
		});

		var authorizationUrl = QueryHelpers.AddQueryString("https://discord.com/oauth2/authorize", new Dictionary<string, string?> {
			["client_id"] = clientId,
			["response_type"] = "code",
			["redirect_uri"] = callbackUri,
			["scope"] = "identify",
			["state"] = state,
		});

		return new Uri(authorizationUrl);
	}

	public async Task<DiscordOAuthCompletion> CompleteAuthorizationAsync(HttpRequest request, string? state, string? code, string? error, CancellationToken ct = default) {
		if (string.IsNullOrWhiteSpace(state)) return new DiscordOAuthCompletion(DiscordOAuthCompletionKind.InvalidState);
		if (!request.Cookies.TryGetValue(StateCookieName, out var cookieState) || !CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(state), Encoding.UTF8.GetBytes(cookieState))) {
			return new DiscordOAuthCompletion(DiscordOAuthCompletionKind.InvalidState);
		}

		var serializedState = await cache.GetStringAsync(GetStateCacheKey(state), ct);
		await cache.RemoveAsync(GetStateCacheKey(state), ct);
		request.HttpContext.Response.Cookies.Delete(StateCookieName, new CookieOptions { Path = "/api/v1/discord" });
		if (string.IsNullOrWhiteSpace(serializedState)) return new DiscordOAuthCompletion(DiscordOAuthCompletionKind.InvalidState);

		var oauthState = JsonSerializer.Deserialize<DiscordOAuthState>(serializedState);
		if (oauthState == null) return new DiscordOAuthCompletion(DiscordOAuthCompletionKind.InvalidState);
		if (!string.IsNullOrWhiteSpace(error) || string.IsNullOrWhiteSpace(code)) {
			return new DiscordOAuthCompletion(DiscordOAuthCompletionKind.Cancelled, oauthState.ReturnOrigin);
		}

		var tokens = await ExchangeCodeAsync(code, oauthState.CallbackUri, ct);
		if (tokens == null) return new DiscordOAuthCompletion(DiscordOAuthCompletionKind.Failed, oauthState.ReturnOrigin);

		var userResult = await GetCurrentUserAsync(tokens.AccessToken, ct);
		if (userResult.User == null) return new DiscordOAuthCompletion(DiscordOAuthCompletionKind.Failed, oauthState.ReturnOrigin);

		if (oauthState.Flow == DiscordOAuthFlow.Login) {
			var connection = await db.DiscordConnections
				.Include(item => item.Account)
				.FirstOrDefaultAsync(item => item.DiscordId == userResult.User.Id, ct);
			if (connection == null) return new DiscordOAuthCompletion(DiscordOAuthCompletionKind.LoginNotLinked, oauthState.ReturnOrigin);

			ApplyConnection(connection.Account, connection, userResult.User, tokens);
			await db.SaveChangesAsync(ct);
			return new DiscordOAuthCompletion(DiscordOAuthCompletionKind.LoginSucceeded, oauthState.ReturnOrigin, connection.AccountId);
		}

		if (oauthState.AccountId == null) return new DiscordOAuthCompletion(DiscordOAuthCompletionKind.InvalidState, oauthState.ReturnOrigin);
		var connectedAccount = await db.Accounts.FirstOrDefaultAsync(item => item.Id == oauthState.AccountId.Value, ct);
		if (connectedAccount == null) return new DiscordOAuthCompletion(DiscordOAuthCompletionKind.Failed, oauthState.ReturnOrigin);

		var alreadyConnected = await db.DiscordConnections
			.AsNoTracking()
			.FirstOrDefaultAsync(item => item.DiscordId == userResult.User.Id && item.AccountId != connectedAccount.Id, ct);
		if (alreadyConnected != null) return new DiscordOAuthCompletion(DiscordOAuthCompletionKind.AlreadyLinked, oauthState.ReturnOrigin);

		var connectedDiscord = await db.DiscordConnections.FirstOrDefaultAsync(item => item.AccountId == connectedAccount.Id, ct);
		var isNewConnection = connectedDiscord == null;
		if (connectedDiscord == null) {
			connectedDiscord = new DiscordConnection { AccountId = connectedAccount.Id, Account = connectedAccount, DiscordId = userResult.User.Id, Username = userResult.User.Username, AccessToken = string.Empty, RefreshToken = string.Empty };
			db.DiscordConnections.Add(connectedDiscord);
		}
		ApplyConnection(connectedAccount, connectedDiscord, userResult.User, tokens);
		await db.SaveChangesAsync(ct);
		if (isNewConnection) await dbLogger.LogInfoAsync($"Účet {FormatAccount(connectedAccount)} propojil platformu Discord jako {userResult.User.Username}.", "platform-connect", ct);
		return new DiscordOAuthCompletion(DiscordOAuthCompletionKind.Connected, oauthState.ReturnOrigin, connectedAccount.Id);
	}

	public async Task<DiscordConnectionStatus> EnsureConnectionAsync(Guid accountId, bool forceValidation, CancellationToken ct = default) {
		var connection = await db.DiscordConnections
			.Include(item => item.Account)
			.FirstOrDefaultAsync(item => item.AccountId == accountId, ct);
		if (connection == null) return DiscordConnectionStatus.NotLinked;
		var account = connection.Account;

		var nowUtc = DateTime.UtcNow;
		if (!forceValidation && connection.LastValidatedUtc >= nowUtc - ValidationInterval) return DiscordConnectionStatus.Valid;

		var accessToken = UnprotectToken(connection.AccessToken);
		var refreshToken = UnprotectToken(connection.RefreshToken);
		if (string.IsNullOrWhiteSpace(accessToken) || string.IsNullOrWhiteSpace(refreshToken)) {
			await DisconnectLocalAsync(connection, true, ct);
			return DiscordConnectionStatus.Removed;
		}

		var usedRefresh = false;
		if (connection.AccessTokenExpiresAtUtc <= nowUtc.AddMinutes(1)) {
			var refreshResult = await RefreshTokenAsync(refreshToken, ct);
			if (refreshResult.Tokens == null) {
				if (refreshResult.Invalid) {
					await DisconnectLocalAsync(connection, true, ct);
					return DiscordConnectionStatus.Removed;
				}
				return DiscordConnectionStatus.TemporarilyUnavailable;
			}

			ApplyTokens(connection, refreshResult.Tokens);
			accessToken = refreshResult.Tokens.AccessToken;
			usedRefresh = true;
		}

		var userResult = await GetCurrentUserAsync(accessToken, ct);
		if (userResult.TokenInvalid && !usedRefresh) {
			var refreshResult = await RefreshTokenAsync(refreshToken, ct);
			if (refreshResult.Tokens != null) {
				ApplyTokens(connection, refreshResult.Tokens);
				userResult = await GetCurrentUserAsync(refreshResult.Tokens.AccessToken, ct);
			} else if (refreshResult.Invalid) {
				await DisconnectLocalAsync(connection, true, ct);
				return DiscordConnectionStatus.Removed;
			} else {
				return DiscordConnectionStatus.TemporarilyUnavailable;
			}
		}

		if (userResult.TokenInvalid || (userResult.User != null && userResult.User.Id != connection.DiscordId)) {
			await DisconnectLocalAsync(connection, true, ct);
			return DiscordConnectionStatus.Removed;
		}
		if (userResult.User == null) return DiscordConnectionStatus.TemporarilyUnavailable;

		connection.Username = userResult.User.Username;
		connection.LastValidatedUtc = nowUtc;
		if (account.AvatarSyncPlatform == AvatarSyncPlatform.Discord) account.AvatarUrl = GetAvatarUrl(userResult.User);
		await db.SaveChangesAsync(ct);
		return DiscordConnectionStatus.Valid;
	}

	public async Task<Account?> DisconnectAsync(Guid accountId, bool revokeRemoteToken, CancellationToken ct = default) {
		var connection = await db.DiscordConnections
			.Include(item => item.Account)
			.FirstOrDefaultAsync(item => item.AccountId == accountId, ct);
		if (connection == null) return await db.Accounts.FirstOrDefaultAsync(item => item.Id == accountId, ct);
		var account = connection.Account;

		if (revokeRemoteToken) {
			var refreshToken = UnprotectToken(connection.RefreshToken);
			if (!string.IsNullOrWhiteSpace(refreshToken)) await RevokeTokenAsync(refreshToken, ct);
		}

		await DisconnectLocalAsync(connection, false, ct);
		return account;
	}

	public async Task<Account?> SetAvatarSyncPlatformAsync(Guid accountId, AvatarSyncPlatform? platform, CancellationToken ct = default) {
		var account = await db.Accounts
			.Include(item => item.DiscordConnection)
			.FirstOrDefaultAsync(item => item.Id == accountId, ct);
		if (account == null) return null;

		account.AvatarSyncPlatform = platform;
		account.AvatarUrl = null;
		await db.SaveChangesAsync(ct);

		if (platform != AvatarSyncPlatform.Discord || account.DiscordConnection == null) return account;
		await EnsureConnectionAsync(account.Id, true, ct);
		return await db.Accounts.FirstOrDefaultAsync(item => item.Id == accountId, ct);
	}

	private async Task<DiscordTokenResponse?> ExchangeCodeAsync(string code, string callbackUri, CancellationToken ct) {
		var result = await RequestTokenAsync(new Dictionary<string, string> {
			["grant_type"] = "authorization_code",
			["code"] = code,
			["redirect_uri"] = callbackUri,
		}, ct);
		return result.Tokens;
	}

	private async Task<DiscordTokenResult> RefreshTokenAsync(string refreshToken, CancellationToken ct) {
		return await RequestTokenAsync(new Dictionary<string, string> {
			["grant_type"] = "refresh_token",
			["refresh_token"] = refreshToken,
		}, ct);
	}

	private async Task<DiscordTokenResult> RequestTokenAsync(Dictionary<string, string> content, CancellationToken ct) {
		var clientId = GetEnvironmentValue("DISCORD_CLIENT_ID");
		var clientSecret = GetEnvironmentValue("DISCORD_CLIENT_SECRET");
		if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret)) return new DiscordTokenResult(null, false);

		using var request = new HttpRequestMessage(HttpMethod.Post, "https://discord.com/api/oauth2/token") {
			Content = new FormUrlEncodedContent(content),
		};
		request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}")));

		try {
			using var response = await httpClient.SendAsync(request, ct);
			if (!response.IsSuccessStatusCode) {
				return new DiscordTokenResult(null, response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden);
			}
			return new DiscordTokenResult(await response.Content.ReadFromJsonAsync<DiscordTokenResponse>(cancellationToken: ct), false);
		} catch (HttpRequestException exception) {
			logger.LogWarning(exception, "Discord token request failed");
			return new DiscordTokenResult(null, false);
		}
	}

	private async Task<DiscordUserResult> GetCurrentUserAsync(string accessToken, CancellationToken ct) {
		using var request = new HttpRequestMessage(HttpMethod.Get, "users/@me");
		request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

		try {
			using var response = await httpClient.SendAsync(request, ct);
			if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden) return new DiscordUserResult(null, true);
			if (!response.IsSuccessStatusCode) return new DiscordUserResult(null, false);
			return new DiscordUserResult(await response.Content.ReadFromJsonAsync<DiscordUser>(cancellationToken: ct), false);
		} catch (HttpRequestException exception) {
			logger.LogWarning(exception, "Discord user request failed");
			return new DiscordUserResult(null, false);
		}
	}

	private async Task RevokeTokenAsync(string refreshToken, CancellationToken ct) {
		var clientId = GetEnvironmentValue("DISCORD_CLIENT_ID");
		var clientSecret = GetEnvironmentValue("DISCORD_CLIENT_SECRET");
		if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret)) return;

		using var request = new HttpRequestMessage(HttpMethod.Post, "https://discord.com/api/oauth2/token/revoke") {
			Content = new FormUrlEncodedContent(new Dictionary<string, string> {
				["token"] = refreshToken,
				["token_type_hint"] = "refresh_token",
			}),
		};
		request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}")));

		try {
			await httpClient.SendAsync(request, ct);
		} catch (HttpRequestException exception) {
			logger.LogWarning(exception, "Discord token revocation failed");
		}
	}

	private async Task DisconnectLocalAsync(DiscordConnection connection, bool automatic, CancellationToken ct) {
		var account = connection.Account;
		var username = connection.Username;
		db.DiscordConnections.Remove(connection);
		account.DiscordConnection = null;
		account.AvatarUrl = null;
		await db.SaveChangesAsync(ct);
		var mode = automatic ? "automaticky odpojena" : "odpojena";
		await dbLogger.LogInfoAsync($"Platforma Discord ({username}) byla {mode} u účtu {FormatAccount(account)}.", "platform-disconnect", ct);
	}

	private static string FormatAccount(Account account) => $"{account.FirstName} {account.LastName} ({account.Id})";

	private void ApplyConnection(Account account, DiscordConnection connection, DiscordUser user, DiscordTokenResponse tokens) {
		connection.DiscordId = user.Id;
		connection.Username = user.Username;
		ApplyTokens(connection, tokens);
		connection.LastValidatedUtc = DateTime.UtcNow;
		if (account.AvatarSyncPlatform == AvatarSyncPlatform.Discord) account.AvatarUrl = GetAvatarUrl(user);
	}

	private void ApplyTokens(DiscordConnection connection, DiscordTokenResponse tokens) {
		connection.AccessToken = tokenProtector.Protect(tokens.AccessToken);
		connection.RefreshToken = tokenProtector.Protect(tokens.RefreshToken);
		connection.AccessTokenExpiresAtUtc = DateTime.UtcNow.AddSeconds(tokens.ExpiresIn);
	}

	private string? UnprotectToken(string? protectedToken) {
		if (string.IsNullOrWhiteSpace(protectedToken)) return null;
		try {
			return tokenProtector.Unprotect(protectedToken);
		} catch (CryptographicException exception) {
			logger.LogWarning(exception, "Discord token could not be decrypted");
			return null;
		}
	}

	private static string? GetAvatarUrl(DiscordUser user) {
		if (string.IsNullOrWhiteSpace(user.Avatar)) return null;
		var extension = user.Avatar.StartsWith("a_", StringComparison.Ordinal) ? "gif" : "png";
		return $"https://cdn.discordapp.com/avatars/{user.Id}/{user.Avatar}.{extension}?size=256";
	}

	private static string GetStateCacheKey(string state) => $"discord-oauth-state:{state}";

	private static string? GetEnvironmentValue(string key) => Program.ENV.TryGetValue(key, out var value) ? value : null;

	private static string GetFrontendOrigin(HttpRequest request) {
		if (Uri.TryCreate(request.Headers.Referer, UriKind.Absolute, out var referer)) return referer.GetLeftPart(UriPartial.Authority);
		return GetRequestOrigin(request);
	}

	private static string GetRequestOrigin(HttpRequest request) {
		var scheme = request.Headers["X-Forwarded-Proto"].FirstOrDefault()?.Split(',')[0] ?? request.Scheme;
		var host = request.Headers["X-Forwarded-Host"].FirstOrDefault()?.Split(',')[0] ?? request.Host.Value;
		return $"{scheme}://{host}";
	}

	private static bool IsHttps(HttpRequest request) => string.Equals(request.Headers["X-Forwarded-Proto"].FirstOrDefault()?.Split(',')[0], "https", StringComparison.OrdinalIgnoreCase) || request.IsHttps;

	private sealed record DiscordOAuthState(Guid? AccountId, DiscordOAuthFlow Flow, string CallbackUri, string ReturnOrigin);
	private sealed record DiscordUserResult(DiscordUser? User, bool TokenInvalid);
	private sealed record DiscordTokenResult(DiscordTokenResponse? Tokens, bool Invalid);

	private sealed class DiscordTokenResponse {
		[JsonPropertyName("access_token")]
		public required string AccessToken { get; init; }

		[JsonPropertyName("refresh_token")]
		public required string RefreshToken { get; init; }

		[JsonPropertyName("expires_in")]
		public required int ExpiresIn { get; init; }
	}

	private sealed class DiscordUser {
		[JsonPropertyName("id")]
		public required string Id { get; init; }

		[JsonPropertyName("username")]
		public required string Username { get; init; }

		[JsonPropertyName("avatar")]
		public string? Avatar { get; init; }
	}
}
