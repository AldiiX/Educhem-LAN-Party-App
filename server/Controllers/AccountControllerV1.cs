using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
public class AccountControllerV1(IAuthService auth, AppDbContext db, IServiceProvider serviceProvider) : Controller {

	[HttpGet]
	public async Task<IActionResult> GetMyAccount(CancellationToken ct = default) {
		var acc = await auth.ReAuthAsync(ct);
		if(acc == null) return new UnauthorizedResult();

		return Ok(acc.ToDto());
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
				created.Email,
				"EDUCHEM LAN Party - přihlašovací údaje",
				"/Views/Emails/UserRegistered.cshtml",
				password
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

		var updated = await db.AccountsEf().AsNoTracking().FirstAsync(a => a.Id == account.Id, ct);
		var emailSent = false;
		if(request.SendLoginCredentialsEmail == true && passwordForEmail != null) {
			emailSent = await SendCredentialsEmailAsync(
				updated.Email,
				"EDUCHEM LAN Party - nové přihlašovací údaje",
				"/Views/Emails/UserResetPassword.cshtml",
				passwordForEmail
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

		var emailSent = await SendCredentialsEmailAsync(
			account.Email,
			"EDUCHEM LAN Party - nové heslo",
			"/Views/Emails/UserResetPassword.cshtml",
			password
		);

		return Ok(new PasswordResetResponse(emailSent));
	}

	[HttpPost("login")]
	public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct = default) {
		var acc = await auth.LoginAsync(request.Email, request.PasswordPlain, ct);
		if (acc == null) return Unauthorized();

		return new OkObjectResult(acc.ToDto());
	}

	[HttpGet("login-link")]
	public async Task<IActionResult> LoginLink([FromQuery] string email, [FromQuery] string password, CancellationToken ct = default) {
		var acc = await auth.LoginAsync(email, password, ct);
		if(acc == null) return Redirect("/app/login");

		return Redirect("/app");
	}

	[HttpPost("logout")]
	public async Task<IActionResult> Logout(CancellationToken ct = default) {
		await auth.LogoutAsync(ct);
		return NoContent();
	}












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

		for (int i = 0; i < length; i++) {
			var randomIndex = random.Next(chars.Length);
			passwordBuilder.Append(chars[randomIndex]);
		}

		return passwordBuilder.ToString();
	}

	private async Task<bool> SendCredentialsEmailAsync(string email, string subject, string viewPath, string password) {
		var webLink = GetWebLink(email, password);
		var model = new EmailUserRegisterModel(password, webLink, email);
		var fallbackBody = $"Email: {email}\nHeslo: {password}\n{webLink}";

		return await EmailService.SendHtmlEmailAsync(email, subject, viewPath, model, serviceProvider, fallbackBody);
	}

	private string GetWebLink(string email, string password) {
		var encodedEmail = Uri.EscapeDataString(email);
		var encodedPassword = Uri.EscapeDataString(password);

		if(Program.ENV.TryGetValue("WEB_URL", out var webUrl) && !string.IsNullOrWhiteSpace(webUrl)) {
			return $"{webUrl.TrimEnd('/')}/api/v1/account/login-link?email={encodedEmail}&password={encodedPassword}";
		}

		var request = HttpContext.Request;
		return $"{request.Scheme}://{request.Host}/api/v1/account/login-link?email={encodedEmail}&password={encodedPassword}";
	}

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
}
