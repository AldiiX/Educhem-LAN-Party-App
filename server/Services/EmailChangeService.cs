using Microsoft.EntityFrameworkCore;
using server.Data;
using server.Data.Entities;
using server.Infrastructure;
using System.Text.Json.Serialization;

namespace server.Services;

public sealed record EmailChangeStatus(Guid Id, string OldEmail, string NewEmail, DateTime ExpiresAtUtc,
	bool OldConfirmed, bool NewConfirmed, string State, DateTime? ResendAtUtc, CommunicationStyle CommunicationStyle);
public sealed record EmailChangeResult(EmailChangeStatus? Request = null, string? Error = null,
	int StatusCode = 200, bool EmailsSent = true, string? TokenAction = null) {
	[JsonIgnore] public Guid? CompletedAccountId { get; init; }
}
public sealed record EmailChangeMessage(string Recipient, Account Account, string OldEmail, string NewEmail,
	DateTime ExpiresAtUtc, string? ConfirmLink = null, string? CancelLink = null, bool Completed = false);

public interface IEmailChangeMailer {
	Task<bool> SendAsync(EmailChangeMessage message);
}

public sealed class EmailChangeService(AppDbContext db, IEmailChangeMailer mailer, IOAuthService oauth,
	IDbLoggerService audit) {
	private const string InvalidLink = "Odkaz je neplatný, již použitý nebo vypršel.";
	private const string UnavailableEmail = "Tuto adresu nelze použít.";
	public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(30);

	public async Task<EmailChangeResult> StatusAsync(Guid accountId, CancellationToken ct) {
		var request = await db.EmailChangeRequests.AsNoTracking().Include(r => r.Account)
			.FirstOrDefaultAsync(r => r.AccountId == accountId, ct);
		return new(request == null ? null : await ToStatusAsync(request, false, ct));
	}

	public async Task<EmailChangeResult> StartAsync(Guid accountId, string? email, string? password, CancellationToken ct) {
		var origin = oauth.GetFrontendOrigin();
		if (origin == null) return new(Error: "Odesílání potvrzení není dostupné.", StatusCode: 503);
		await using var transaction = await db.Database.BeginTransactionAsync(ct);
		var account = await db.GetAccountForUpdateAsync(accountId, ct);
		if (account == null) return new(Error: "Účet není dostupný.", StatusCode: 401);
		var limited = await CheckLimitAsync(accountId, ct);
		if (limited != null) return limited;
		db.EmailChangeAttempts.Add(new() { AccountId = accountId, CreatedAtUtc = Now });
		await db.SaveChangesAsync(ct);
		// neuspesny heslo se taky pocita, at nejde zkouset donekonecna
		if (password == null || password.Length > 512 || !AuthService.VerifyPassword(password, account.PasswordHash)) {
			await transaction.CommitAsync(ct);
			return new(Error: "Nesprávné současné heslo.", StatusCode: 400);
		}
		if (!AccountEmail.TryNormalize(email, out var normalized) || normalized == account.Email.Trim().ToLowerInvariant()
			|| await db.Accounts.AnyAsync(a => a.Email == normalized, ct)) {
			await transaction.CommitAsync(ct);
			return new(Error: UnavailableEmail, StatusCode: 400);
		}
		var previous = await db.EmailChangeRequests.FirstOrDefaultAsync(r => r.AccountId == accountId, ct);
		if (previous != null) {
			if (State(previous) == "pending") await LogAsync(previous, "cancel", "Nahrazena zadost", accountId, ct);
			db.EmailChangeRequests.Remove(previous);
			await db.SaveChangesAsync(ct);
		}
		var request = new EmailChangeRequest {
			AccountId = accountId, Account = account,
			OldEmail = account.Email, NewEmail = normalized, CreatedAtUtc = Now, ExpiresAtUtc = Now.Add(Lifetime),
			CancelTokenHash = "",
		};
		var cancel = OneTimeToken.Create();
		request.CancelTokenHash = OneTimeToken.Hash(cancel)!;
		var messages = ConfirmationMessages(request, origin, cancel);
		db.EmailChangeRequests.Add(request);
		await LogAsync(request, "request", "Zadost o zmenu", accountId, ct);
		await transaction.CommitAsync(ct);
		return new(await ToStatusAsync(request, false, ct), EmailsSent: await SendAsync(messages, request, ct));
	}

	public async Task<EmailChangeResult> ResendAsync(Guid accountId, CancellationToken ct) {
		var origin = oauth.GetFrontendOrigin();
		if (origin == null) return new(Error: "Odesílání potvrzení není dostupné.", StatusCode: 503);
		await using var transaction = await db.Database.BeginTransactionAsync(ct);
		if (await db.GetAccountForUpdateAsync(accountId, ct) == null) return new(Error: InvalidLink, StatusCode: 400);
		var request = await db.EmailChangeRequests.Include(r => r.Account).FirstOrDefaultAsync(r => r.AccountId == accountId, ct);
		if (request == null || State(request) != "pending") return new(Error: InvalidLink, StatusCode: 400);
		var limited = await CheckLimitAsync(accountId, ct);
		if (limited != null) return limited;
		db.EmailChangeAttempts.Add(new() { AccountId = accountId, CreatedAtUtc = Now });
		string? cancel = null;
		if (request.OldConfirmedAtUtc == null) {
			cancel = OneTimeToken.Create();
			request.CancelTokenHash = OneTimeToken.Hash(cancel)!;
		}
		var messages = ConfirmationMessages(request, origin, cancel);
		await LogAsync(request, "resend", "Znovu odeslano potvrzeni", accountId, ct);
		await transaction.CommitAsync(ct);
		return new(await ToStatusAsync(request, false, ct), EmailsSent: await SendAsync(messages, request, ct));
	}

	public async Task<EmailChangeResult> CancelAsync(Guid accountId, CancellationToken ct) {
		await using var transaction = await db.Database.BeginTransactionAsync(ct);
		if (await db.GetAccountForUpdateAsync(accountId, ct) == null) return new(Error: InvalidLink, StatusCode: 400);
		var request = await db.EmailChangeRequests.Include(r => r.Account).FirstOrDefaultAsync(r => r.AccountId == accountId, ct);
		if (request == null || State(request) != "pending") return new(Error: InvalidLink, StatusCode: 400);
		request.CancelledAtUtc = Now;
		await LogAsync(request, "cancel", "Zrusena zadost", accountId, ct);
		await db.SaveChangesAsync(ct);
		await transaction.CommitAsync(ct);
		return new(await ToStatusAsync(request, false, ct));
	}

	public async Task<EmailChangeResult> PreviewAsync(string token, CancellationToken ct) {
		var match = await FindTokenAsync(token, ct);
		return match == null ? new(Error: InvalidLink, StatusCode: 400)
			: new(await ToStatusAsync(match.Request, true, ct), TokenAction: match.Action);
	}

	public async Task<EmailChangeResult> ConfirmAsync(string token, CancellationToken ct) {
		var initial = await FindTokenAsync(token, ct);
		if (initial == null) return new(Error: InvalidLink, StatusCode: 400);
		await using var transaction = await db.Database.BeginTransactionAsync(ct);
		db.ChangeTracker.Clear();
		if (await db.GetAccountForUpdateAsync(initial.Request.AccountId, ct) == null)
			return new(Error: InvalidLink, StatusCode: 400);
		var match = await FindTokenAsync(token, ct);
		if (match == null) return new(Error: InvalidLink, StatusCode: 400);
		var request = match.Request;
		var kind = match.Action;
		if (kind == "cancel") {
			request.CancelledAtUtc = Now;
			await LogAsync(request, "cancel", "Zruseno odkazem", null, ct);
		} else {
			if (kind == "old") { request.OldConfirmedAtUtc = Now; request.OldTokenHash = null; }
			else { request.NewConfirmedAtUtc = Now; request.NewTokenHash = null; }
			await LogAsync(request, "confirm", kind == "old" ? "Potvrzen puvodni email" : "Potvrzen novy email", null, ct);
			if (request.OldConfirmedAtUtc != null && request.NewConfirmedAtUtc != null) {
				if (await db.Accounts.AnyAsync(a => a.Id != request.AccountId && a.Email == request.NewEmail, ct)) {
					request.CancelledAtUtc = Now;
					await LogAsync(request, "cancel", "Cilovy email uz nejde pouzit", null, ct);
					await db.SaveChangesAsync(ct);
					await transaction.CommitAsync(ct);
					return new(Error: UnavailableEmail, StatusCode: 409);
				}
				request.CompletedAtUtc = Now;
				request.Account.Email = request.NewEmail;
				await LogAsync(request, "complete", "Dokoncena zmena", null, ct);
			}
		}
		await db.SaveChangesAsync(ct);
		await transaction.CommitAsync(ct);
		var sent = true;
		if (request.CompletedAtUtc != null) {
			sent = await SendAsync([
				new(request.OldEmail, request.Account, request.OldEmail, request.NewEmail, request.ExpiresAtUtc, Completed: true),
				new(request.NewEmail, request.Account, request.OldEmail, request.NewEmail, request.ExpiresAtUtc, Completed: true),
			], request, ct);
		}
		return new(await ToStatusAsync(request, true, ct), EmailsSent: sent, TokenAction: kind) {
			CompletedAccountId = request.CompletedAtUtc == null ? null : request.AccountId,
		};
	}

	private static DateTime Now => DateTime.UtcNow;

	private async Task<EmailChangeResult?> CheckLimitAsync(Guid id, CancellationToken ct) {
		var since = Now.AddHours(-1);
		var attempts = await db.EmailChangeAttempts.Where(a => a.AccountId == id && a.CreatedAtUtc > since)
			.OrderByDescending(a => a.CreatedAtUtc).Select(a => a.CreatedAtUtc).ToListAsync(ct);
		if (attempts.Count >= 5) return new(Error: "Nejvýše 5 žádostí nebo odeslání za hodinu. Zkuste to později.", StatusCode: 429);
		if (attempts.Count > 0 && attempts[0].AddSeconds(60) > Now)
			return new(Error: "Mezi žádostmi a odesláním je potřeba počkat 60 sekund.", StatusCode: 429);
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

	private string State(EmailChangeRequest r) => r.CompletedAtUtc != null ? "completed"
		: r.CancelledAtUtc != null || r.OldEmail != r.Account.Email ? "cancelled"
		: r.ExpiresAtUtc <= Now ? "expired" : "pending";

	private async Task<EmailChangeStatus> ToStatusAsync(EmailChangeRequest r, bool masked, CancellationToken ct) {
		DateTime? resendAt = null;
		if (!masked) {
			var since = Now.AddHours(-1);
			var attempts = await db.EmailChangeAttempts.Where(a => a.AccountId == r.AccountId && a.CreatedAtUtc > since)
				.OrderBy(a => a.CreatedAtUtc).Select(a => a.CreatedAtUtc).ToListAsync(ct);
			if (attempts.Count > 0) resendAt = attempts.Count >= 5 ? attempts[0].AddHours(1) : attempts[^1].AddSeconds(60);
		}
		return new(r.Id, masked ? AccountEmail.Mask(r.OldEmail) : r.OldEmail, masked ? AccountEmail.Mask(r.NewEmail) : r.NewEmail,
			r.ExpiresAtUtc, r.OldConfirmedAtUtc != null, r.NewConfirmedAtUtc != null, State(r), resendAt, r.Account.CommunicationStyle);
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

	private async Task<bool> SendAsync(IEnumerable<EmailChangeMessage> messages, EmailChangeRequest request, CancellationToken ct) {
		var success = true;
		foreach (var message in messages) success &= await mailer.SendAsync(message);
		if (!success) await audit.LogWarnAsync($"Odeslani emailu selhalo: {request.OldEmail} -> {request.NewEmail}.",
			"email-change-mail-failed", null, request.AccountId.ToString(), ct);
		return success;
	}
}
