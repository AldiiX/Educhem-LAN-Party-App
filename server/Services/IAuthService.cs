using server.Data.Entities;

namespace server.Services;

public readonly record struct CurrentUserContext(
	Guid Id,
	AccountType Role,
	Guid SessionId,
	CommunicationStyle CommunicationStyle
);

public interface IAuthService {
	Task<Account?> LoginAsync(string identifier, string plainPassword, bool rememberMe, CancellationToken ct = default);
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
}
