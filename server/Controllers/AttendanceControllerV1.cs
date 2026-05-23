using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using server.Data;
using server.Data.Entities;
using server.Dto;
using server.Dto.Mappers;
using server.Infrastructure;
using server.Services;

namespace server.Controllers;

[ApiController]
[Route("api/v1/attendance")]
public sealed class AttendanceControllerV1(
	IAuthService auth,
	AppDbContext db
) : Controller {

	[HttpGet]
	public async Task<IActionResult> GetAttendance(CancellationToken ct = default) {
		var acc = await auth.ReAuthAsync(ct);
		if(acc == null) return new UnauthorizedResult();

		var entries = await db.AttendanceEntriesEf()
			.AsNoTracking()
			.ToListAsync(ct);
		var participants = await db.AccountsEf()
			.AsNoTracking()
			.Where(a => a.EnableReservations)
			.OrderBy(a => a.LastName)
			.ThenBy(a => a.FirstName)
			.ToListAsync(ct);
		var latestByAccount = entries
			.GroupBy(e => e.AccountId)
			.ToDictionary(g => g.Key, g => g.OrderByDescending(e => e.CreatedAtUtc).First());
		var participantDtos = participants
			.Select(participant => {
				latestByAccount.TryGetValue(participant.Id, out var latestEntry);
				return new AttendanceParticipantDto(
					participant.ToProfileDto(),
					latestEntry?.Type,
					latestEntry?.ToDto(false)
				);
			})
			.OrderBy(p => p.Profile.FullName)
			.ToList();
		var present = participantDtos.Count(p => p.CurrentState == AttendanceEntryType.CheckIn);

		return Ok(new AttendanceOverviewDto(
			entries.Select(e => e.ToDto()).ToList(),
			participantDtos,
			new AttendanceStatsDto(present, Math.Max(0, participants.Count - present), participants.Count)
		));
	}

	[HttpPost]
	public async Task<IActionResult> CreateAttendanceEntry([FromBody] CreateAttendanceEntryRequest request, CancellationToken ct = default) {
		var acc = await auth.ReAuthAsync(ct);
		if(acc == null) return new UnauthorizedResult();

		var targetAccountId = request.AccountId ?? acc.Id;
		var isSelfEntry = targetAccountId == acc.Id;

		if(!isSelfEntry && !HasRoleAtLeast(acc, AccountType.TeacherOrg)) {
			return Forbid();
		}

		var targetAccount = await db.AccountsEf().FirstOrDefaultAsync(a => a.Id == targetAccountId, ct);
		if(targetAccount == null) return NotFound();

		if(!isSelfEntry && !CanManageAccount(acc, targetAccount)) {
			return Forbid();
		}

		var latestEntry = await db.AttendanceEntries
			.AsNoTracking()
			.Where(e => e.AccountId == targetAccount.Id)
			.OrderByDescending(e => e.CreatedAtUtc)
			.FirstOrDefaultAsync(ct);
		var expectedType = latestEntry?.Type == AttendanceEntryType.CheckIn
			? AttendanceEntryType.CheckOut
			: AttendanceEntryType.CheckIn;

		if(request.Type != expectedType) {
			return BadRequest(expectedType == AttendanceEntryType.CheckIn
				? "Účastník aktuálně není na akci, můžeš zapsat jen příchod."
				: "Účastník aktuálně je na akci, můžeš zapsat jen odchod.");
		}

		var reason = NormalizeOptional(request.Reason);
		if(request.Type == AttendanceEntryType.CheckOut && reason == null) {
			return BadRequest("U odchodu je potřeba vyplnit důvod.");
		}

		var actor = isSelfEntry ? targetAccount : await db.AccountsEf().FirstOrDefaultAsync(a => a.Id == acc.Id, ct);
		if(actor == null) return new UnauthorizedResult();

		var entry = new AttendanceEntry {
			Id = Guid.Empty,
			AccountId = targetAccount.Id,
			Account = targetAccount,
			Type = request.Type,
			Reason = reason,
			CreatedById = acc.Id,
			CreatedBy = actor,
		};

		db.AttendanceEntries.Add(entry);
		await db.SaveChangesAsync(ct);

		var created = await db.AttendanceEntriesEf()
			.AsNoTracking()
			.FirstAsync(e => e.Id == entry.Id, ct);

		return Ok(created.ToDto());
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

	private static string? NormalizeOptional(string? value) {
		return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
	}

	public sealed record CreateAttendanceEntryRequest(
		AttendanceEntryType Type,
		Guid? AccountId,
		string? Reason
	);
}
