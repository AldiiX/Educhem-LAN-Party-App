using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using server.Data;
using server.Data.Entities;
using server.Emails;
using server.Infrastructure;
using server.Models;
using server.Services;

namespace server.Controllers;

/// <summary>
/// stav zadosti o zmenu emailu, co se posila na frontend.
/// </summary>
/// <param name="Id">idcko zadosti v databazi</param>
/// <param name="OldEmail">puvodni email (v nahledu muze bejt zamaskovanej)</param>
/// <param name="NewEmail">novej pozadovanej email (v nahledu taky muze bejt zamaskovanej)</param>
/// <param name="ExpiresAtUtc">kdy tomu vyprsi platnost v utc (standardne za pul hodiny)</param>
/// <param name="OldConfirmed">jestli uzivatel odklepnul odkaz ze staryho emailu</param>
/// <param name="NewConfirmed">jestli uzivatel odklepnul odkaz z novyho emailu</param>
/// <param name="State">aktualni stav zadosti (pending, completed, cancelled, expired)</param>
/// <param name="ResendAtUtc">odkdy muze user kliknout na znovuposlani emailu</param>
/// <param name="CommunicationStyle">styl komunikace (formalni nebo tykani) kvuli sablonam</param>
public sealed record EmailChangeStatus(
	Guid Id,
	string OldEmail,
	string NewEmail,
	DateTime ExpiresAtUtc,
	bool OldConfirmed,
	bool NewConfirmed,
	string State,
	DateTime? ResendAtUtc,
	CommunicationStyle CommunicationStyle
);

/// <summary>
/// obecna odpoved kontroleru pro zmenu emailu.
/// </summary>
/// <param name="Request">detail zadosti nebo null kdyz zadna nebezi</param>
/// <param name="EmailsSent">jestli proslo odeslani vsech emailu</param>
/// <param name="TokenAction">co ten token vlastne udelal (old, new, cancel)</param>
public sealed record EmailChangeResponse(
	EmailChangeStatus? Request = null,
	bool EmailsSent = true,
	string? TokenAction = null
);

