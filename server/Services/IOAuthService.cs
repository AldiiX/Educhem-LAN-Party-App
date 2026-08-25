using server.Data.Entities;

namespace server.Services;

public interface IOAuthService {
	Task<Uri?> CreateAuthorizationUrlAsync(Guid? accountId, OAuthProvider provider, OAuthFlow flow, HttpRequest request, CancellationToken ct = default);
	Task<OAuthCompletion> CompleteAuthorizationAsync(HttpRequest request, OAuthProvider provider, string? state, string? code, string? error, CancellationToken ct = default);
	Task<Account?> DisconnectAsync(Guid accountId, OAuthProvider provider, CancellationToken ct = default);
	Task EnsureDiscordConnectionAsync(Guid accountId, bool forceValidation, CancellationToken ct = default);
	Task<Account?> SetAvatarSyncPlatformAsync(Guid accountId, OAuthProvider? platform, CancellationToken ct = default);
}

public enum OAuthFlow {
	Login,
	Connect,
}

public enum OAuthCompletionKind {
	InvalidState,
	Cancelled,
	Failed,
	LoginNotLinked,
	AlreadyLinked,
	LoginSucceeded,
	Connected,
}

public sealed record OAuthCompletion(OAuthCompletionKind Kind, string? ReturnOrigin = null, Guid? AccountId = null);
