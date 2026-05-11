using System.Text.Json;
using BCrypt.Net;
using Microsoft.EntityFrameworkCore;
using server.Data;
using server.Data.Entities;
using server.Dto;
using server.Dto.Mappers;
using server.Infrastructure;

namespace server.Services;





public sealed class AuthService(
    AppDbContext db,
    IHttpContextAccessor http,
    IServiceScopeFactory scopeFactory,
    ILogger<AuthService> logger
) : IAuthService {

    private const HashType EnhancedType = HashType.SHA384;

    public static string HashPassword(string plain, int workFactor = 12) {
        return BCrypt.Net.BCrypt.EnhancedHashPassword(plain, workFactor, EnhancedType);
    }

    public static bool VerifyPassword(in string plainPassword, in string hashedPassword) {
        if (string.IsNullOrEmpty(plainPassword) || string.IsNullOrEmpty(hashedPassword)) return false;
        try {
            // nejdriv enhanced, pak klasicky
            return BCrypt.Net.BCrypt.EnhancedVerify(plainPassword, hashedPassword, EnhancedType)
                   || BCrypt.Net.BCrypt.Verify(plainPassword, hashedPassword);
        } catch (SaltParseException) {
            Program.Application.Logger.LogError("SaltParseException in Utilities.VerifyPassword");
            return false;
        }
    }


    public async Task<Account?> LoginAsync(string identifier, string plainPassword, CancellationToken ct = default) {
        var acc = await db.AccountsEf()
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Email.ToLower() == identifier.ToLower(), ct);
        if (acc == null) return null;

        var dto = acc.ToSessionDto();

        //Console.WriteLine("Login attempt for json: " + json);

        if (!VerifyPassword(plainPassword, acc.PasswordHash)) return null;
        http.HttpContext!.Session.SetString("loggedaccount", JsonSerializer.Serialize(dto));
        http.HttpContext.Items["loggedaccount"] = dto;
        QueueLastActiveUpdate(acc.Id);
        
        // Load full account with relationships for the response
        return await db.AccountsEf()
            .AsNoTracking()
            .AsSplitQuery()
            .FirstOrDefaultAsync(a => a.Id == acc.Id, ct);
    }

    public async Task<Account?> SignInAsAsync(Guid accountId, CancellationToken ct = default) {
        var acc = await db.AccountsEf()
            .AsNoTracking()
            .AsSplitQuery()
            .FirstOrDefaultAsync(a => a.Id == accountId, ct);
        if (acc == null) return null;

        var dto = acc.ToSessionDto();
        http.HttpContext!.Session.SetString("loggedaccount", JsonSerializer.Serialize(dto));
        http.HttpContext.Items["loggedaccount"] = dto;
        QueueLastActiveUpdate(acc.Id);

        return acc;
    }


    public async Task<Account?> ReAuthAsync(CancellationToken ct = default) {
        var json = http.HttpContext?.Session.GetString("loggedaccount");
        if (string.IsNullOrEmpty(json)) return null;

        var sessionAcc = JsonSerializer.Deserialize<AccountSessionDto>(json);
        if (sessionAcc == null) return null;

        // First check password without loading relationships
        var accLight = await db.Accounts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == sessionAcc.Id, ct);
        if (accLight == null || accLight.PasswordHash != sessionAcc.PasswordHash) return null;

        // Keep session data consistent by storing AccountSessionDto instead of full Account
        var sessionDto = accLight.ToSessionDto();
        http.HttpContext!.Items["loggedaccount"] = sessionDto;
        http.HttpContext!.Session.SetString("loggedaccount", JsonSerializer.Serialize(sessionDto));
        if (accLight.LastActiveUtc <= DateTime.UtcNow.AddMinutes(-1))
            QueueLastActiveUpdate(accLight.Id);
        
        // Load full account with relationships for the response
        return await db.AccountsEf()
            .AsNoTracking()
            .AsSplitQuery()
            .FirstOrDefaultAsync(a => a.Id == sessionAcc.Id, ct);
    }


    public async Task<Account?> RegisterAsync(string username, string email, string plainPassword,  CancellationToken ct = default) {
        throw new NotImplementedException();
        /*var existing = await db.Accounts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Username.Equals(username, StringComparison.CurrentCultureIgnoreCase), ct);
        if (existing != null) return null;

        var hashedPassword = HashPassword(plainPassword);

        var acc = new Account {
            Username = username,
            Email = email,
            PasswordHash = hashedPassword,
            DisplayName = username,
            AvatarUrl = null,
        };

        db.Accounts.Add(acc);
        await db.SaveChangesAsync(ct);

        var dto = acc.ToSessionDto();

        http.HttpContext!.Session.SetString("loggedaccount", JsonSerializer.Serialize(dto));
        http.HttpContext.Items["loggedaccount"] = dto;

        // For newly registered accounts, there are no relationships yet, so return the created entity
        return acc;*/
    }


    public async Task<Account?> ReAuthFromContextOrNullAsync(CancellationToken ct = default) {
        if (http.HttpContext == null) return null;
        if (!http.HttpContext.Items.ContainsKey("loggedaccount")) return await ReAuthAsync(ct);

        // Session always contains AccountSessionDto, so we need to re-auth to get full Account
        return await ReAuthAsync(ct);
    }
    
    public async Task<bool> LogoutAsync(CancellationToken ct = default) {
        if (http.HttpContext == null) return false;
        http.HttpContext.Items.Remove("loggedaccount");
        http.HttpContext.Session.Remove("loggedaccount");
        return true;
    }

    private void QueueLastActiveUpdate(Guid accountId) {
        _ = Task.Run(async () => {
            try {
                await using var scope = scopeFactory.CreateAsyncScope();
                var scopedDb = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var nowUtc = DateTime.UtcNow;

                await scopedDb.Accounts
                    .Where(a => a.Id == accountId)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(a => a.LastActiveUtc, nowUtc), CancellationToken.None);
            } catch (Exception ex) {
                logger.LogWarning(ex, "Failed to update LastActiveUtc for account {AccountId}", accountId);
            }
        });
    }
}
