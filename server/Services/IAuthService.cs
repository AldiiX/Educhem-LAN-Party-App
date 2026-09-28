using server.Data.Entities;
using server.Dto;

namespace server.Services;

/// <summary>
/// stav vysledku pokusu o prihlaseni
/// </summary>
public enum LoginStatus {
	/// <summary>
	/// prihlaseni probehlo v poradku
	/// </summary>
	Success,
	/// <summary>
	/// spatny email nebo heslo
	/// </summary>
	InvalidCredentials,
	/// <summary>
	/// ucet je docasne uzamcen kvuli prekroceni limitu pokusu
	/// </summary>
	LockedOut
}

/// <summary>
/// vysledek prihlaseni vcetne pripadneho uctu nebo lockout zpravy
/// </summary>
public sealed record LoginResult(LoginStatus Status, Account? Account = null, string? LockoutMessage = null) {
	public static LoginResult Success(Account account) => new(LoginStatus.Success, account);
	public static LoginResult InvalidCredentials() => new(LoginStatus.InvalidCredentials);
	public static LoginResult LockedOut(string message) => new(LoginStatus.LockedOut, LockoutMessage: message);
}

public readonly record struct CurrentUserContext(
	Guid Id,
	AccountType Role,
	Guid SessionId,
	CommunicationStyle CommunicationStyle
);

public interface IAuthService {
	Task<LoginResult> LoginAsync(string identifier, string plainPassword, bool rememberMe, CancellationToken ct = default);
	Task<Account?> SignInAsAsync(Guid accountId, bool rememberMe, CancellationToken ct = default);
	CurrentUserContext? GetCurrentUser();
	Guid? GetCurrentAccountId();
	AccountType? GetCurrentAccountType();
	CommunicationStyle GetCurrentCommunicationStyle();
	Task<Account?> GetCurrentAccountAsync(CancellationToken ct = default);
	Task<Account?> GetCurrentAccountFullAsync(CancellationToken ct = default);
	Task<bool> RefreshAsync(CancellationToken ct = default);
	Task LogoutAsync(CancellationToken ct = default);
	Task RevokeAllSessionsAsync(Guid accountId, CancellationToken ct = default);
	Task<List<AuthSessionDto>> GetActiveSessionsAsync(Guid accountId, Guid? currentSessionId, CancellationToken ct = default);
	Task<bool> RevokeSessionAsync(Guid sessionId, Guid accountId, CancellationToken ct = default);
	Task<int> RevokeOtherSessionsAsync(Guid currentSessionId, Guid accountId, CancellationToken ct = default);
}
