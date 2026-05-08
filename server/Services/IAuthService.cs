using server.Data.Entities;

namespace server.Services;

public interface IAuthService {
    Task<Account?> LoginAsync(string identifier, string plainPassword, CancellationToken ct = default);
    Task<Account?> ReAuthAsync(CancellationToken ct = default);
    Task<Account?> RegisterAsync(string username, string plainPassword, string email, CancellationToken ct = default);
    
    Task<Account?> ReAuthFromContextOrNullAsync(CancellationToken ct = default);
    Task<bool> LogoutAsync(CancellationToken ct = default);
}