/// <summary>
/// stara se o kompletni dvoufazovou zmenu emailu.
/// flow:
/// 1. user zada novej email a soucasny heslo pres start.
/// 2. system overi heslo, overi dostupnost mailu a posle potvrzovaci link na starej i novej email (na starym je i link na okamzity zruseni kdyby to user nebyl).
/// 3. user musi potvrdit linky z obou schranek (kvuli bezpecnosti a preklepum).
/// 4. po potvrzeni obou se email prepise v db, poslou se notifikace na obe schranky a shodi to vsechny aktivni sessions (user se musi znova prihlasit).
/// 5. dokud to neni cely potvrzeny, jde to kdykoliv zrusit z webu nebo z mailu, pripadne to po 30 minutach samo expne.
/// </summary>
[ApiController]
[TypeFilter(typeof(AccountWriteExceptionFilter))]
[Route("api/v1/account/email-change")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[EnableRateLimiting("email-change")]
public sealed class EmailChangeControllerV1(
	AppDbContext db,
	IAuthService auth,
	IOAuthService oauth,
	IDbLoggerService audit,
	IServiceProvider serviceProvider
) : ControllerBase {
	private const string InvalidLink = "Odkaz je neplatný, již použitý nebo vypršel.";
	private const string UnavailableEmail = "Tuto adresu nelze použít.";
	private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(30);

	private static DateTime Now => DateTime.UtcNow;

	/// <summary>
	/// vrati stav rozpracovany nebo dokonceny zadosti prihlasenyho usera.
	/// </summary>
	/// <param name="ct">cancellation token</param>
	/// <returns>stav zadosti v request fieldu, pripadne null kdyz nic nebezi</returns>
	[HttpGet, Authorize]
	public async Task<IActionResult> Status(CancellationToken ct) {
		var id = auth.GetCurrentAccountId();
		if (id == null) return Unauthorized();

		var request = await db.EmailChangeRequests.AsNoTracking().Include(r => r.Account)
			.FirstOrDefaultAsync(r => r.AccountId == id.Value, ct);

		var status = request == null ? null : await ToStatusAsync(request, false, ct);
		return Ok(new EmailChangeResponse(status));
	}

	/// <summary>
	/// nastartuje zmenu emailu.
	/// zkontroluje heslo, overi ze novej email neni zabranej, vygeneruje jednorazovy tokeny a posle potvrzovaci maily na obe adresy.
	/// plati 30 minut a hlida rate limit (max 5 pokusu za hodinu, cooldown 60 sekund).
	/// </summary>
	/// <param name="request">novej email a soucasny heslo</param>
	/// <param name="ct">cancellation token</param>
	/// <returns>stav nove zadosti s priznakem jestli odletely maily</returns>
	[HttpPost, Authorize]
	public async Task<IActionResult> Start([FromBody] StartRequest request, CancellationToken ct) {
		var id = auth.GetCurrentAccountId();
		if (id == null) return Unauthorized();

		var origin = oauth.GetFrontendOrigin();
		if (origin == null) return StatusCode(503, new { message = "Odesílání potvrzení není dostupné." });

		await using var transaction = await db.Database.BeginTransactionAsync(ct);
		var account = await db.GetAccountForUpdateAsync(id.Value, ct);
		if (account == null) return StatusCode(401, new { message = "Účet není dostupný." });

		var limitResult = await CheckLimitAsync(id.Value, ct);
		if (limitResult != null) return limitResult;

		db.EmailChangeAttempts.Add(new() { AccountId = id.Value, CreatedAtUtc = Now });
		await db.SaveChangesAsync(ct);

		if (request.Password.Length > 512 || !AuthService.VerifyPassword(request.Password, account.PasswordHash)) {
			await transaction.CommitAsync(ct);
			return BadRequest(new { message = "Nesprávné současné heslo." });
		}

		if (!AccountEmail.TryNormalize(request.Email, out var normalized) || normalized == account.Email.Trim().ToLowerInvariant()
			|| await db.Accounts.AnyAsync(a => a.Email == normalized, ct)) {
			await transaction.CommitAsync(ct);
			return BadRequest(new { message = UnavailableEmail });
		}

		var previous = await db.EmailChangeRequests.FirstOrDefaultAsync(r => r.AccountId == id.Value, ct);
		if (previous != null) {
			if (State(previous) == "pending") await LogAsync(previous, "cancel", "Nahrazena zadost", id.Value, ct);
			db.EmailChangeRequests.Remove(previous);
			await db.SaveChangesAsync(ct);
		}

		var changeRequest = new EmailChangeRequest {
			AccountId = id.Value,
			Account = account,
			OldEmail = account.Email,
			NewEmail = normalized,
			CreatedAtUtc = Now,
			ExpiresAtUtc = Now.Add(Lifetime),
			CancelTokenHash = "",
		};

		var cancel = OneTimeToken.Create();
		changeRequest.CancelTokenHash = OneTimeToken.Hash(cancel)!;
		var messages = ConfirmationMessages(changeRequest, origin, cancel);
		db.EmailChangeRequests.Add(changeRequest);
		await LogAsync(changeRequest, "request", "Zadost o zmenu", id.Value, ct);
		await transaction.CommitAsync(ct);

		var sent = await SendEmailsAsync(messages, changeRequest, ct);
		var status = await ToStatusAsync(changeRequest, false, ct);
		return Ok(new EmailChangeResponse(status, EmailsSent: sent));
	}

	/// <summary>
	/// posle znova potvrzovaci maily pro bezici zadost na adresy, co jeste nejsou potvrzeny.
	/// hlida 60s cooldown mezi pokusama.
	/// </summary>
	/// <param name="ct">cancellation token</param>
	/// <returns>aktualni stav zadosti po preposlani mailu</returns>
	[HttpPost("resend"), Authorize]
	public async Task<IActionResult> Resend(CancellationToken ct) {
		var id = auth.GetCurrentAccountId();
		if (id == null) return Unauthorized();

		var origin = oauth.GetFrontendOrigin();
		if (origin == null) return StatusCode(503, new { message = "Odesílání potvrzení není dostupné." });

		await using var transaction = await db.Database.BeginTransactionAsync(ct);
		if (await db.GetAccountForUpdateAsync(id.Value, ct) == null) return BadRequest(new { message = InvalidLink });

		var changeRequest = await db.EmailChangeRequests.Include(r => r.Account).FirstOrDefaultAsync(r => r.AccountId == id.Value, ct);
		if (changeRequest == null || State(changeRequest) != "pending") return BadRequest(new { message = InvalidLink });

		var limitResult = await CheckLimitAsync(id.Value, ct);
		if (limitResult != null) return limitResult;

		db.EmailChangeAttempts.Add(new() { AccountId = id.Value, CreatedAtUtc = Now });
		string? cancel = null;
		if (changeRequest.OldConfirmedAtUtc == null) {
			cancel = OneTimeToken.Create();
			changeRequest.CancelTokenHash = OneTimeToken.Hash(cancel)!;
		}

		var messages = ConfirmationMessages(changeRequest, origin, cancel);
		await LogAsync(changeRequest, "resend", "Znovu odeslano potvrzeni", id.Value, ct);
		await transaction.CommitAsync(ct);

		var sent = await SendEmailsAsync(messages, changeRequest, ct);
		var status = await ToStatusAsync(changeRequest, false, ct);
		return Ok(new EmailChangeResponse(status, EmailsSent: sent));
	}

	/// <summary>
	/// rucne zrusi rozpracovanou zadost o zmenu emailu z profilu usera.
	/// </summary>
	/// <param name="ct">cancellation token</param>
	/// <returns>stav zruseny zadosti</returns>
	[HttpPost("cancel"), Authorize]
	public async Task<IActionResult> Cancel(CancellationToken ct) {
		var id = auth.GetCurrentAccountId();
		if (id == null) return Unauthorized();

		await using var transaction = await db.Database.BeginTransactionAsync(ct);
		if (await db.GetAccountForUpdateAsync(id.Value, ct) == null) return BadRequest(new { message = InvalidLink });

		var changeRequest = await db.EmailChangeRequests.Include(r => r.Account).FirstOrDefaultAsync(r => r.AccountId == id.Value, ct);
		if (changeRequest == null || State(changeRequest) != "pending") return BadRequest(new { message = InvalidLink });

		changeRequest.CancelledAtUtc = Now;
		await LogAsync(changeRequest, "cancel", "Zrusena zadost", id.Value, ct);
		await db.SaveChangesAsync(ct);
		await transaction.CommitAsync(ct);

		var status = await ToStatusAsync(changeRequest, false, ct);
		return Ok(new EmailChangeResponse(status));
	}

	/// <summary>
	/// verejnej nahled tokenu z linku v mailu.
	/// token se nezkonsumuje, jenom to overi platnost a vrati zamaskovany emaily a typ akce pro frontend modal.
	/// </summary>
	/// <param name="request">token z url fragmentu</param>
	/// <param name="ct">cancellation token</param>
	/// <returns>info o zadosti a akce co se ma provest</returns>
	[HttpPost("preview"), AllowAnonymous]
	public async Task<IActionResult> Preview([FromBody] TokenRequest request, CancellationToken ct) {
		var match = await FindTokenAsync(request.Token, ct);
		if (match == null) return BadRequest(new { message = InvalidLink });

		var status = await ToStatusAsync(match.Request, true, ct);
		return Ok(new EmailChangeResponse(status, TokenAction: match.Action));
	}

	/// <summary>
	/// odklepne a zkonzumuje token z mailu.
	/// kdyz jde o cancel, zrusi to zadost.
	/// kdyz jde o old/new, oznaci danou stranu za potvrzenou.
	/// jakmile jsou potvrzeny obe adresy, prepise email v db na novej, posle finalni maily a shodi vsechny aktivni sessions usera.
	/// </summary>
	/// <param name="request">token z linku</param>
	/// <param name="ct">cancellation token</param>
	/// <returns>vyslednej stav zadosti</returns>
	[HttpPost("confirm"), AllowAnonymous]
	public async Task<IActionResult> Confirm([FromBody] TokenRequest request, CancellationToken ct) {
		var initial = await FindTokenAsync(request.Token, ct);
		if (initial == null) return BadRequest(new { message = InvalidLink });

		await using var transaction = await db.Database.BeginTransactionAsync(ct);
		db.ChangeTracker.Clear();
		if (await db.GetAccountForUpdateAsync(initial.Request.AccountId, ct) == null)
			return BadRequest(new { message = InvalidLink });

		var match = await FindTokenAsync(request.Token, ct);
		if (match == null) return BadRequest(new { message = InvalidLink });

		var changeRequest = match.Request;
		var kind = match.Action;
		if (kind == "cancel") {
			changeRequest.CancelledAtUtc = Now;
			await LogAsync(changeRequest, "cancel", "Zruseno odkazem", null, ct);
		} else {
			if (kind == "old") {
				changeRequest.OldConfirmedAtUtc = Now;
				changeRequest.OldTokenHash = null;
			} else {
				changeRequest.NewConfirmedAtUtc = Now;
				changeRequest.NewTokenHash = null;
			}

			await LogAsync(changeRequest, "confirm", kind == "old" ? "Potvrzen puvodni email" : "Potvrzen novy email", null, ct);

			if (changeRequest.OldConfirmedAtUtc != null && changeRequest.NewConfirmedAtUtc != null) {
				if (await db.Accounts.AnyAsync(a => a.Id != changeRequest.AccountId && a.Email == changeRequest.NewEmail, ct)) {
					changeRequest.CancelledAtUtc = Now;
					await LogAsync(changeRequest, "cancel", "Cilovy email uz nejde pouzit", null, ct);
					await db.SaveChangesAsync(ct);
					await transaction.CommitAsync(ct);
					return Conflict(new { message = UnavailableEmail });
				}

				changeRequest.CompletedAtUtc = Now;
				changeRequest.Account.Email = changeRequest.NewEmail;
				await LogAsync(changeRequest, "complete", "Dokoncena zmena", null, ct);
			}
		}

		await db.SaveChangesAsync(ct);
		await transaction.CommitAsync(ct);

		var sent = true;
		if (changeRequest.CompletedAtUtc != null) {
			sent = await SendEmailsAsync([
				new(changeRequest.OldEmail, changeRequest.Account, changeRequest.OldEmail, changeRequest.NewEmail, changeRequest.ExpiresAtUtc, Completed: true),
				new(changeRequest.NewEmail, changeRequest.Account, changeRequest.OldEmail, changeRequest.NewEmail, changeRequest.ExpiresAtUtc, Completed: true),
			], changeRequest, ct);

			await auth.RevokeAllSessionsAsync(changeRequest.AccountId, ct);
			if (auth.GetCurrentAccountId() == changeRequest.AccountId) {
				await auth.LogoutAsync(ct);
			}
		}

		var status = await ToStatusAsync(changeRequest, true, ct);
		return Ok(new EmailChangeResponse(status, EmailsSent: sent, TokenAction: kind));
	}

	private async Task<IActionResult?> CheckLimitAsync(Guid id, CancellationToken ct) {
		var since = Now.AddHours(-1);
		var attempts = await db.EmailChangeAttempts.Where(a => a.AccountId == id && a.CreatedAtUtc > since)
			.OrderByDescending(a => a.CreatedAtUtc).Select(a => a.CreatedAtUtc).ToListAsync(ct);

		if (attempts.Count >= 5) {
			return StatusCode(StatusCodes.Status429TooManyRequests, new { message = "Nejvýše 5 žádostí nebo odeslání za hodinu. Zkuste to později." });
		}

		if (attempts.Count > 0 && attempts[0].AddSeconds(60) > Now) {
			return StatusCode(StatusCodes.Status429TooManyRequests, new { message = "Mezi žádostmi a odesláním je potřeba počkat 60 sekund." });
		}

		await db.EmailChangeAttempts.Where(a => a.AccountId == id && a.CreatedAtUtc <= since).ExecuteDeleteAsync(ct);
		return null;
	}

	private sealed record EmailChangeTokenMatch(EmailChangeRequest Request, string Action);

	private async Task<EmailChangeTokenMatch?> FindTokenAsync(string token, CancellationToken ct) {
		var hash = OneTimeToken.Hash(token);
		if (hash == null) return null;

		var request = await db.EmailChangeRequests.Include(r => r.Account)
			.FirstOrDefaultAsync(r => r.OldTokenHash == hash || r.NewTokenHash == hash || r.CancelTokenHash == hash, ct);

		if (request == null || State(request) != "pending") return null;
		var action = request.OldTokenHash == hash ? "old" : request.NewTokenHash == hash ? "new" : "cancel";
		return new(request, action);
	}

	private static string State(EmailChangeRequest r) => r.CompletedAtUtc != null ? "completed"
		: r.CancelledAtUtc != null || r.OldEmail != r.Account.Email ? "cancelled"
		: r.ExpiresAtUtc <= Now ? "expired" : "pending";

	private async Task<EmailChangeStatus> ToStatusAsync(EmailChangeRequest r, bool masked, CancellationToken ct) {
		DateTime? resendAt = null;
		if (!masked) {
			var since = Now.AddHours(-1);
			var attempts = await db.EmailChangeAttempts.Where(a => a.AccountId == r.AccountId && a.CreatedAtUtc > since)
				.OrderBy(a => a.CreatedAtUtc).Select(a => a.CreatedAtUtc).ToListAsync(ct);
			if (attempts.Count > 0) {
				resendAt = attempts.Count >= 5 ? attempts[0].AddHours(1) : attempts[^1].AddSeconds(60);
			}
		}

		return new(
			r.Id,
			masked ? AccountEmail.Mask(r.OldEmail) : r.OldEmail,
			masked ? AccountEmail.Mask(r.NewEmail) : r.NewEmail,
			r.ExpiresAtUtc,
			r.OldConfirmedAtUtc != null,
			r.NewConfirmedAtUtc != null,
			State(r),
			resendAt,
			r.Account.CommunicationStyle
		);
	}

	private static string Link(string origin, string token) => $"{origin}/app/change-email#token={Uri.EscapeDataString(token)}";

	private static List<EmailChangeMessage> ConfirmationMessages(EmailChangeRequest r, string origin, string? cancel) {
		var messages = new List<EmailChangeMessage>();
		if (r.OldConfirmedAtUtc == null) {
			var token = OneTimeToken.Create();
			r.OldTokenHash = OneTimeToken.Hash(token);
			messages.Add(new(r.OldEmail, r.Account, r.OldEmail, r.NewEmail, r.ExpiresAtUtc, Link(origin, token), cancel == null ? null : Link(origin, cancel)));
		}
		if (r.NewConfirmedAtUtc == null) {
			var token = OneTimeToken.Create();
			r.NewTokenHash = OneTimeToken.Hash(token);
			messages.Add(new(r.NewEmail, r.Account, r.OldEmail, r.NewEmail, r.ExpiresAtUtc, Link(origin, token)));
		}
		return messages;
	}

	private Task<bool> LogAsync(EmailChangeRequest r, string action, string message, Guid? actor, CancellationToken ct) =>
		audit.LogInfoAsync($"{message}: {r.OldEmail} -> {r.NewEmail}.", $"email-change-{action}", actor, r.AccountId.ToString(), ct);

	private async Task<bool> SendEmailsAsync(IEnumerable<EmailChangeMessage> messages, EmailChangeRequest request, CancellationToken ct) {
		var success = true;
		foreach (var message in messages) {
			var model = new EmailChangeModel(message);
			var title = message.Completed ? "E-mail byl změněn" : "Potvrzení změny e-mailu";
			var body = message.Completed
				? $"{model.Greeting}, e-mail účtu byl změněn z {message.OldEmail} na {message.NewEmail}. Přihlášení nyní používá nový e-mail a stejné heslo. Ostatní zařízení se odhlásí po vypršení aktuálního přihlášení, přibližně do 10 minut. Pokud změna nebyla vaše, kontaktujte administrátora."
				: $"{model.Greeting}, změna e-mailu z {message.OldEmail} na {message.NewEmail} čeká na potvrzení obou adres. Platnost do {model.Expires} (Europe/Prague). Potvrzení: {message.ConfirmLink}\nZrušení: {message.CancelLink}\nBez přístupu k původnímu e-mailu kontaktujte administrátora.";

			success &= await EmailService.SendHtmlEmailAsync<UserEmailChangeEmail, EmailChangeModel>(
				message.Recipient,
				$"EDUCHEM LAN Party - {title}",
				model,
				serviceProvider,
				body
			);
		}

		if (!success) {
			await audit.LogWarnAsync($"Odeslani emailu selhalo: {request.OldEmail} -> {request.NewEmail}.",
				"email-change-mail-failed", null, request.AccountId.ToString(), ct);
		}

		return success;
	}

	/// <summary>
	/// payload pro zahajeni zmeny emailu.
	/// </summary>
	/// <param name="Email">novej pozadovanej email</param>
	/// <param name="Password">soucasny heslo usera pro overeni</param>
	public sealed record StartRequest([Required, MaxLength(254)] string Email, [Required, MaxLength(512)] string Password);

	/// <summary>
	/// payload pro overeni nebo konzumaci jednorazovyho tokenu.
	/// </summary>
	/// <param name="Token">jednorazovej token z mailu</param>
	public sealed record TokenRequest([Required, MaxLength(128)] string Token);
}
