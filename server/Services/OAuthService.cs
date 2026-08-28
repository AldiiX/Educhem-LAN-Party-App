using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using server.Data;
using server.Data.Entities;
using server.Services.OAuth;
using server.Services.OAuth.Platforms;

namespace server.Services;

/// <summary>
/// orchestrator pro prihlasovani a propojovani uctu pres externi platformy
/// doménova pravidla pro ucty, session a audit drzi na jednom miste, platformove specifika deleguje implementacim IOAuthPlatform
/// </summary>
internal sealed class OAuthService(
	AppDbContext db,
	IEnumerable<IOAuthPlatform> platformImplementations,
	IDbLoggerService dbLogger
) : IOAuthService {
	private static readonly TimeSpan ValidationInterval = TimeSpan.FromMinutes(15);
	private readonly IReadOnlyDictionary<OAuthProvider, IOAuthPlatform> platforms = platformImplementations.ToDictionary(p => p.Provider);

	/// <inheritdoc />
	public bool IsProviderConfigured(OAuthProvider provider) =>
		platforms.TryGetValue(provider, out var platform) && platform.IsConfigured;

	/// <inheritdoc />
	public string GetAuthenticationScheme(OAuthProvider provider) =>
		platforms.TryGetValue(provider, out var platform)
			? platform.Scheme
			: throw new InvalidOperationException($"Nepodporovany provider: {provider}");

	/// <inheritdoc />
	public string? GetFrontendOrigin() {
		if (!Program.ENV.TryGetValue("WEB_URL", out var webUrl) || !Uri.TryCreate(webUrl, UriKind.Absolute, out var uri)) return null;
		if (uri.Scheme is not ("http" or "https") || !string.IsNullOrEmpty(uri.UserInfo) || !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment)) return null;
		return uri.GetLeftPart(UriPartial.Authority);
	}

	/// <inheritdoc />
	public async Task<OAuthCompletion> CompleteExternalAuthAsync(AuthenticateResult authResult, OAuthProvider provider, CancellationToken ct = default) {
		if (!authResult.Succeeded || authResult.Principal == null || authResult.Properties == null || !platforms.TryGetValue(provider, out var platform)) {
			return new OAuthCompletion(OAuthCompletionKind.Failed, GetFrontendOrigin(), Flow: OAuthFlow.Login);
		}

		var properties = authResult.Properties;
		var origin = properties.Items.TryGetValue("origin", out var o) && !string.IsNullOrWhiteSpace(o)
			? o
			: GetFrontendOrigin() ?? "/";

		var flowString = properties.Items.TryGetValue("flow", out var f) ? f : "login";
		var flow = flowString == "connect" ? OAuthFlow.Connect : OAuthFlow.Login;

		Guid? accountId = null;
		if (properties.Items.TryGetValue("accountId", out var accIdStr) && Guid.TryParse(accIdStr, out var parsedGuid)) {
			accountId = parsedGuid;
		}

		var profile = platform.ExtractProfile(authResult.Principal, properties);
		if (string.IsNullOrWhiteSpace(profile.UserId)) {
			return new OAuthCompletion(OAuthCompletionKind.Failed, origin, Flow: flow);
		}

		return flow == OAuthFlow.Login
			? await CompleteLoginAsync(platform, profile, origin, ct)
			: await CompleteConnectAsync(platform, profile, origin, accountId, ct);
	}

	/// <inheritdoc />
	public async Task<Account?> DisconnectAsync(Guid accountId, OAuthProvider provider, CancellationToken ct = default) {
		var connection = await db.OAuthConnections
			.Include(item => item.Account)
			.FirstOrDefaultAsync(item => item.AccountId == accountId && item.Provider == provider, ct);
		if (connection == null) return await db.Accounts.FirstOrDefaultAsync(item => item.Id == accountId, ct);

		if (platforms.TryGetValue(provider, out var platform)) {
			await platform.RevokeConnectionAsync(connection, ct);
		}

		await RemoveConnectionAsync(connection, false, ct);
		return connection.Account;
	}

	/// <inheritdoc />
	public Task EnsureDiscordConnectionAsync(Guid accountId, bool forceValidation, CancellationToken ct = default) =>
		EnsureConnectionAsync(accountId, OAuthProvider.Discord, forceValidation, ct);

	/// <inheritdoc />
	public Task EnsureSteamConnectionAsync(Guid accountId, bool forceValidation, CancellationToken ct = default) =>
		EnsureConnectionAsync(accountId, OAuthProvider.Steam, forceValidation, ct);

	/// <inheritdoc />
	public async Task<Account?> SetAvatarSyncPlatformAsync(Guid accountId, OAuthProvider? platform, CancellationToken ct = default) {
		if (platform == OAuthProvider.Apple) return null;
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

	/// <summary>
	/// dokonci prihlasovaci flow, najde propojeny ucet a aktualizuje profilova data
	/// </summary>
	/// <param name="platform">implementace platformy</param>
	/// <param name="profile">extrahovany profil z externiho tokenu</param>
	/// <param name="origin">overeny frontend origin</param>
	/// <param name="ct">token pro zruseni asynchronni operace</param>
	/// <returns>vysledek prihlaseni</returns>
	private async Task<OAuthCompletion> CompleteLoginAsync(IOAuthPlatform platform, ExtractedOAuthProfile profile, string origin, CancellationToken ct) {
		var connection = await db.OAuthConnections
			.Include(item => item.Account)
			.FirstOrDefaultAsync(item => item.Provider == platform.Provider && item.ProviderUserId == profile.UserId, ct);
		if (connection == null) return new OAuthCompletion(OAuthCompletionKind.LoginNotLinked, origin, Flow: OAuthFlow.Login);

		ApplyProfile(connection.Account, connection, platform, profile);
		await db.SaveChangesAsync(ct);

		return new OAuthCompletion(OAuthCompletionKind.LoginSucceeded, origin, connection.AccountId, OAuthFlow.Login);
	}

	/// <summary>
	/// dokonci propojovaci flow, overi kolize a ulozi nove nebo aktualizovane spojeni k prihlasenemu uctu
	/// </summary>
	/// <param name="platform">implementace platformy</param>
	/// <param name="profile">extrahovany profil z externiho tokenu</param>
	/// <param name="origin">overeny frontend origin</param>
	/// <param name="accountId">id prihlaseneho uctu ziskane ze state</param>
	/// <param name="ct">token pro zruseni asynchronni operace</param>
	/// <returns>vysledek propojeni</returns>
	private async Task<OAuthCompletion> CompleteConnectAsync(IOAuthPlatform platform, ExtractedOAuthProfile profile, string origin, Guid? accountId, CancellationToken ct) {
		if (accountId == null) return new OAuthCompletion(OAuthCompletionKind.InvalidState, origin, Flow: OAuthFlow.Connect);

		var account = await db.Accounts
			.Include(item => item.OAuthConnections)
			.FirstOrDefaultAsync(item => item.Id == accountId.Value, ct);
		if (account == null) return new OAuthCompletion(OAuthCompletionKind.Failed, origin, Flow: OAuthFlow.Connect);

		var alreadyConnected = await db.OAuthConnections.AsNoTracking()
			.FirstOrDefaultAsync(item => item.Provider == platform.Provider && item.ProviderUserId == profile.UserId && item.AccountId != account.Id, ct);
		if (alreadyConnected != null) return new OAuthCompletion(OAuthCompletionKind.AlreadyLinked, origin, account.Id, OAuthFlow.Connect);

		var connection = account.OAuthConnections.FirstOrDefault(item => item.Provider == platform.Provider);
		var isNew = connection == null;
		if (connection == null) {
			connection = new OAuthConnection {
				AccountId = account.Id,
				Account = account,
				Provider = platform.Provider,
				ProviderUserId = profile.UserId,
				Username = profile.Username,
			};
			db.OAuthConnections.Add(connection);
		}

		ApplyProfile(account, connection, platform, profile);
		await db.SaveChangesAsync(ct);

		if (isNew) {
			await dbLogger.LogInfoAsync($"Účet {FormatAccount(account)} propojil platformu {platform.Provider} jako {profile.Username}.", "platform-connect", ct);
		}

		return new OAuthCompletion(OAuthCompletionKind.Connected, origin, account.Id, OAuthFlow.Connect);
	}

	/// <summary>
	/// zkontroluje a pripadne validuje existujici spojeni podle stanoveneho casoveho intervalu
	/// </summary>
	/// <param name="accountId">id uctu</param>
	/// <param name="provider">kontrolovany provider</param>
	/// <param name="forceValidation">urcuje, zda se ma ignorovat casovy interval</param>
	/// <param name="ct">token pro zruseni asynchronni operace</param>
	private async Task EnsureConnectionAsync(Guid accountId, OAuthProvider provider, bool forceValidation, CancellationToken ct) {
		if (!platforms.TryGetValue(provider, out var platform) || !platform.IsConfigured) return;

		var connection = await db.OAuthConnections
			.Include(item => item.Account)
			.FirstOrDefaultAsync(item => item.AccountId == accountId && item.Provider == provider, ct);
		if (connection == null) return;

		var nowUtc = DateTime.UtcNow;
		if (!forceValidation && connection.LastValidatedUtc is { } lastValidated && lastValidated >= nowUtc - ValidationInterval) return;

		var result = await platform.ValidateConnectionAsync(connection, ct);
		if (result.Status == PlatformValidationStatus.Invalid) {
			await RemoveConnectionAsync(connection, true, ct);
			return;
		}
		if (result.Status != PlatformValidationStatus.Valid) return;

		connection.Username = result.Username ?? connection.Username;
		if (!string.IsNullOrWhiteSpace(result.AvatarUrl)) {
			connection.AvatarUrl = result.AvatarUrl;
			if (connection.Account.AvatarSyncPlatform == provider) {
				connection.Account.AvatarUrl = result.AvatarUrl;
			}
		}
		if (!string.IsNullOrWhiteSpace(result.ProfileUrl)) {
			connection.ProfileUrl = result.ProfileUrl;
		}
		connection.LastValidatedUtc = DateTime.UtcNow;
		await db.SaveChangesAsync(ct);
	}

	/// <summary>
	/// promitne ziskana data profilu do entity uctu a spojeni
	/// </summary>
	/// <param name="account">ucet vlastnika</param>
	/// <param name="connection">entita spojeni s platformou</param>
	/// <param name="platform">implementace platformy</param>
	/// <param name="profile">extrahovany profil</param>
	private static void ApplyProfile(Account account, OAuthConnection connection, IOAuthPlatform platform, ExtractedOAuthProfile profile) {
		connection.ProviderUserId = profile.UserId;
		connection.Username = profile.Username;
		connection.ProfileUrl = profile.ProfileUrl;
		if (!string.IsNullOrWhiteSpace(profile.AvatarUrl)) {
			connection.AvatarUrl = profile.AvatarUrl;
			if (account.AvatarSyncPlatform == platform.Provider) {
				account.AvatarUrl = profile.AvatarUrl;
			}
		}
		if (platform is DiscordOAuthPlatform discordPlatform) {
			discordPlatform.ApplyTokens(connection, profile.AccessToken, profile.RefreshToken, profile.ExpiresAtUtc);
		}
		connection.LastValidatedUtc = DateTime.UtcNow;
	}

	/// <summary>
	/// odstrani spojeni z databaze a zapise zaznam do audit logu
	/// </summary>
	/// <param name="connection">odstranovane spojeni</param>
	/// <param name="automatic">urcuje, zda k odpojeni doslo automaticky pri neplatnem tokenu</param>
	/// <param name="ct">token pro zruseni asynchronni operace</param>
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

	/// <summary>
	/// naformatuje identifikaci uctu pro ucely audit logu
	/// </summary>
	/// <param name="account">ucet</param>
	/// <returns>textova reprezentace jmena a id uctu</returns>
	private static string FormatAccount(Account account) => $"{account.FirstName} {account.LastName} ({account.Id})";
}