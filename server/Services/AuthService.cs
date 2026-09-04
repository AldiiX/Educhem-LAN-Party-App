using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using BCrypt.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;
using server.Data;
using server.Data.Entities;
using server.Dto;
using server.Dto.Mappers;
using server.Infrastructure;

namespace server.Services;

internal sealed class AuthService(
	AppDbContext db,
	IHttpContextAccessor http,
	IServiceScopeFactory scopeFactory,
	IOAuthService oauth,
	ILogger<AuthService> logger,
	JwtAuthConfiguration jwt,
	IMemoryCache cache
) : IAuthService {
	private const HashType EnhancedType = HashType.SHA384;
	private const string SessionIdClaim = "sid";
	private const string CommunicationStyleClaim = "communication_style";

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

	public async Task<Account?> LoginAsync(string identifier, string plainPassword, bool rememberMe, CancellationToken ct = default) {
		var normalizedIdentifier = AccountEmail.TryNormalize(identifier, out var normalized)
			? normalized
			: identifier.Trim().ToLowerInvariant();

		var clientInfo = ClientInfoExtractor.Extract(http.HttpContext);
		var clientIp = clientInfo.IpAddress ?? "unknown";
		var failureKey = $"login:failed:{clientIp}:{normalizedIdentifier}";
		if (cache.TryGetValue<int>(failureKey, out var failures) && failures >= 5) {
			logger.LogWarning("Příliš mnoho neúspěšných pokusů o přihlášení k účtu {Email} z IP {Ip}", normalizedIdentifier, clientIp);
			return null;
		}

		var account = await db.AccountsEf()
			.AsNoTracking()
			.FirstOrDefaultAsync(item => item.Email == normalizedIdentifier, ct);
		if (account == null || !VerifyPassword(plainPassword, account.PasswordHash)) {
			var currentFailures = (cache.TryGetValue<int>(failureKey, out var f) ? f : 0) + 1;
			cache.Set(failureKey, currentFailures, TimeSpan.FromMinutes(5));
			return null;
		}

		cache.Remove(failureKey);

		var verifiedPasswordHash = account.PasswordHash;
		var verifiedEmail = account.Email;
		await RefreshOAuthConnectionsAsync(account, true, ct);
		account = await LoadAccountAsync(account.Id, ct);
		if (account == null || account.PasswordHash != verifiedPasswordHash || account.Email != verifiedEmail) return null;

		await ReplaceCurrentSessionAsync(account, rememberMe, ct);
		QueueLastActiveUpdate(account.Id);
		return account;
	}

	public async Task<Account?> SignInAsAsync(Guid accountId, bool rememberMe, CancellationToken ct = default) {
		var account = await LoadAccountAsync(accountId, ct);
		if (account == null) return null;

		await ReplaceCurrentSessionAsync(account, rememberMe, ct);
		QueueLastActiveUpdate(account.Id);
		return account;
	}

	public CurrentUserContext? GetCurrentUser() {
		var context = http.HttpContext;
		if (context?.User.Identity?.IsAuthenticated != true) return null;

		var idValue = context.User.FindFirstValue(JwtRegisteredClaimNames.Sub)
			?? context.User.FindFirstValue(ClaimTypes.NameIdentifier);
		if (!Guid.TryParse(idValue, out var accountId)) return null;

		var roleValue = context.User.FindFirstValue(ClaimTypes.Role);
		if (!Enum.TryParse<AccountType>(roleValue, out var role)) return null;

		var sessionValue = context.User.FindFirstValue(SessionIdClaim);
		Guid.TryParse(sessionValue, out var sessionId);

		var styleValue = context.User.FindFirstValue(CommunicationStyleClaim);
		var communicationStyle = Enum.TryParse<CommunicationStyle>(styleValue, out var parsedStyle)
			? parsedStyle
			: CommunicationStyle.Formal;

		return new CurrentUserContext(accountId, role, sessionId, communicationStyle);
	}

	public AccountType? GetCurrentAccountType() {
		var context = http.HttpContext;
		if (context?.User.Identity?.IsAuthenticated != true) return null;

		var value = context.User.FindFirstValue(ClaimTypes.Role);
		return Enum.TryParse<AccountType>(value, out var role) ? role : null;
	}

	public CommunicationStyle GetCurrentCommunicationStyle() {
		var context = http.HttpContext;
		if (context?.User.Identity?.IsAuthenticated != true) return CommunicationStyle.Formal;

		var value = context.User.FindFirstValue(CommunicationStyleClaim);
		return Enum.TryParse<CommunicationStyle>(value, out var style) ? style : CommunicationStyle.Formal;
	}

	public async Task<Account?> GetCurrentAccountAsync(CancellationToken ct = default) {
		var accountId = GetCurrentAccountId();
		if (accountId == null) return null;

		var account = await db.Accounts
			.IgnoreAutoIncludes()
			.AsNoTracking()
			.FirstOrDefaultAsync(item => item.Id == accountId.Value, ct);
		if (account != null && account.LastActiveUtc <= DateTime.UtcNow.AddMinutes(-1)) {
			QueueLastActiveUpdate(account.Id);
		}

		return account;
	}

	public Guid? GetCurrentAccountId() {
		var context = http.HttpContext;
		if (context?.User.Identity?.IsAuthenticated != true) return null;

		var value = context.User.FindFirstValue(JwtRegisteredClaimNames.Sub)
			?? context.User.FindFirstValue(ClaimTypes.NameIdentifier);
		return Guid.TryParse(value, out var accountId) ? accountId : null;
	}

	public async Task<Account?> GetCurrentAccountFullAsync(CancellationToken ct = default) {
		var accountId = GetCurrentAccountId();
		return accountId == null ? null : await LoadAccountAsync(accountId.Value, ct);
	}

	public async Task<bool> RefreshAsync(CancellationToken ct = default) {
		var context = http.HttpContext;
		var rawToken = context?.Request.Cookies[AuthCookieNames.Refresh];
		if (!TryReadRefreshToken(rawToken, out var sessionId)) {
			ClearAuthCookies();
			return false;
		}

		var session = await db.AuthSessions
			.Include(item => item.Account)
				.ThenInclude(account => account.OAuthConnections)
			.AsSplitQuery()
			.FirstOrDefaultAsync(item => item.Id == sessionId, ct);
		if (session == null || session.RevokedAtUtc != null || session.ExpiresAtUtc <= DateTime.UtcNow
			|| !RefreshTokenMatches(rawToken!, session.RefreshTokenHash)) {
			ClearAuthCookies();
			return false;
		}

		await RefreshOAuthConnectionsAsync(session.Account, false, ct);
		var account = await db.Accounts.AsNoTracking().FirstOrDefaultAsync(item => item.Id == session.AccountId, ct);
		if (account == null) {
			session.RevokedAtUtc = DateTime.UtcNow;
			await db.SaveChangesAsync(ct);
			ClearAuthCookies();
			return false;
		}

		var clientInfo = ClientInfoExtractor.Extract(context);
		session.LastActiveUtc = DateTime.UtcNow;
		if (!string.IsNullOrWhiteSpace(clientInfo.IpAddress)) session.IpAddress = clientInfo.IpAddress;
		if (!string.IsNullOrWhiteSpace(clientInfo.City)) session.City = clientInfo.City;
		if (!string.IsNullOrWhiteSpace(clientInfo.Country)) session.Country = clientInfo.Country;
		await db.SaveChangesAsync(ct);

		AppendAccessCookie(account, session);
		QueueLastActiveUpdate(account.Id);
		return true;
	}

	public async Task LogoutAsync(CancellationToken ct = default) {
		await RevokeCurrentSessionAsync(ct);
		ClearAuthCookies();
	}

	public async Task RevokeAllSessionsAsync(Guid accountId, CancellationToken ct = default) {
		var nowUtc = DateTime.UtcNow;
		await db.AuthSessions
			.Where(item => item.AccountId == accountId && item.RevokedAtUtc == null)
			.ExecuteUpdateAsync(setters => setters.SetProperty(item => item.RevokedAtUtc, nowUtc), ct);
	}

	public async Task<List<AuthSessionDto>> GetActiveSessionsAsync(Guid accountId, Guid? currentSessionId, CancellationToken ct = default) {
		var nowUtc = DateTime.UtcNow;
		var sessions = await db.AuthSessions
			.AsNoTracking()
			.Where(item => item.AccountId == accountId && item.RevokedAtUtc == null && item.ExpiresAtUtc > nowUtc)
			.OrderByDescending(item => item.LastActiveUtc)
			.ToListAsync(ct);

		return sessions.Select(item => item.ToDto(currentSessionId)).ToList();
	}

	public async Task<bool> RevokeSessionAsync(Guid sessionId, Guid accountId, CancellationToken ct = default) {
		var session = await db.AuthSessions.FirstOrDefaultAsync(item => item.Id == sessionId && item.AccountId == accountId && item.RevokedAtUtc == null, ct);
		if (session == null) return false;

		session.RevokedAtUtc = DateTime.UtcNow;
		await db.SaveChangesAsync(ct);
		return true;
	}

	public async Task<int> RevokeOtherSessionsAsync(Guid currentSessionId, Guid accountId, CancellationToken ct = default) {
		var nowUtc = DateTime.UtcNow;
		return await db.AuthSessions
			.Where(item => item.AccountId == accountId && item.Id != currentSessionId && item.RevokedAtUtc == null)
			.ExecuteUpdateAsync(setters => setters.SetProperty(item => item.RevokedAtUtc, nowUtc), ct);
	}

	private async Task ReplaceCurrentSessionAsync(Account account, bool rememberMe, CancellationToken ct) {
		await RevokeCurrentSessionAsync(ct);

		var nowUtc = DateTime.UtcNow;
		var sessionId = Guid.CreateVersion7();
		var secret = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(32));
		var rawRefreshToken = $"{sessionId:N}.{secret}";
		var clientInfo = ClientInfoExtractor.Extract(http.HttpContext);
		var session = new AuthSession {
			Id = sessionId,
			AccountId = account.Id,
			RefreshTokenHash = HashRefreshToken(rawRefreshToken),
			CreatedAtUtc = nowUtc,
			ExpiresAtUtc = nowUtc.Add(JwtAuthConfiguration.RefreshTokenLifetime),
			RevokedAtUtc = null,
			IsPersistent = rememberMe,
			IpAddress = clientInfo.IpAddress,
			UserAgent = clientInfo.UserAgent,
			DeviceType = clientInfo.DeviceType,
			Browser = clientInfo.Browser,
			OperatingSystem = clientInfo.OperatingSystem,
			City = clientInfo.City,
			Country = clientInfo.Country,
			LastActiveUtc = nowUtc,
		};

		db.AuthSessions.Add(session);
		await db.SaveChangesAsync(ct);

		AppendAccessCookie(account, session);
		AppendRefreshCookie(rawRefreshToken, session);
	}

	private async Task RevokeCurrentSessionAsync(CancellationToken ct) {
		var rawToken = http.HttpContext?.Request.Cookies[AuthCookieNames.Refresh];
		if (!TryReadRefreshToken(rawToken, out var sessionId)) return;

		var session = await db.AuthSessions.FirstOrDefaultAsync(item => item.Id == sessionId, ct);
		if (session == null || session.RevokedAtUtc != null || !RefreshTokenMatches(rawToken!, session.RefreshTokenHash)) return;

		session.RevokedAtUtc = DateTime.UtcNow;
		await db.SaveChangesAsync(ct);
	}

	private void AppendAccessCookie(Account account, AuthSession session) {
		var context = http.HttpContext ?? throw new InvalidOperationException("HTTP context neni dostupny.");
		var nowUtc = DateTime.UtcNow;
		var expiresAtUtc = nowUtc.Add(JwtAuthConfiguration.AccessTokenLifetime);
		var claims = new[] {
			new Claim(JwtRegisteredClaimNames.Sub, account.Id.ToString()),
			new Claim(ClaimTypes.NameIdentifier, account.Id.ToString()),
			new Claim(ClaimTypes.Role, account.AccountType.ToString()),
			new Claim(SessionIdClaim, session.Id.ToString()),
			new Claim(CommunicationStyleClaim, account.CommunicationStyle.ToString()),
			new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
			new Claim(JwtRegisteredClaimNames.Iat, EpochTime.GetIntDate(nowUtc).ToString(), ClaimValueTypes.Integer64),
		};
		var token = new JwtSecurityToken(
			JwtAuthConfiguration.Issuer,
			JwtAuthConfiguration.Audience,
			claims,
			notBefore: nowUtc,
			expires: expiresAtUtc,
			signingCredentials: jwt.SigningCredentials
		);

		context.Response.Cookies.Append(
			AuthCookieNames.Access,
			new JwtSecurityTokenHandler().WriteToken(token),
			CreateCookieOptions(true, session.IsPersistent ? expiresAtUtc : null)
		);
		// klient vidi jen expiraci, ne samotnej token; hint zustava i po vyprseni access cookie
		context.Response.Cookies.Append(
			AuthCookieNames.AccessExpires,
			EpochTime.GetIntDate(expiresAtUtc).ToString(System.Globalization.CultureInfo.InvariantCulture),
			CreateCookieOptions(false, session.IsPersistent ? session.ExpiresAtUtc : null)
		);
	}

	private void AppendRefreshCookie(string rawRefreshToken, AuthSession session) {
		var context = http.HttpContext ?? throw new InvalidOperationException("HTTP context neni dostupny.");
		context.Response.Cookies.Append(
			AuthCookieNames.Refresh,
			rawRefreshToken,
			CreateCookieOptions(true, session.IsPersistent ? session.ExpiresAtUtc : null)
		);
	}

	private void ClearAuthCookies() {
		var context = http.HttpContext;
		if (context == null) return;
		context.Response.Cookies.Delete(AuthCookieNames.Access, CreateCookieOptions(true, null));
		context.Response.Cookies.Delete(AuthCookieNames.AccessExpires, CreateCookieOptions(false, null));
		context.Response.Cookies.Delete(AuthCookieNames.Refresh, CreateCookieOptions(true, null));
	}

	private static CookieOptions CreateCookieOptions(bool httpOnly, DateTime? expiresAtUtc) {
		return new CookieOptions {
			HttpOnly = httpOnly,
			Secure = !Program.DevelopmentMode,
			SameSite = SameSiteMode.Lax,
			Path = "/",
			IsEssential = true,
			Expires = expiresAtUtc.HasValue ? new DateTimeOffset(expiresAtUtc.Value) : null,
		};
	}

	private static bool TryReadRefreshToken(string? rawToken, out Guid sessionId) {
		sessionId = Guid.Empty;
		if (string.IsNullOrWhiteSpace(rawToken)) return false;
		var separator = rawToken.IndexOf('.');
		return separator == 32 && Guid.TryParseExact(rawToken[..separator], "N", out sessionId)
			&& rawToken.Length > separator + 1;
	}

	private static bool RefreshTokenMatches(string rawToken, string storedHash) {
		var actual = Encoding.ASCII.GetBytes(HashRefreshToken(rawToken));
		var expected = Encoding.ASCII.GetBytes(storedHash);
		return actual.Length == expected.Length && CryptographicOperations.FixedTimeEquals(actual, expected);
	}

	private static string HashRefreshToken(string rawToken) {
		return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));
	}

	private async Task<Account?> LoadAccountAsync(Guid accountId, CancellationToken ct) {
		return await db.AccountsEf()
			.AsNoTracking()
			.AsSplitQuery()
			.FirstOrDefaultAsync(item => item.Id == accountId, ct);
	}

	private async Task RefreshOAuthConnectionsAsync(Account account, bool force, CancellationToken ct) {
		if (account.OAuthConnections.Any(connection => connection.Provider == OAuthProvider.Discord)) {
			await oauth.EnsureDiscordConnectionAsync(account.Id, force, ct);
		}
		if (account.OAuthConnections.Any(connection => connection.Provider == OAuthProvider.Steam)) {
			await oauth.EnsureSteamConnectionAsync(account.Id, force, ct);
		}
	}

	private void QueueLastActiveUpdate(Guid accountId) {
		_ = Task.Run(async () => {
			try {
				await using var scope = scopeFactory.CreateAsyncScope();
				var scopedDb = scope.ServiceProvider.GetRequiredService<AppDbContext>();
				var nowUtc = DateTime.UtcNow;

				await scopedDb.Accounts
					.Where(account => account.Id == accountId && account.LastActiveUtc <= nowUtc.AddMinutes(-1))
					.ExecuteUpdateAsync(setters => setters.SetProperty(account => account.LastActiveUtc, nowUtc), CancellationToken.None);
			} catch (Exception exception) {
				logger.LogWarning(exception, "Failed to update LastActiveUtc for account {AccountId}", accountId);
			}
		});
	}
}
