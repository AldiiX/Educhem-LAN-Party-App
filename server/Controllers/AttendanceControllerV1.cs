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
	AppDbContext db,
	IAppSettingsService appSettings,
	AppCacheService cache
) : Controller {
	private const string AttendanceOverviewCacheKey = "attendance:overview";

	[HttpGet]
	public async Task<IActionResult> GetAttendance(CancellationToken ct = default) {
		var acc = await auth.ReAuthAsync(ct);
		if(acc == null) return new UnauthorizedResult();

		var attendanceEnabled = await appSettings.GetAttendanceEnabledAsync(ct);
		if(cache.TryGetValue(AttendanceOverviewCacheKey, out AttendanceOverviewDto? cachedOverview)
		   && cachedOverview is not null
		   && cachedOverview.AttendanceEnabled == attendanceEnabled) {
			return Ok(cachedOverview);
		}

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

		var overview = new AttendanceOverviewDto(
			entries.Select(e => e.ToDto()).ToList(),
			participantDtos,
			new AttendanceStatsDto(present, Math.Max(0, participants.Count - present), participants.Count),
			attendanceEnabled
		);
		cache.Set(AttendanceOverviewCacheKey, overview);

		return Ok(overview);
	}

	[HttpPost]
	public async Task<IActionResult> CreateAttendanceEntry([FromBody] CreateAttendanceEntryRequest request, CancellationToken ct = default) {
		var acc = await auth.ReAuthAsync(ct);
		if(acc == null) return new UnauthorizedResult();
		if(!await appSettings.GetAttendanceEnabledAsync(ct)) {
			return StatusCode(StatusCodes.Status423Locked, "Dochazka je momentalne uzamcena.");
		}

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

		var delta = await BuildDeltaAsync(created, ct);
		UpdateCachedOverview(delta);

		return Ok(delta);
	}

	private async Task<AttendanceDeltaDto> BuildDeltaAsync(AttendanceEntry created, CancellationToken ct) {
		var latestEntry = await db.AttendanceEntriesEf()
			.AsNoTracking()
			.Where(e => e.AccountId == created.AccountId)
			.OrderByDescending(e => e.CreatedAtUtc)
			.FirstAsync(ct);
		var participant = await db.AccountsEf()
			.AsNoTracking()
			.FirstAsync(a => a.Id == created.AccountId, ct);
		var presentAccountIds = await db.AttendanceEntries
			.AsNoTracking()
			.GroupBy(e => e.AccountId)
			.Select(g => new {
				AccountId = g.Key,
				LatestType = g.OrderByDescending(e => e.CreatedAtUtc).First().Type,
			})
			.Where(x => x.LatestType == AttendanceEntryType.CheckIn)
			.Select(x => x.AccountId)
			.ToListAsync(ct);
		var total = await db.Accounts
			.AsNoTracking()
			.CountAsync(a => a.EnableReservations, ct);
		var present = presentAccountIds.Count;

		return new AttendanceDeltaDto(
			created.ToDto(),
			new AttendanceParticipantDto(
				participant.ToProfileDto(),
				latestEntry.Type,
				latestEntry.ToDto(false)
			),
			new AttendanceStatsDto(present, Math.Max(0, total - present), total)
		);
	}

	private void UpdateCachedOverview(AttendanceDeltaDto delta) {
		if(!cache.TryGetValue(AttendanceOverviewCacheKey, out AttendanceOverviewDto? overview)
		   || overview is null) {
			return;
		}

		var entries = new[] { delta.Entry }
			.Concat(overview.Entries.Where(entry => entry.Id != delta.Entry.Id))
			.ToList();
		var participants = overview.Participants
			.Select(participant => participant.Profile.Id == delta.Participant.Profile.Id ? delta.Participant : participant)
			.ToList();

		if(!participants.Any(participant => participant.Profile.Id == delta.Participant.Profile.Id)) {
			participants.Add(delta.Participant);
			participants = participants.OrderBy(participant => participant.Profile.FullName).ToList();
		}

		cache.Set(AttendanceOverviewCacheKey, overview with {
			Entries = entries,
			Participants = participants,
			Stats = delta.Stats,
		});
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
