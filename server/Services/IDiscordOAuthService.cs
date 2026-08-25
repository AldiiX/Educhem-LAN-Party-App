using server.Data.Entities;

namespace server.Services;

public interface IDiscordOAuthService {
	Task<Uri?> CreateAuthorizationUrlAsync(Guid? accountId, DiscordOAuthFlow flow, HttpRequest request, CancellationToken ct = default);
	Task<DiscordOAuthCompletion> CompleteAuthorizationAsync(HttpRequest request, string? state, string? code, string? error, CancellationToken ct = default);
	Task<DiscordConnectionStatus> EnsureConnectionAsync(Guid accountId, bool forceValidation, CancellationToken ct = default);
	Task<Account?> DisconnectAsync(Guid accountId, bool revokeRemoteToken, CancellationToken ct = default);
	Task<Account?> SetAvatarSyncPlatformAsync(Guid accountId, AvatarSyncPlatform? platform, CancellationToken ct = default);
}

public enum DiscordOAuthFlow {
	Login,
	Connect,
}

public enum DiscordConnectionStatus {
	NotLinked,
	Valid,
	Removed,
	TemporarilyUnavailable,
}

public enum DiscordOAuthCompletionKind {
	InvalidState,
	Cancelled,
	Failed,
	LoginNotLinked,
	AlreadyLinked,
	LoginSucceeded,
	Connected,
}

public sealed record DiscordOAuthCompletion(DiscordOAuthCompletionKind Kind, string? ReturnOrigin = null, Guid? AccountId = null);
