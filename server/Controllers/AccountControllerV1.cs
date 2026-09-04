using System.Text;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
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
[TypeFilter(typeof(AccountWriteExceptionFilter))]
[Route("api/v1/account")]
public sealed class AccountControllerV1(
	IAuthService auth,
	AppDbContext db,
	IServiceProvider serviceProvider,
	ReservationCacheService reservationCache,
	IDbLoggerService dbLogger,
	IOAuthService oauth,
	IMemoryCache cache
) : Controller {
	private const int AdministrationPageSize = 25;

	private static readonly TimeSpan PasswordResetTokenLifetime = TimeSpan.FromMinutes(15);
	private static readonly TimeSpan LoginLinkTokenLifetime = TimeSpan.FromMinutes(30);
	private static readonly TimeSpan RegistrationLoginLinkTokenLifetime = TimeSpan.FromHours(24);

	[HttpGet]
	[Authorize]
	public async Task<IActionResult> GetMyAccount(CancellationToken ct = default) {
		var acc = await auth.GetCurrentAccountFullAsync(ct);
		if(acc == null) return new UnauthorizedResult();

		return Ok(acc.ToDto());
	}

	[HttpGet("dashboard")]
	public async Task<IActionResult> GetDashboard(CancellationToken ct = default) {
		var isAuthenticated = User.Identity?.IsAuthenticated == true;
		var nowUtc = DateTime.UtcNow;
		var accounts = await db.AccountsEf()
			.AsNoTracking()
			.ToListAsync(ct);

		var activeNow = accounts.Count(a => a.LastActiveUtc >= nowUtc.AddMinutes(-15));
		var activeToday = accounts.Count(a => a.LastActiveUtc >= nowUtc.Date);
		var reservationsEnabled = accounts.Count(a => a.EnableReservations);
		var staffCount = accounts.Count(a => a.AccountType >= AccountType.Teacher);
		var latestAccounts = !isAuthenticated ? [] : accounts
			.OrderByDescending(a => a.CreatedAtUtc)
			.Take(4)
			.Select(a => a.ToProfileDto())
			.ToList();
		var classBreakdown = accounts
			.Where(a => a.EnableReservations && a.Enrollment?.Class != null)
			.GroupBy(a => new {
				a.Enrollment!.SchoolId,
				a.Enrollment.Class,
			})
			.Select(group => new DashboardClassStat(
				group.First().Enrollment!.School.ToDto(false),
				group.Key.Class!,
				group.Count()
			))
			.OrderByDescending(item => item.Count)
			.ThenBy(item => item.School.ShortName)
			.ThenBy(item => item.Class)
			.Take(6)
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
	[Authorize(Policy = AuthPolicies.TeacherOrg)]
	public async Task<IActionResult> GetAllAccounts(
		[FromQuery] int page = 1,
		[FromQuery(Name = "q")] string? search = null,
		[FromQuery] string[]? accountType = null,
		[FromQuery] string[]? gender = null,
		[FromQuery(Name = "class")] string[]? classes = null,
		[FromQuery] ushort[]? school = null,
		[FromQuery] string? reservations = null,
		[FromQuery] string? sort = null,
		[FromQuery] string? direction = null,
		CancellationToken ct = default
	) {
		var accountTypes = EnumFilters.ParseEnumFilters<AccountType>(accountType);
		var genders = EnumFilters.ParseEnumFilters<Gender>(gender);
		var classFilters = classes?
			.Select(value => value.Trim())
			.Where(value => value.Length > 0)
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.ToArray() ?? [];
		var schoolFilters = school?.Distinct().ToArray() ?? [];
		var query = db.AccountsEf().AsNoTracking();

		if(!string.IsNullOrWhiteSpace(search)) {
			var normalizedSearch = search.Trim().ToLower();
			var studentMaleMatches = "student".Contains(normalizedSearch);
			var studentFemaleMatches = "studentka".Contains(normalizedSearch);
			var teacherMaleMatches = "učitel".Contains(normalizedSearch);
			var teacherFemaleMatches = "učitelka".Contains(normalizedSearch);
			var teacherOrgMaleMatches = "učitel (org)".Contains(normalizedSearch);
			var teacherOrgFemaleMatches = "učitelka (org)".Contains(normalizedSearch);
			var adminMaleMatches = "administrátor".Contains(normalizedSearch);
			var adminFemaleMatches = "administrátorka".Contains(normalizedSearch);
			var superAdminMaleMatches = "administrátor (su)".Contains(normalizedSearch);
			var superAdminFemaleMatches = "administrátorka (su)".Contains(normalizedSearch);

			query = query.Where(account =>
				(account.FirstName + " " + account.LastName).ToLower().Contains(normalizedSearch)
				|| account.Email.ToLower().Contains(normalizedSearch)
				|| (account.Enrollment != null && account.Enrollment.Class != null && account.Enrollment.Class.ToLower().Contains(normalizedSearch))
				|| (account.Enrollment != null && account.Enrollment.School.DisplayName.ToLower().Contains(normalizedSearch))
				|| account.OAuthConnections.Any(connection =>
					connection.Provider == OAuthProvider.Discord
					&& connection.Username != null
					&& connection.Username.ToLower().Contains(normalizedSearch)
				)
				|| (account.AccountType == AccountType.Student && (
					(account.Gender == Gender.Female && studentFemaleMatches)
					|| (account.Gender != Gender.Female && studentMaleMatches)
				))
				|| (account.AccountType == AccountType.Teacher && (
					(account.Gender == Gender.Female && teacherFemaleMatches)
					|| (account.Gender != Gender.Female && teacherMaleMatches)
				))
				|| (account.AccountType == AccountType.TeacherOrg && (
					(account.Gender == Gender.Female && teacherOrgFemaleMatches)
					|| (account.Gender != Gender.Female && teacherOrgMaleMatches)
				))
				|| (account.AccountType == AccountType.Admin && (
					(account.Gender == Gender.Female && adminFemaleMatches)
					|| (account.Gender != Gender.Female && adminMaleMatches)
				))
				|| (account.AccountType == AccountType.SuperAdmin && (
					(account.Gender == Gender.Female && superAdminFemaleMatches)
					|| (account.Gender != Gender.Female && superAdminMaleMatches)
				))
			);
		}

		if(accountTypes.Length > 0) query = query.Where(account => accountTypes.Contains(account.AccountType));
		if(genders.Length > 0) query = query.Where(account => account.Gender.HasValue && genders.Contains(account.Gender.Value));
		if(classFilters.Length > 0) query = query.Where(account => account.Enrollment != null && account.Enrollment.Class != null && classFilters.Contains(account.Enrollment.Class));
		if(schoolFilters.Length > 0) query = query.Where(account => account.Enrollment != null && schoolFilters.Contains(account.Enrollment.SchoolId));
		if(string.Equals(reservations, "enabled", StringComparison.OrdinalIgnoreCase)) query = query.Where(account => account.EnableReservations);
		if(string.Equals(reservations, "disabled", StringComparison.OrdinalIgnoreCase)) query = query.Where(account => !account.EnableReservations);

		var totalItems = await db.Accounts.AsNoTracking().CountAsync(ct);
		var totalEntries = await query.CountAsync(ct);
		var totalPages = totalEntries == 0 ? 0 : (int)Math.Ceiling(totalEntries / (double)AdministrationPageSize);
		var currentPage = totalPages == 0 ? 1 : Math.Clamp(page, 1, totalPages);
		var orderedQuery = OrderAccounts(query, sort, direction);
		var accountEntities = await orderedQuery
			.Skip((currentPage - 1) * AdministrationPageSize)
			.Take(AdministrationPageSize)
			.AsSplitQuery()
			.ToListAsync(ct);
		var filterOptions = await BuildAccountFilterOptionsAsync(ct);

		return Ok(new AdministrationAccountsPageDto(
			accountEntities.Select(account => account.ToDto()).ToList(),
			new PaginationDto(currentPage, AdministrationPageSize, totalEntries, totalPages),
			totalItems,
			filterOptions
		));
	}

	[HttpPost]
	[Authorize(Policy = AuthPolicies.TeacherOrg)]
	public async Task<IActionResult> CreateAccount([FromBody] AccountMutationRequest request, CancellationToken ct = default) {
		var user = auth.GetCurrentUser();
		if(user == null) return new UnauthorizedResult();
		if(!HasRoleAtLeast(user.Value.Role, AccountType.TeacherOrg))
			return Forbid();

		if(string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName) || string.IsNullOrWhiteSpace(request.Email))
			return BadRequest("Missing required account fields.");
		if (!AccountEmail.TryNormalize(request.Email, out var normalizedEmail)
			|| await db.Accounts.AnyAsync(a => a.Email == normalizedEmail, ct))
			return BadRequest("Tuto adresu nelze použít.");

		var requestedAccountType = request.AccountType ?? AccountType.Student;
		if(!CanManageRole(user.Value.Role, requestedAccountType))
			return Forbid();

		var enrollmentResult = await ResolveEnrollmentEntitiesAsync(request.Enrollment, ct);
		if(enrollmentResult.Error != null) return BadRequest(enrollmentResult.Error);

		if(!TryNormalizeUrlOptional(request.AvatarUrl, out var avatarUrl, out var avatarError))
			return BadRequest(avatarError);
		if(!TryNormalizeUrlOptional(request.BannerUrl, out var bannerUrl, out var bannerError))
			return BadRequest(bannerError);

		var password = string.IsNullOrWhiteSpace(request.Password) ? GenerateRandomPassword() : request.Password;
		var account = new Account {
			Id = Guid.Empty,
			FirstName = request.FirstName.Trim(),
			LastName = request.LastName.Trim(),
			Email = normalizedEmail,
			PasswordHash = AuthService.HashPassword(password),
			Gender = request.Gender,
			AccountType = requestedAccountType,
			AvatarUrl = avatarUrl,
			BannerUrl = bannerUrl,
			EnableReservations = request.EnableReservations ?? false,
			CommunicationStyle = request.CommunicationStyle ?? (requestedAccountType >= AccountType.Teacher ? CommunicationStyle.Formal : CommunicationStyle.Informal),
		};
		if(enrollmentResult.School != null) {
			account.Enrollment = new Enrollment {
				AccountId = account.Id,
				SchoolId = enrollmentResult.School.Id,
				Class = enrollmentResult.Class,
			};
		}

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
				created.Gender,
				tokenLifetime: RegistrationLoginLinkTokenLifetime
			);
		}

		await dbLogger.LogInfoAsync(
			$"{UserNoun(created)} {FormatAccount(created)} {PastVerb(created, "byl vytvořen", "byla vytvořena")} uživatelem ({user.Value.Id}).",
			"user-create",
			user.Value.Id,
			created.Id.ToString(),
			ct
		);

		return Ok(new AccountMutationResponse(created.ToDto(), emailSent));
	}

	[HttpPut("{id:guid}")]
	[Authorize(Policy = AuthPolicies.TeacherOrg)]
	public async Task<IActionResult> UpdateAccount(Guid id, [FromBody] AccountMutationRequest request, CancellationToken ct = default) {
		var user = auth.GetCurrentUser();
		if(user == null) return new UnauthorizedResult();
		if(!HasRoleAtLeast(user.Value.Role, AccountType.TeacherOrg))
			return Forbid();

		var account = await db.AccountsEf().FirstOrDefaultAsync(a => a.Id == id, ct);
		if(account == null) return NotFound();
		if(!CanManageAccount(user.Value.Role, account))
			return Forbid();

		var previousEnableReservations = account.EnableReservations;
		var requestedAccountType = request.AccountType ?? account.AccountType;
		if(!CanManageRole(user.Value.Role, requestedAccountType))
			return Forbid();

		if(!string.IsNullOrWhiteSpace(request.FirstName)) account.FirstName = request.FirstName.Trim();
		if(!string.IsNullOrWhiteSpace(request.LastName)) account.LastName = request.LastName.Trim();
		var previousEmail = account.Email;
		if (!string.IsNullOrWhiteSpace(request.Email)) {
			if (!AccountEmail.TryNormalize(request.Email, out var normalizedEmail)
				|| await db.Accounts.AnyAsync(a => a.Id != id && a.Email == normalizedEmail, ct))
				return BadRequest("Tuto adresu nelze použít.");
			account.Email = normalizedEmail;
		}

		if(!TryNormalizeUrlOptional(request.AvatarUrl, out var avatarUrl, out var avatarError))
			return BadRequest(avatarError);
		if(!TryNormalizeUrlOptional(request.BannerUrl, out var bannerUrl, out var bannerError))
			return BadRequest(bannerError);

		account.Gender = request.Gender;
		account.AccountType = requestedAccountType;
		if (account.AvatarSyncPlatform == null) account.AvatarUrl = avatarUrl;
		account.BannerUrl = bannerUrl;
		account.EnableReservations = request.EnableReservations ?? account.EnableReservations;
		account.CommunicationStyle = request.CommunicationStyle ?? account.CommunicationStyle;

		var enrollmentResult = await ResolveEnrollmentEntitiesAsync(request.Enrollment, ct);
		if(enrollmentResult.Error != null) return BadRequest(enrollmentResult.Error);
		if(enrollmentResult.School == null) {
			if(account.Enrollment != null) db.Enrollments.Remove(account.Enrollment);
			account.Enrollment = null;
		} else if(account.Enrollment == null) {
			account.Enrollment = new Enrollment {
				AccountId = account.Id,
				SchoolId = enrollmentResult.School.Id,
				Class = enrollmentResult.Class,
			};
		} else {
			account.Enrollment.SchoolId = enrollmentResult.School.Id;
			account.Enrollment.Class = enrollmentResult.Class;
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
		if(passwordForEmail != null || previousEmail != account.Email) await auth.RevokeAllSessionsAsync(account.Id, ct);
		reservationCache.InvalidateReservations();

		var updated = await db.AccountsEf().AsNoTracking().FirstAsync(a => a.Id == account.Id, ct);
		if (previousEmail != updated.Email) await dbLogger.LogInfoAsync(
			$"Admin zmenil email: {previousEmail} -> {updated.Email}.", "email-change-admin", user.Value.Id, updated.Id.ToString(), ct);
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

		await dbLogger.LogInfoAsync(
			$"{UserNoun(updated)} {FormatAccount(updated)} {PastVerb(updated, "byl upraven", "byla upravena")} uživatelem ({user.Value.Id}).",
			"user-edit",
			user.Value.Id,
			updated.Id.ToString(),
			ct
		);

		if(request.EnableReservations.HasValue && previousEnableReservations != updated.EnableReservations) {
			var stateMessage = updated.EnableReservations
				? $"Uživatel ({user.Value.Id}) změnil stav {ParticipantGenitive(updated)} {FormatAccount(updated)} na možnost rezervace."
				: $"Uživatel ({user.Value.Id}) změnil stav {ParticipantGenitive(updated)} {FormatAccount(updated)} na zákaz rezervace.";

			await dbLogger.LogInfoAsync(stateMessage, "user-edit", user.Value.Id, updated.Id.ToString(), ct);
		}

		return Ok(new AccountMutationResponse(updated.ToDto(), emailSent));
	}

	[HttpDelete("{id:guid}")]
	[Authorize(Policy = AuthPolicies.TeacherOrg)]
	public async Task<IActionResult> DeleteAccount(Guid id, CancellationToken ct = default) {
		var user = auth.GetCurrentUser();
		if(user == null) return new UnauthorizedResult();
		if(!HasRoleAtLeast(user.Value.Role, AccountType.TeacherOrg))
			return Forbid();

		var account = await db.Accounts.FirstOrDefaultAsync(a => a.Id == id, ct);
		if(account == null) return NotFound();
		if(!CanManageAccount(user.Value.Role, account))
			return Forbid();

		db.Accounts.Remove(account);
		await db.SaveChangesAsync(ct);
		reservationCache.InvalidateReservations();

		await dbLogger.LogInfoAsync(
			$"{UserNoun(account)} {FormatAccount(account)} {PastVerb(account, "byl smazán", "byla smazána")} uživatelem ({user.Value.Id}).",
			"user-delete",
			user.Value.Id,
			account.Id.ToString(),
			ct
		);

		return NoContent();
	}

	[HttpPost("{id:guid}/reset-password")]
	[Authorize(Policy = AuthPolicies.TeacherOrg)]
	public async Task<IActionResult> ResetPassword(Guid id, CancellationToken ct = default) {
		var user = auth.GetCurrentUser();
		if(user == null) return new UnauthorizedResult();
		if(!HasRoleAtLeast(user.Value.Role, AccountType.TeacherOrg))
			return Forbid();

		var account = await db.Accounts.FirstOrDefaultAsync(a => a.Id == id, ct);
		if(account == null) return NotFound();
		if(!CanManageAccount(user.Value.Role, account))
			return Forbid();

		var password = GenerateRandomPassword();
		account.PasswordHash = AuthService.HashPassword(password);
		await db.SaveChangesAsync(ct);
		await auth.RevokeAllSessionsAsync(account.Id, ct);
		reservationCache.InvalidateReservations();

		await dbLogger.LogInfoAsync(
			$"{UserNoun(account)} {FormatAccount(account)} {PastVerb(account, "měl", "měla")} resetované heslo uživatelem ({user.Value.Id}).",
			"user-reset-password",
			user.Value.Id,
			account.Id.ToString(),
			ct
		);

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
	[Authorize(Policy = AuthPolicies.Admin)]
	public async Task<IActionResult> Impersonate(Guid id, CancellationToken ct = default) {
		var user = auth.GetCurrentUser();
		if(user == null) return new UnauthorizedResult();
		if(!HasRoleAtLeast(user.Value.Role, AccountType.Admin))
			return Forbid();

		var account = await db.Accounts.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id, ct);
		if(account == null) return NotFound();
		if(!CanManageAccount(user.Value.Role, account))
			return Forbid();

		var signedInAccount = await auth.SignInAsAsync(account.Id, false, ct);
		if(signedInAccount == null) return NotFound();

		await dbLogger.LogWarnAsync(
			$"Administrátor ({user.Value.Id}) převzal identitu uživatele {FormatAccount(account)} ({account.Id}).",
			"user-impersonate",
			user.Value.Id,
			account.Id.ToString(),
			ct
		);

		return Ok(signedInAccount.ToDto());
	}

	[HttpPut("me")]
	[Authorize]
	public async Task<IActionResult> UpdateMyAccount([FromBody] MyAccountMutationRequest request, CancellationToken ct = default) {
		var accountId = auth.GetCurrentAccountId();
		if(accountId == null) return new UnauthorizedResult();

		var account = await db.AccountsEf().FirstOrDefaultAsync(a => a.Id == accountId.Value, ct);
		if(account == null) return NotFound();

		if(!TryNormalizeUrlOptional(request.AvatarUrl, out var avatarUrl, out var avatarError))
			return BadRequest(avatarError);
		if(!TryNormalizeUrlOptional(request.BannerUrl, out var bannerUrl, out var bannerError))
			return BadRequest(bannerError);

		account.Gender = request.Gender;
		account.CommunicationStyle = request.CommunicationStyle ?? account.CommunicationStyle;
		if (account.AvatarSyncPlatform == null) account.AvatarUrl = avatarUrl;
		account.BannerUrl = bannerUrl;

		await db.SaveChangesAsync(ct);
		reservationCache.InvalidateReservations();

		var updated = await db.AccountsEf().AsNoTracking().FirstAsync(a => a.Id == account.Id, ct);
		return Ok(updated.ToDto());
	}

	[HttpPut("avatar-sync-platform")]
	[Authorize]
	public async Task<IActionResult> SetAvatarSyncPlatform([FromBody] AvatarSyncPlatformRequest request, CancellationToken ct = default) {
		var accountId = auth.GetCurrentAccountId();
		if (accountId == null) return new UnauthorizedResult();

		var updated = await oauth.SetAvatarSyncPlatformAsync(accountId.Value, request.Platform, ct);
		return updated == null ? NotFound() : Ok(updated.ToDto());
	}

	[HttpPost("me/password")]
	[Authorize]
	[EnableRateLimiting("auth-change-password")]
	public async Task<IActionResult> ChangeMyPassword([FromBody] ChangeMyPasswordRequest request, CancellationToken ct = default) {
		var accountId = auth.GetCurrentAccountId();
		if(accountId == null) return new UnauthorizedResult();

		var lockoutKey = $"password-change:failed:{accountId.Value}";
		if (cache.TryGetValue<int>(lockoutKey, out var failedAttempts) && failedAttempts >= 5) {
			return StatusCode(StatusCodes.Status429TooManyRequests, "Příliš mnoho neúspěšných pokusů o změnu hesla. Zkuste to prosím za hodinu.");
		}

		if(string.IsNullOrWhiteSpace(request.OldPassword) || string.IsNullOrWhiteSpace(request.NewPassword))
			return BadRequest("Vyplň staré i nové heslo.");

		var account = await db.Accounts.FirstOrDefaultAsync(a => a.Id == accountId.Value, ct);
		if(account == null) return NotFound();
		if(!AuthService.VerifyPassword(request.OldPassword, account.PasswordHash)) {
			var currentFailures = (cache.TryGetValue<int>(lockoutKey, out var f) ? f : 0) + 1;
			cache.Set(lockoutKey, currentFailures, TimeSpan.FromHours(1));
			return BadRequest("Nesprávné staré heslo.");
		}
		cache.Remove(lockoutKey);

		if(AuthService.VerifyPassword(request.NewPassword, account.PasswordHash))
			return BadRequest("Nové heslo nesmí být stejné jako staré heslo.");

		account.PasswordHash = AuthService.HashPassword(request.NewPassword);
		await db.SaveChangesAsync(ct);
		await auth.RevokeAllSessionsAsync(account.Id, ct);

		await dbLogger.LogInfoAsync(
			$"{UserNoun(account)} {FormatAccount(account)} si {PastVerb(account, "změnil", "změnila")} heslo.",
			"user-change-own-password",
			account.Id,
			account.Id.ToString(),
			ct
		);

		await auth.LogoutAsync(ct);

		return NoContent();
	}

	[HttpPost("forgot-password")]
	[EnableRateLimiting("auth-forgot-password")]
	public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken ct = default) {
		if(string.IsNullOrWhiteSpace(request.Email))
			return BadRequest("Missing email.");

		var normalizedEmail = AccountEmail.TryNormalize(request.Email, out var normalized)
			? normalized
			: request.Email.Trim().ToLowerInvariant();
		var account = await db.Accounts.FirstOrDefaultAsync(a => a.Email == normalizedEmail, ct);
		if(account == null) return Ok(new PasswordResetResponse(true));

		var token = await CreateEmailTokenAsync(account, AccountEmailTokenPurpose.PasswordReset, PasswordResetTokenLifetime, ct);
		if (token == null)
			return Ok(new PasswordResetResponse(true));
		var resetLink = BuildAbsoluteUrl($"/app/reset-password#token={Uri.EscapeDataString(token)}");

		var emailSent = await SendPasswordResetLinkEmailAsync(
			account.Email,
			"EDUCHEM LAN Party - reset hesla",
			"/Views/Emails/UserForgotPassword.cshtml",
			resetLink,
			account.FirstName,
			account.LastName,
			account.Gender,
			account.CommunicationStyle
		);

		if(emailSent) {
			await dbLogger.LogInfoAsync(
				$"{UserNoun(account)} {FormatAccount(account)} si {PastVerb(account, "vyžádal", "vyžádala")} reset hesla; resetovací email byl odeslán.",
				"user-password-reset-request",
				null,
				account.Id.ToString(),
				ct
			);
		} else {
			await dbLogger.LogWarnAsync(
				$"{UserNoun(account)} {FormatAccount(account)} si {PastVerb(account, "vyžádal", "vyžádala")} reset hesla, ale resetovací email se nepodařilo odeslat.",
				"user-password-reset-email-failed",
				null,
				account.Id.ToString(),
				ct
			);
		}

		return Ok(new PasswordResetResponse(true));
	}

	[HttpPost("reset-password")]
	public async Task<IActionResult> ConfirmPasswordReset([FromBody] ConfirmPasswordResetRequest request, CancellationToken ct = default) {
		if(string.IsNullOrWhiteSpace(request.Token) || string.IsNullOrWhiteSpace(request.NewPassword))
			return BadRequest("Missing reset data.");

		var tokenHash = OneTimeToken.Hash(request.Token);
		if(tokenHash == null) return BadRequest("Invalid reset token.");
		var resetToken = await db.AccountEmailTokens.AsNoTracking()
			.FirstOrDefaultAsync(item => item.TokenHash == tokenHash && item.Purpose == AccountEmailTokenPurpose.PasswordReset
				&& item.ExpiresAtUtc > DateTime.UtcNow, ct);
		if(resetToken == null) return BadRequest("Invalid reset token.");

		await using var transaction = await db.Database.BeginTransactionAsync(ct);
		var account = await db.GetAccountForUpdateAsync(resetToken.AccountId, ct);
		if(account == null) return BadRequest("Invalid reset token.");
		var consumed = await db.AccountEmailTokens.Where(token => token.Id == resetToken.Id && token.TokenHash == tokenHash
			&& token.Purpose == AccountEmailTokenPurpose.PasswordReset && token.AccountId == account.Id
			&& token.ExpiresAtUtc > DateTime.UtcNow).ExecuteDeleteAsync(ct);
		if(consumed != 1)
			return BadRequest("Reset token expired.");

		account.PasswordHash = AuthService.HashPassword(request.NewPassword);
		await db.SaveChangesAsync(ct);
		await transaction.CommitAsync(ct);
		await auth.RevokeAllSessionsAsync(account.Id, ct);

		await dbLogger.LogInfoAsync(
			$"{UserNoun(account)} {FormatAccount(account)} {PastVerb(account, "dokončil", "dokončila")} reset hesla přes resetovací odkaz.",
			"user-password-reset-confirm",
			null,
			account.Id.ToString(),
			ct
		);

		return NoContent();
	}

	[HttpGet("login-link")]
	[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
	public IActionResult LoginLinkRedirect([FromQuery] string? token) {
		Response.Headers["Referrer-Policy"] = "no-referrer";
		return Redirect(BuildAbsoluteUrl(string.IsNullOrWhiteSpace(token)
			? "/app/login-link"
			: $"/app/login-link#token={Uri.EscapeDataString(token)}"));
	}

	[HttpPost("login-link/preview")]
	[ValidateAntiForgeryToken]
	[EnableRateLimiting("auth-login")]
	[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
	public async Task<IActionResult> PreviewLoginLink([FromBody] ConfirmLoginLinkRequest request, CancellationToken ct = default) {
		var tokenHash = OneTimeToken.Hash(request.Token);
		if(tokenHash == null) return BadRequest("Invalid login token.");
		var account = await db.AccountEmailTokens.AsNoTracking()
			.Where(item => item.TokenHash == tokenHash && item.Purpose == AccountEmailTokenPurpose.Login && item.ExpiresAtUtc > DateTime.UtcNow)
			.Select(item => new { item.Account.Email })
			.FirstOrDefaultAsync(ct);
		return account == null ? BadRequest("Invalid login token.") : Ok(account);
	}

	[HttpPost("login-link")]
	[ValidateAntiForgeryToken]
	[EnableRateLimiting("auth-login")]
	public async Task<IActionResult> LoginLink([FromBody] ConfirmLoginLinkRequest request, CancellationToken ct = default) {
		var tokenHash = OneTimeToken.Hash(request.Token);
		if(tokenHash == null) return BadRequest("Invalid login token.");
		var loginToken = await db.AccountEmailTokens.AsNoTracking()
			.FirstOrDefaultAsync(item => item.TokenHash == tokenHash && item.Purpose == AccountEmailTokenPurpose.Login
				&& item.ExpiresAtUtc > DateTime.UtcNow, ct);
		if(loginToken == null) return BadRequest("Invalid login token.");
		await using var transaction = await db.Database.BeginTransactionAsync(ct);
		var account = await db.GetAccountForUpdateAsync(loginToken.AccountId, ct, tracking: false);
		if(account == null) return BadRequest("Invalid login token.");
		// po zamknuti uctu znovu overime platnost; mezitim se mohl token pouzit nebo zneplatnit
		var consumed = await db.AccountEmailTokens.Where(item => item.Id == loginToken.Id && item.TokenHash == tokenHash
			&& item.Purpose == AccountEmailTokenPurpose.Login
			&& item.AccountId == account.Id && item.ExpiresAtUtc > DateTime.UtcNow).ExecuteDeleteAsync(ct);
		if (consumed != 1) return BadRequest("Invalid login token.");

		var acc = await auth.SignInAsAsync(account.Id, false, ct);
		if(acc == null) return BadRequest("Invalid login token.");
		await transaction.CommitAsync(ct);

		return NoContent();
	}

	public sealed record AccountMutationRequest(
		string? FirstName,
		string? LastName,
		string? Email,
		EnrollmentMutationRequest? Enrollment,
		Gender? Gender,
		AccountType? AccountType,
		string? AvatarUrl,
		string? BannerUrl,
		bool? EnableReservations,
		bool? SendLoginCredentialsEmail,
		string? Password,
		CommunicationStyle? CommunicationStyle
	);
	public sealed record EnrollmentMutationRequest(ushort? SchoolId, string? Class);

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
	public sealed record DashboardClassStat(SchoolDto School, string Class, int Count);
	public sealed record MyAccountMutationRequest(Gender? Gender, CommunicationStyle? CommunicationStyle, string? AvatarUrl, string? BannerUrl);
	public sealed record AvatarSyncPlatformRequest(OAuthProvider? Platform);
	public sealed record ChangeMyPasswordRequest(string OldPassword, string NewPassword);
	public sealed record ForgotPasswordRequest(string Email);
	public sealed record ConfirmPasswordResetRequest(string Token, string NewPassword);
	public sealed record ConfirmLoginLinkRequest(string Token);

	private static IOrderedQueryable<Account> OrderAccounts(IQueryable<Account> query, string? sort, string? direction) {
		var descending = string.Equals(direction, "desc", StringComparison.OrdinalIgnoreCase);
		var sortKey = sort?.Trim().ToLowerInvariant();

		return (sortKey, descending) switch {
			("email", false) => query.OrderBy(account => account.Email).ThenBy(account => account.Id),
			("email", true) => query.OrderByDescending(account => account.Email).ThenByDescending(account => account.Id),
			("gender", false) => query.OrderBy(account => account.Gender == Gender.Female ? "Žena" : account.Gender == Gender.Male ? "Muž" : account.Gender == Gender.Other ? "Ostatní" : "Neznámé").ThenBy(account => account.Id),
			("gender", true) => query.OrderByDescending(account => account.Gender == Gender.Female ? "Žena" : account.Gender == Gender.Male ? "Muž" : account.Gender == Gender.Other ? "Ostatní" : "Neznámé").ThenByDescending(account => account.Id),
			("school", false) => query.OrderBy(account => account.Enrollment == null ? "" : account.Enrollment.School.DisplayName).ThenBy(account => account.Id),
			("school", true) => query.OrderByDescending(account => account.Enrollment == null ? "" : account.Enrollment.School.DisplayName).ThenByDescending(account => account.Id),
			("class", false) => query.OrderBy(account => account.Enrollment == null ? "" : account.Enrollment.Class ?? "").ThenBy(account => account.Id),
			("class", true) => query.OrderByDescending(account => account.Enrollment == null ? "" : account.Enrollment.Class ?? "").ThenByDescending(account => account.Id),
			("accounttype", false) => query.OrderBy(account =>
				account.AccountType == AccountType.Admin ? (account.Gender == Gender.Female ? "Administrátorka" : "Administrátor")
				: account.AccountType == AccountType.SuperAdmin ? (account.Gender == Gender.Female ? "Administrátorka (SU)" : "Administrátor (SU)")
				: account.AccountType == AccountType.Teacher ? (account.Gender == Gender.Female ? "Učitelka" : "Učitel")
				: account.AccountType == AccountType.TeacherOrg ? (account.Gender == Gender.Female ? "Učitelka (ORG)" : "Učitel (ORG)")
				: account.Gender == Gender.Female ? "Studentka" : "Student"
			).ThenBy(account => account.Id),
			("accounttype", true) => query.OrderByDescending(account =>
				account.AccountType == AccountType.Admin ? (account.Gender == Gender.Female ? "Administrátorka" : "Administrátor")
				: account.AccountType == AccountType.SuperAdmin ? (account.Gender == Gender.Female ? "Administrátorka (SU)" : "Administrátor (SU)")
				: account.AccountType == AccountType.Teacher ? (account.Gender == Gender.Female ? "Učitelka" : "Učitel")
				: account.AccountType == AccountType.TeacherOrg ? (account.Gender == Gender.Female ? "Učitelka (ORG)" : "Učitel (ORG)")
				: account.Gender == Gender.Female ? "Studentka" : "Student"
			).ThenByDescending(account => account.Id),
			("createdatutc", false) => query.OrderBy(account => account.CreatedAtUtc).ThenBy(account => account.Id),
			("createdatutc", true) => query.OrderByDescending(account => account.CreatedAtUtc).ThenByDescending(account => account.Id),
			("updatedatutc", false) => query.OrderBy(account => account.UpdatedAtUtc).ThenBy(account => account.Id),
			("updatedatutc", true) => query.OrderByDescending(account => account.UpdatedAtUtc).ThenByDescending(account => account.Id),
			("lastactiveutc", false) => query.OrderBy(account => account.LastActiveUtc).ThenBy(account => account.Id),
			("lastactiveutc", true) => query.OrderByDescending(account => account.LastActiveUtc).ThenByDescending(account => account.Id),
			(_, true) => query.OrderByDescending(account => account.FirstName).ThenByDescending(account => account.LastName).ThenByDescending(account => account.Id),
			_ => query.OrderBy(account => account.FirstName).ThenBy(account => account.LastName).ThenBy(account => account.Id),
		};
	}

	private async Task<AccountFilterOptionsDto> BuildAccountFilterOptionsAsync(CancellationToken ct) {
		var accountTypeCounts = await db.Accounts
			.AsNoTracking()
			.GroupBy(account => account.AccountType)
			.Select(group => new {Value = group.Key, Count = group.Count()})
			.ToListAsync(ct);
		var accountTypes = accountTypeCounts
			.OrderBy(option => option.Value)
			.Select(option => new ValueCountDto<AccountType>(option.Value, option.Count))
			.ToList();
		var genderCounts = await db.Accounts
			.AsNoTracking()
			.Where(account => account.Gender.HasValue)
			.GroupBy(account => account.Gender!.Value)
			.Select(group => new {Value = group.Key, Count = group.Count()})
			.ToListAsync(ct);
		var genders = genderCounts
			.OrderBy(option => option.Value)
			.Select(option => new ValueCountDto<Gender>(option.Value, option.Count))
			.ToList();
		var classCounts = await db.Enrollments
			.AsNoTracking()
			.Where(enrollment => enrollment.Class != null && enrollment.Class != "")
			.GroupBy(enrollment => enrollment.Class!)
			.Select(group => new {Value = group.Key, Count = group.Count()})
			.ToListAsync(ct);
		var classes = classCounts
			.Select(option => new ValueCountDto<string>(option.Value, option.Count))
			.ToList();
		var schoolCounts = await db.Enrollments
			.AsNoTracking()
			.GroupBy(enrollment => enrollment.SchoolId)
			.Select(group => new {SchoolId = group.Key, Count = group.Count()})
			.ToListAsync(ct);
		var schoolIds = schoolCounts.Select(option => option.SchoolId).ToArray();
		var schools = schoolIds.Length == 0
			? []
			: await db.Schools
				.AsNoTracking()
				.Where(school => schoolIds.Contains(school.Id))
				.OrderBy(school => school.DisplayName)
				.ToListAsync(ct);
		var schoolCountById = schoolCounts.ToDictionary(option => option.SchoolId, option => option.Count);

		return new AccountFilterOptionsDto(
			accountTypes,
			genders,
			classes,
			schools.Select(school => new AccountSchoolFilterOptionDto(school.ToDto(false), schoolCountById[school.Id])).ToList()
		);
	}

	private static string? NormalizeOptional(string? value) {
		return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
	}

	private static bool TryNormalizeUrlOptional(string? value, out string? normalizedUrl, out string? error) {
		normalizedUrl = null;
		error = null;
		if (string.IsNullOrWhiteSpace(value)) return true;
		var trimmed = value.Trim();
		if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri)
			|| (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)) {
			error = "URL adresa musí být platná absolutní adresa začínající http:// nebo https://.";
			return false;
		}
		normalizedUrl = uri.ToString();
		return true;
	}

	private async Task<EnrollmentEntitiesResult> ResolveEnrollmentEntitiesAsync(EnrollmentMutationRequest? request, CancellationToken ct) {
		var className = NormalizeOptional(request?.Class);
		var schoolId = request?.SchoolId;

		if(schoolId == null && className == null) return new EnrollmentEntitiesResult(null, null, null);
		if(schoolId == null) return new EnrollmentEntitiesResult(null, null, "School must be provided when class is set.");

		var school = await db.Schools.FirstOrDefaultAsync(item => item.Id == schoolId, ct);
		if(school == null) return new EnrollmentEntitiesResult(null, null, "Unknown school.");

		return new EnrollmentEntitiesResult(school, className, null);
	}

	private sealed record EnrollmentEntitiesResult(School? School, string? Class, string? Error);

	private static bool HasRoleAtLeast(AccountType actorRole, AccountType accountType) {
		return actorRole >= accountType;
	}

	private static bool CanManageRole(AccountType actorRole, AccountType targetAccountType) {
		return actorRole == AccountType.SuperAdmin || actorRole > targetAccountType;
	}

	private static bool CanManageAccount(AccountType actorRole, Account target) {
		return CanManageRole(actorRole, target.AccountType);
	}

	private static string GenerateRandomPassword(int length = 24) {
		const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@*";
		var passwordBuilder = new StringBuilder(length);
		for (var i = 0; i < length; i++) {
			var randomIndex = RandomNumberGenerator.GetInt32(chars.Length);
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
		Gender? gender,
		TimeSpan? tokenLifetime = null
	) {
		var webLink = await GetLoginLinkAsync(account, tokenLifetime ?? LoginLinkTokenLifetime);
		if (webLink == null) return false;
		var model = new EmailUserRegisterModel(password, webLink, email, firstName, lastName, gender, account.CommunicationStyle);
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
		Gender? gender,
		CommunicationStyle communicationStyle
	) {
		var model = new EmailPasswordResetLinkModel(resetLink, email, firstName, lastName, gender, communicationStyle);
		var fallbackBody = $"Reset link: {resetLink}";
		return await EmailService.SendHtmlEmailAsync(email, subject, viewPath, model, serviceProvider, fallbackBody);
	}

	private async Task<string?> GetLoginLinkAsync(Account account, TimeSpan tokenLifetime) {
		var token = await CreateEmailTokenAsync(account, AccountEmailTokenPurpose.Login, tokenLifetime, HttpContext.RequestAborted);
		return token == null ? null : BuildAbsoluteUrl($"/app/login-link#token={Uri.EscapeDataString(token)}");
	}

	private async Task<string?> CreateEmailTokenAsync(Account account, AccountEmailTokenPurpose purpose, TimeSpan lifetime, CancellationToken ct) {
		await using var transaction = await db.Database.BeginTransactionAsync(ct);
		var current = await db.GetAccountForUpdateAsync(account.Id, ct, tracking: false);
		if (current == null || current.Email != account.Email || current.PasswordHash != account.PasswordHash) return null;
		await db.AccountEmailTokens.Where(token => token.AccountId == account.Id && token.ExpiresAtUtc <= DateTime.UtcNow).ExecuteDeleteAsync(ct);
		var rawToken = OneTimeToken.Create();
		db.AccountEmailTokens.Add(new() {
			Id = Guid.NewGuid(), AccountId = account.Id, ExpiresAtUtc = DateTime.UtcNow.Add(lifetime),
			TokenHash = OneTimeToken.Hash(rawToken), Purpose = purpose,
		});
		await db.SaveChangesAsync(ct);
		await transaction.CommitAsync(ct);
		return rawToken;
	}

	private static string BuildAbsoluteUrl(string pathAndQuery) {
		if (!Program.ENV.TryGetValue("WEB_URL", out var webUrl) || !Uri.TryCreate(webUrl, UriKind.Absolute, out var uri)) {
			throw new InvalidOperationException("WEB_URL musi byt nastavene jako absolutni URL.");
		}
		if (uri.Scheme is not ("http" or "https") || !string.IsNullOrEmpty(uri.UserInfo) || !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment)) {
			throw new InvalidOperationException("WEB_URL musi obsahovat jen HTTP(S) originu.");
		}

		return $"{uri.GetLeftPart(UriPartial.Authority)}{pathAndQuery}";
	}

	private static string FormatAccount(Account account) {
		return $"{account.FirstName} {account.LastName} ({account.Email})";
	}

	private static string UserNoun(Account account) {
		return account.Gender == Gender.Female ? "Uživatelka" : "Uživatel";
	}

	private static string ParticipantGenitive(Account account) {
		return account.Gender == Gender.Female ? "účastnice" : "účastníka";
	}

	[HttpGet("sessions")]
	[Authorize]
	public async Task<IActionResult> GetMySessions(CancellationToken ct = default) {
		var user = auth.GetCurrentUser();
		if (user == null) return Unauthorized();

		var sessions = await auth.GetActiveSessionsAsync(user.Value.Id, user.Value.SessionId, ct);
		return Ok(sessions);
	}

	[HttpDelete("sessions/{id:guid}")]
	[Authorize]
	public async Task<IActionResult> RevokeSession(Guid id, CancellationToken ct = default) {
		var user = auth.GetCurrentUser();
		if (user == null) return Unauthorized();

		var success = await auth.RevokeSessionAsync(id, user.Value.Id, ct);
		if (!success) return NotFound("Relace nebyla nalezena.");

		await dbLogger.LogInfoAsync(
			$"Uživatel ({user.Value.Id}) odhlásil zařízení s relací {id}.",
			"session-revoke",
			user.Value.Id,
			id.ToString(),
			ct
		);

		return NoContent();
	}

	[HttpDelete("sessions/other")]
	[Authorize]
	public async Task<IActionResult> RevokeOtherSessions(CancellationToken ct = default) {
		var user = auth.GetCurrentUser();
		if (user == null) return Unauthorized();

		var count = await auth.RevokeOtherSessionsAsync(user.Value.SessionId, user.Value.Id, ct);

		await dbLogger.LogInfoAsync(
			$"Uživatel ({user.Value.Id}) odhlásil všechna ostatní zařízení ({count} relací).",
			"session-revoke-other",
			user.Value.Id,
			user.Value.SessionId.ToString(),
			ct
		);

		return Ok(new { RevokedCount = count });
	}

	private static string PastVerb(Account account, string masculine, string feminine) {
		return account.Gender == Gender.Female ? feminine : masculine;
	}
}
