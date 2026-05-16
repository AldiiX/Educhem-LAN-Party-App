using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using server.Data;
using server.Data.Entities;
using server.Dto;
using server.Dto.Mappers;
using server.Dto.Requests;
using server.Infrastructure;
using server.Models;
using server.Services;

namespace server.Controllers;






[ApiController]
[Route("api/v1/account")]
public sealed class AccountControllerV1(
	IAuthService auth,
	AppDbContext db,
	IServiceProvider serviceProvider,
	IDataProtectionProvider dataProtectionProvider,
	IDistributedCache distributedCache,
	ReservationCacheService reservationCache
) : Controller {

	private readonly IDataProtector passwordResetProtector = dataProtectionProvider.CreateProtector("account-password-reset");
	private readonly IDataProtector loginLinkProtector = dataProtectionProvider.CreateProtector("account-login-link");
	private static readonly TimeSpan PasswordResetTokenLifetime = TimeSpan.FromMinutes(15);
	private static readonly TimeSpan LoginLinkTokenLifetime = TimeSpan.FromMinutes(30);



	[HttpGet]
	public async Task<IActionResult> GetMyAccount(CancellationToken ct = default) {
		var acc = await auth.ReAuthAsync(ct);
		if(acc == null) return new UnauthorizedResult();

		return Ok(acc.ToDto());
	}

	[HttpGet("dashboard")]
	public async Task<IActionResult> GetDashboard(CancellationToken ct = default) {
		var acc = await auth.ReAuthAsync(ct);
		var nowUtc = DateTime.UtcNow;
		var accounts = await db.AccountsEf()
			.AsNoTracking()
			.ToListAsync(ct);

		var activeNow = accounts.Count(a => a.LastActiveUtc >= nowUtc.AddMinutes(-15));
		var activeToday = accounts.Count(a => a.LastActiveUtc >= nowUtc.Date);
		var reservationsEnabled = accounts.Count(a => a.EnableReservations);
		var staffCount = accounts.Count(a => a.AccountType >= AccountType.Teacher);
		var latestAccounts = acc == null ? [] : accounts
			.OrderByDescending(a => a.CreatedAtUtc)
			.Take(4)
			.Select(a => a.ToProfileDto())
			.ToList();
		var classBreakdown = accounts
			.Where(a => a.EnableReservations && !string.IsNullOrWhiteSpace(a.Class))
			.GroupBy(a => a.Class!)
			.OrderByDescending(group => group.Count())
			.ThenBy(group => group.Key)
			.Take(6)
			.Select(group => new DashboardClassStat(group.Key, group.Count()))
			.ToList();

		return Ok(new DashboardResponse(
			accounts.Count,
			activeNow,
			activeToday,
			reservationsEnabled,
			staffCount,
			latestAccounts,
			classBreakdown
		));
	}

	[HttpGet("all")]
	public async Task<IActionResult> GetAllAccounts(CancellationToken ct = default) {
		var acc = await auth.ReAuthAsync(ct);
		if(acc == null) return new UnauthorizedResult();
		if(!HasRoleAtLeast(acc, AccountType.TeacherOrg))
			return Forbid();

		var accounts = await db.AccountsEf()
			.AsNoTracking()
			.OrderBy(a => a.LastName)
			.ThenBy(a => a.FirstName)
			.Select(a => a.ToDto())
			.ToListAsync(ct);

		return Ok(accounts);
	}

	[HttpPost]
	public async Task<IActionResult> CreateAccount([FromBody] AccountMutationRequest request, CancellationToken ct = default) {
		var acc = await auth.ReAuthAsync(ct);
		if(acc == null) return new UnauthorizedResult();
		if(!HasRoleAtLeast(acc, AccountType.TeacherOrg))
			return Forbid();

		if(string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName) || string.IsNullOrWhiteSpace(request.Email))
			return BadRequest("Missing required account fields.");

		var requestedAccountType = request.AccountType ?? AccountType.Student;
		if(!CanManageRole(acc, requestedAccountType))
			return Forbid();

		School? school = null;
		if(request.SchoolId != null) {
			school = await db.Set<School>().FirstOrDefaultAsync(s => s.Id == request.SchoolId, ct);
			if(school == null) return BadRequest("Unknown school.");
		}

		var password = string.IsNullOrWhiteSpace(request.Password) ? GenerateRandomPassword() : request.Password;
		var account = new Account {
			Id = Guid.Empty,
			FirstName = request.FirstName.Trim(),
			LastName = request.LastName.Trim(),
			Email = request.Email.Trim(),
			PasswordHash = AuthService.HashPassword(password),
			Class = NormalizeOptional(request.Class),
			Gender = request.Gender,
			School = school,
			AccountType = requestedAccountType,
			AvatarUrl = NormalizeOptional(request.AvatarUrl),
			BannerUrl = NormalizeOptional(request.BannerUrl),
			EnableReservations = request.EnableReservations ?? false,
		};

		db.Accounts.Add(account);
		await db.SaveChangesAsync(ct);

		var created = await db.AccountsEf().AsNoTracking().FirstAsync(a => a.Id == account.Id, ct);
		var emailSent = false;
		if(request.SendLoginCredentialsEmail == true) {
			emailSent = await SendCredentialsEmailAsync(
				created,
				created.Email,
				"EDUCHEM LAN Party - přihlašovací údaje",
				"/Views/Emails/UserRegistered.cshtml",
				password,
				created.FirstName,
				created.LastName,
				created.Gender
			);
		}

		return Ok(new AccountMutationResponse(created.ToDto(), emailSent));
	}

	[HttpPut("{id:guid}")]
	public async Task<IActionResult> UpdateAccount(Guid id, [FromBody] AccountMutationRequest request, CancellationToken ct = default) {
		var acc = await auth.ReAuthAsync(ct);
		if(acc == null) return new UnauthorizedResult();
		if(!HasRoleAtLeast(acc, AccountType.TeacherOrg))
			return Forbid();

		var account = await db.AccountsEf().FirstOrDefaultAsync(a => a.Id == id, ct);
		if(account == null) return NotFound();
		if(!CanManageAccount(acc, account))
			return Forbid();

		var requestedAccountType = request.AccountType ?? account.AccountType;
		if(!CanManageRole(acc, requestedAccountType))
			return Forbid();

		if(!string.IsNullOrWhiteSpace(request.FirstName)) account.FirstName = request.FirstName.Trim();
		if(!string.IsNullOrWhiteSpace(request.LastName)) account.LastName = request.LastName.Trim();
		if(!string.IsNullOrWhiteSpace(request.Email)) account.Email = request.Email.Trim();

		account.Class = NormalizeOptional(request.Class);
		account.Gender = request.Gender;
		account.AccountType = requestedAccountType;
		account.AvatarUrl = NormalizeOptional(request.AvatarUrl);
		account.BannerUrl = NormalizeOptional(request.BannerUrl);
		account.EnableReservations = request.EnableReservations ?? account.EnableReservations;

		if(request.SchoolId != null) {
			var school = await db.Set<School>().FirstOrDefaultAsync(s => s.Id == request.SchoolId, ct);
			if(school == null) return BadRequest("Unknown school.");
			account.School = school;
		} else if(request.SchoolId == null) {
			account.School = null;
		}

		string? passwordForEmail = null;
		if(!string.IsNullOrWhiteSpace(request.Password)) {
			passwordForEmail = request.Password;
			account.PasswordHash = AuthService.HashPassword(passwordForEmail);
		} else if(request.SendLoginCredentialsEmail == true) {
			passwordForEmail = GenerateRandomPassword();
			account.PasswordHash = AuthService.HashPassword(passwordForEmail);
		}

		await db.SaveChangesAsync(ct);
		reservationCache.InvalidateReservations();

		var updated = await db.AccountsEf().AsNoTracking().FirstAsync(a => a.Id == account.Id, ct);
		var emailSent = false;
		if(request.SendLoginCredentialsEmail == true && passwordForEmail != null) {
			emailSent = await SendCredentialsEmailAsync(
				updated,
				updated.Email,
				"EDUCHEM LAN Party - nové přihlašovací údaje",
				"/Views/Emails/UserResetPassword.cshtml",
				passwordForEmail,
				updated.FirstName,
				updated.LastName,
				updated.Gender
			);
		}

		return Ok(new AccountMutationResponse(updated.ToDto(), emailSent));
	}

	[HttpDelete("{id:guid}")]
	public async Task<IActionResult> DeleteAccount(Guid id, CancellationToken ct = default) {
		var acc = await auth.ReAuthAsync(ct);
		if(acc == null) return new UnauthorizedResult();
		if(!HasRoleAtLeast(acc, AccountType.TeacherOrg))
			return Forbid();

		var account = await db.Accounts.FirstOrDefaultAsync(a => a.Id == id, ct);
		if(account == null) return NotFound();
		if(!CanManageAccount(acc, account))
			return Forbid();

		db.Accounts.Remove(account);
		await db.SaveChangesAsync(ct);
		reservationCache.InvalidateReservations();
		return NoContent();
	}

	[HttpPost("{id:guid}/reset-password")]
	public async Task<IActionResult> ResetPassword(Guid id, CancellationToken ct = default) {
		var acc = await auth.ReAuthAsync(ct);
		if(acc == null) return new UnauthorizedResult();
		if(!HasRoleAtLeast(acc, AccountType.TeacherOrg))
			return Forbid();

		var account = await db.Accounts.FirstOrDefaultAsync(a => a.Id == id, ct);
		if(account == null) return NotFound();
		if(!CanManageAccount(acc, account))
			return Forbid();

		var password = GenerateRandomPassword();
		account.PasswordHash = AuthService.HashPassword(password);
		await db.SaveChangesAsync(ct);
		reservationCache.InvalidateReservations();

		var emailSent = await SendCredentialsEmailAsync(
			account,
			account.Email,
			"EDUCHEM LAN Party - nové heslo",
			"/Views/Emails/UserResetPassword.cshtml",
			password,
			account.FirstName,
			account.LastName,
			account.Gender
		);

		return Ok(new PasswordResetResponse(emailSent));
	}

	[HttpPost("{id:guid}/impersonate")]
	public async Task<IActionResult> Impersonate(Guid id, CancellationToken ct = default) {
		var acc = await auth.ReAuthAsync(ct);
		if(acc == null) return new UnauthorizedResult();
		if(!HasRoleAtLeast(acc, AccountType.Admin))
			return Forbid();

		var account = await db.Accounts.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id, ct);
		if(account == null) return NotFound();
		if(!CanManageAccount(acc, account))
			return Forbid();

		var signedInAccount = await auth.SignInAsAsync(account.Id, ct);
		if(signedInAccount == null) return NotFound();

		return Ok(signedInAccount.ToDto());
	}

	[HttpPost("login")]
	public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct = default) {
		var acc = await auth.LoginAsync(request.Email, request.PasswordPlain, ct);
		if (acc == null) return Unauthorized();

		return new OkObjectResult(acc.ToDto());
	}

	[HttpPut("me")]
	public async Task<IActionResult> UpdateMyAccount([FromBody] MyAccountMutationRequest request, CancellationToken ct = default) {
		var acc = await auth.ReAuthAsync(ct);
		if(acc == null) return new UnauthorizedResult();

		var account = await db.AccountsEf().FirstOrDefaultAsync(a => a.Id == acc.Id, ct);
		if(account == null) return NotFound();

		account.Gender = request.Gender;
		account.AvatarUrl = NormalizeOptional(request.AvatarUrl);
		account.BannerUrl = NormalizeOptional(request.BannerUrl);

		await db.SaveChangesAsync(ct);
		reservationCache.InvalidateReservations();

		var updated = await db.AccountsEf().AsNoTracking().FirstAsync(a => a.Id == account.Id, ct);
		return Ok(updated.ToDto());
	}

	[HttpPost("me/password")]
	public async Task<IActionResult> ChangeMyPassword([FromBody] ChangeMyPasswordRequest request, CancellationToken ct = default) {
		var acc = await auth.ReAuthAsync(ct);
		if(acc == null) return new UnauthorizedResult();

		if(string.IsNullOrWhiteSpace(request.OldPassword) || string.IsNullOrWhiteSpace(request.NewPassword))
			return BadRequest("Missing password.");

		var account = await db.Accounts.FirstOrDefaultAsync(a => a.Id == acc.Id, ct);
		if(account == null) return NotFound();
		if(!AuthService.VerifyPassword(request.OldPassword, account.PasswordHash))
			return BadRequest("Invalid old password.");

		account.PasswordHash = AuthService.HashPassword(request.NewPassword);
		await db.SaveChangesAsync(ct);
		await auth.LogoutAsync(ct);

		return NoContent();
	}

	[HttpPost("forgot-password")]
	public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken ct = default) {
		if(string.IsNullOrWhiteSpace(request.Email))
			return BadRequest("Missing email.");

		var email = request.Email.Trim();
		var account = await db.Accounts.FirstOrDefaultAsync(a => a.Email.ToLower() == email.ToLower(), ct);
		if(account == null) return Ok(new PasswordResetResponse(false));

		var token = CreatePasswordResetToken(account);
		var resetLink = GetPasswordResetLink(token);

		var emailSent = await SendPasswordResetLinkEmailAsync(
			account.Email,
			"EDUCHEM LAN Party - reset hesla",
			"/Views/Emails/UserForgotPassword.cshtml",
			resetLink,
			account.FirstName,
			account.LastName,
			account.Gender
		);

		return Ok(new PasswordResetResponse(emailSent));
	}

	[HttpPost("reset-password")]
	public async Task<IActionResult> ConfirmPasswordReset([FromBody] ConfirmPasswordResetRequest request, CancellationToken ct = default) {
		if(string.IsNullOrWhiteSpace(request.Token) || string.IsNullOrWhiteSpace(request.NewPassword))
			return BadRequest("Missing reset data.");

		var resetToken = ReadPasswordResetToken(request.Token);
		if(resetToken == null) return BadRequest("Invalid reset token.");
		if(DateTime.UtcNow - resetToken.CreatedAtUtc > PasswordResetTokenLifetime)
			return BadRequest("Reset token expired.");

		var account = await db.Accounts.FirstOrDefaultAsync(a => a.Id == resetToken.AccountId, ct);
		if(account == null) return BadRequest("Invalid reset token.");
		if(account.PasswordHash != resetToken.PasswordHash)
			return BadRequest("Reset token expired.");

		account.PasswordHash = AuthService.HashPassword(request.NewPassword);
		await db.SaveChangesAsync(ct);

		return NoContent();
	}

	[HttpGet("login-link")]
	public async Task<IActionResult> LoginLink([FromQuery] string token, CancellationToken ct = default) {
		var loginToken = ReadLoginLinkToken(token);
		if(loginToken == null) return Redirect("/app/login");
		if(DateTime.UtcNow - loginToken.CreatedAtUtc > LoginLinkTokenLifetime) return Redirect("/app/login");
		if(await distributedCache.GetStringAsync(GetUsedLoginLinkCacheKey(loginToken.TokenId), ct) != null) return Redirect("/app/login");

		var account = await db.Accounts
			.AsNoTracking()
			.FirstOrDefaultAsync(a => a.Id == loginToken.AccountId, ct);
		if(account == null || account.PasswordHash != loginToken.PasswordHash) return Redirect("/app/login");

		var acc = await auth.SignInAsAsync(account.Id, ct);
		if(acc == null) return Redirect("/app/login");

		await distributedCache.SetStringAsync(
			GetUsedLoginLinkCacheKey(loginToken.TokenId),
			"1",
			new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = LoginLinkTokenLifetime },
			ct
		);

		return Redirect("/app");
	}

	[HttpPost("logout")]
	public async Task<IActionResult> Logout(CancellationToken ct = default) {
		await auth.LogoutAsync(ct);
		return NoContent();
	}

	[HttpPut("me/achievements/{id:guid}")]
	public async Task<IActionResult> UpdateMyAchievementVisibility(Guid id, [FromBody] AccountAchievementVisibilityRequest request, CancellationToken ct = default) {
		var acc = await auth.ReAuthAsync(ct);
		if(acc == null) return new UnauthorizedResult();

		var entry = await db.AccountAchievements
			.Include(x => x.Achievement)
			.FirstOrDefaultAsync(x => x.Id == id && x.AccountId == acc.Id, ct);
		if(entry == null) return NotFound();

		entry.IsHidden = request.IsHidden;
		await db.SaveChangesAsync(ct);

		var updated = await db.AccountsEf().AsNoTracking().FirstAsync(a => a.Id == acc.Id, ct);
		return Ok(updated.ToDto());
	}

	[HttpPut("me/badges/{id:guid}")]
	public async Task<IActionResult> UpdateMyBadgeTakenOut(Guid id, [FromBody] AccountBadgeTakeoutRequest request, CancellationToken ct = default) {
		var acc = await auth.ReAuthAsync(ct);
		if(acc == null) return new UnauthorizedResult();

		var entry = await db.AccountBadges
			.Include(x => x.Badge)
			.FirstOrDefaultAsync(x => x.Id == id && x.AccountId == acc.Id, ct);
		if(entry == null) return NotFound();

		if(request.IsTakenOut && !entry.IsTakenOut) {
			var takenOutCount = await db.AccountBadges
				.AsNoTracking()
				.CountAsync(x => x.AccountId == acc.Id && x.IsTakenOut, ct);
			if(takenOutCount >= 3) return BadRequest("Na profilu mohou být maximálně 3 badge.");
		}

		entry.IsTakenOut = request.IsTakenOut;
		await db.SaveChangesAsync(ct);

		var updated = await db.AccountsEf().AsNoTracking().FirstAsync(a => a.Id == acc.Id, ct);
		return Ok(updated.ToDto());
	}

	public sealed record AccountAchievementVisibilityRequest(bool IsHidden);
	public sealed record AccountBadgeTakeoutRequest(bool IsTakenOut);

	public sealed record AccountMutationRequest(
		string? FirstName,
		string? LastName,
		string? Email,
		string? Class,
		Gender? Gender,
		ushort? SchoolId,
		AccountType? AccountType,
		string? AvatarUrl,
		string? BannerUrl,
		bool? EnableReservations,
		bool? SendLoginCredentialsEmail,
		string? Password
	);

	public sealed record AccountMutationResponse(AccountDto Account, bool LoginCredentialsEmailSent = false);
	public sealed record PasswordResetResponse(bool LoginCredentialsEmailSent);
	public sealed record DashboardResponse(
		int TotalAccounts,
		int ActiveNow,
		int ActiveToday,
		int ReservationsEnabled,
		int StaffCount,
		IReadOnlyList<ProfileDto> LatestAccounts,
		IReadOnlyList<DashboardClassStat> ClassBreakdown
	);
	public sealed record DashboardClassStat(string Class, int Count);
	public sealed record MyAccountMutationRequest(Gender? Gender, string? AvatarUrl, string? BannerUrl);
	public sealed record ChangeMyPasswordRequest(string OldPassword, string NewPassword);
	public sealed record ForgotPasswordRequest(string Email);
	public sealed record ConfirmPasswordResetRequest(string Token, string NewPassword);
	private sealed record PasswordResetToken(Guid AccountId, string PasswordHash, DateTime CreatedAtUtc);
	private sealed record LoginLinkToken(Guid AccountId, string PasswordHash, DateTime CreatedAtUtc, string TokenId);

	private static string? NormalizeOptional(string? value) {
		return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
	}

	private static bool HasRoleAtLeast(Account account, AccountType accountType) {
		return account.AccountType >= accountType;
	}

	private static bool CanManageRole(Account actor, AccountType targetAccountType) {
		return actor.AccountType == AccountType.SuperAdmin || actor.AccountType > targetAccountType;
	}

	private static bool CanManageAccount(Account actor, Account target) {
		return CanManageRole(actor, target.AccountType);
	}

	private static string GenerateRandomPassword(int length = 24) {
		const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789ěščřž!@*";
		var random = new Random();
		var passwordBuilder = new StringBuilder(length);
		for (var i = 0; i < length; i++) {
			var randomIndex = random.Next(chars.Length);
			passwordBuilder.Append(chars[randomIndex]);
		}
		return passwordBuilder.ToString();
	}

	private async Task<bool> SendCredentialsEmailAsync(
		Account account,
		string email,
		string subject,
		string viewPath,
		string password,
		string? firstName,
		string? lastName,
		Gender? gender
	) {
		var webLink = GetLoginLink(account);
		var model = new EmailUserRegisterModel(password, webLink, email, firstName, lastName, gender);
		var fallbackBody = $"Email: {email}\nHeslo: {password}\n{webLink}";
		return await EmailService.SendHtmlEmailAsync(email, subject, viewPath, model, serviceProvider, fallbackBody);
	}

	private async Task<bool> SendPasswordResetLinkEmailAsync(
		string email,
		string subject,
		string viewPath,
		string resetLink,
		string? firstName,
		string? lastName,
		Gender? gender
	) {
		var model = new EmailPasswordResetLinkModel(resetLink, email, firstName, lastName, gender);
		var fallbackBody = $"Reset link: {resetLink}";
		return await EmailService.SendHtmlEmailAsync(email, subject, viewPath, model, serviceProvider, fallbackBody);
	}

	private PasswordResetToken CreatePasswordResetToken(Account account) {
		return new PasswordResetToken(account.Id, account.PasswordHash, DateTime.UtcNow);
	}

	private string GetPasswordResetLink(PasswordResetToken token) {
		var protectedToken = passwordResetProtector.Protect(JsonSerializer.Serialize(token));
		return BuildAbsoluteUrl($"/app/reset-password?token={Uri.EscapeDataString(protectedToken)}");
	}

	private PasswordResetToken? ReadPasswordResetToken(string token) {
		try {
			var json = passwordResetProtector.Unprotect(token);
			return JsonSerializer.Deserialize<PasswordResetToken>(json);
		} catch {
			return null;
		}
	}

	private string GetLoginLink(Account account) {
		var loginToken = new LoginLinkToken(account.Id, account.PasswordHash, DateTime.UtcNow, Guid.NewGuid().ToString("N"));
		var protectedToken = loginLinkProtector.Protect(JsonSerializer.Serialize(loginToken));
		return BuildAbsoluteUrl($"/api/v1/account/login-link?token={Uri.EscapeDataString(protectedToken)}");
	}

	private LoginLinkToken? ReadLoginLinkToken(string token) {
		try {
			var json = loginLinkProtector.Unprotect(token);
			return JsonSerializer.Deserialize<LoginLinkToken>(json);
		} catch {
			return null;
		}
	}

	private static string GetUsedLoginLinkCacheKey(string tokenId) {
		return $"login-link:{tokenId}";
	}

	private string BuildAbsoluteUrl(string pathAndQuery) {
		if(Program.ENV.TryGetValue("WEB_URL", out var webUrl) && !string.IsNullOrWhiteSpace(webUrl)) {
			return $"{webUrl.TrimEnd('/')}{pathAndQuery}";
		}

		var request = HttpContext.Request;
		return $"{request.Scheme}://{request.Host}{pathAndQuery}";
	}
}
