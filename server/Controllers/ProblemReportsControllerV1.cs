using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
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
[Route("api/v1/problem-reports")]
[Authorize]
public sealed class ProblemReportsControllerV1(
	IAuthService auth,
	AppDbContext db,
	IAppSettingsService appSettings,
	IDbLoggerService dbLogger
) : Controller {
	private static readonly TimeSpan CreateCooldown = TimeSpan.FromMinutes(30);

	[HttpGet]
	public async Task<IActionResult> GetProblemReports(CancellationToken ct = default) {
		var user = auth.GetCurrentUser();
		if(user == null) return new UnauthorizedResult();

		var query = db.ProblemReportsEf().AsNoTracking();
		if(!HasRoleAtLeast(user.Value.Role, AccountType.TeacherOrg)) {
			query = query.Where(report => report.ReporterId == user.Value.Id);
		}

		var reports = (await query.ToListAsync(ct))
			.Select(report => report.ToDto())
			.ToList();

		return Ok(reports);
	}

	[HttpGet("availability")]
	public async Task<IActionResult> GetAvailability(CancellationToken ct = default) {
		return Ok(new ProblemReportsAvailabilityResponse(
			await appSettings.GetProblemReportsEnabledAsync(ct)
		));
	}

	[HttpPost]
	public async Task<IActionResult> CreateProblemReport([FromBody] CreateProblemReportRequest request, CancellationToken ct = default) {
		var user = auth.GetCurrentUser();
		if(user == null) return new UnauthorizedResult();
		if(!HasRoleAtLeast(user.Value.Role, AccountType.SuperAdmin) && !await appSettings.GetProblemReportsEnabledAsync(ct)) {
			return StatusCode(StatusCodes.Status423Locked, "Hlášení problémů je momentálně vypnuté.");
		}

		if(string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Description))
			return BadRequest("Missing required problem report fields.");
		if(request.Title.Trim().Length > 128)
			return BadRequest("Název hlášení může mít nejvýše 128 znaků.");
		if(request.Description.Trim().Length > 2048)
			return BadRequest("Popis problému může mít nejvýše 2048 znaků.");
		if(request.Contact?.Trim().Length > 128)
			return BadRequest("Kontakt může mít nejvýše 128 znaků.");
		if(!Enum.IsDefined(request.Category) || !Enum.IsDefined(request.Priority)) {
			return BadRequest("Invalid problem report category or priority.");
		}

		if(!HasRoleAtLeast(user.Value.Role, AccountType.TeacherOrg)) {
			var nowUtc = DateTime.UtcNow;
			var latestReportCreatedAtUtc = await db.ProblemReports
				.AsNoTracking()
				.Where(r => r.ReporterId == user.Value.Id)
				.OrderByDescending(r => r.CreatedAtUtc)
				.Select(r => (DateTime?)r.CreatedAtUtc)
				.FirstOrDefaultAsync(ct);
			if(latestReportCreatedAtUtc is not null) {
				var retryAfter = CreateCooldown - (nowUtc - latestReportCreatedAtUtc.Value);
				if(retryAfter > TimeSpan.Zero) {
					await dbLogger.LogWarnAsync(
						$"Problem report cooldown hit by account {user.Value.Id}; retry after {Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds))}s",
						"problem-report-cooldown",
						user.Value.Id,
						null,
						ct
					);
					return Cooldown(
						retryAfter,
						user.Value.CommunicationStyle,
						"Další hlášení můžeš vytvořit za {0} sekund.",
						"Další hlášení můžete vytvořit za {0} sekund."
					);
				}
			}
		}

		var report = new ProblemReport {
			Id = Guid.Empty,
			ReporterId = user.Value.Id,
			Category = request.Category,
			Priority = request.Priority,
			Status = ProblemReportStatus.Pending,
			Title = request.Title.Trim(),
			Description = request.Description.Trim(),
			Contact = NormalizeOptional(request.Contact),
			ResolutionNote = null,
			ResolvedAtUtc = null,
			ResolvedById = null,
			ResolvedBy = null,
		};

		db.ProblemReports.Add(report);
		await db.SaveChangesAsync(ct);

		var created = await db.ProblemReportsEf()
			.AsNoTracking()
			.FirstAsync(r => r.Id == report.Id, ct);

		return Ok(created.ToDto());
	}

	[HttpPut("{id:guid}/status")]
	[Authorize(Policy = AuthPolicies.TeacherOrg)]
	public async Task<IActionResult> UpdateProblemReportStatus(Guid id, [FromBody] UpdateProblemReportStatusRequest request, CancellationToken ct = default) {
		var accountId = auth.GetCurrentAccountId();
		if(accountId == null) return new UnauthorizedResult();
		if(!Enum.IsDefined(request.Status)) {
			return BadRequest("Invalid problem report status.");
		}
		if(request.ResolutionNote?.Trim().Length > 1024) {
			return BadRequest("Poznámka k vyřešení může mít nejvýše 1024 znaků.");
		}

		var report = await db.ProblemReports.FirstOrDefaultAsync(r => r.Id == id, ct);
		if(report == null) return NotFound();

		report.Status = request.Status;
		report.ResolutionNote = NormalizeOptional(request.ResolutionNote);

		if(request.Status == ProblemReportStatus.Pending) {
			report.ResolvedAtUtc = null;
			report.ResolvedById = null;
		} else {
			report.ResolvedAtUtc = DateTime.UtcNow;
			report.ResolvedById = accountId.Value;
		}

		await db.SaveChangesAsync(ct);

		var updated = await db.ProblemReportsEf()
			.AsNoTracking()
			.FirstAsync(r => r.Id == report.Id, ct);

		return Ok(updated.ToDto());
	}

	[HttpDelete("{id:guid}")]
	[Authorize(Policy = AuthPolicies.TeacherOrg)]
	public async Task<IActionResult> DeleteProblemReport(Guid id, CancellationToken ct = default) {
		var report = await db.ProblemReports.FirstOrDefaultAsync(r => r.Id == id, ct);
		if(report == null) return NotFound();

		db.ProblemReports.Remove(report);
		await db.SaveChangesAsync(ct);

		return NoContent();
	}

	private static bool HasRoleAtLeast(AccountType accountType, AccountType requiredType) {
		return accountType >= requiredType;
	}

	private static string? NormalizeOptional(string? value) {
		return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
	}

	private IActionResult Cooldown(TimeSpan retryAfter, CommunicationStyle communicationStyle, string informalMessageFormat, string formalMessageFormat) {
		var seconds = Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds));
		Response.Headers["Retry-After"] = seconds.ToString();
		return StatusCode(StatusCodes.Status429TooManyRequests, string.Format(Phrase(communicationStyle, informalMessageFormat, formalMessageFormat), seconds));
	}

	private static string Phrase(CommunicationStyle communicationStyle, string informal, string formal) {
		return communicationStyle == CommunicationStyle.Informal ? informal : formal;
	}

	public sealed record CreateProblemReportRequest(
		ProblemReportCategory Category,
		ProblemReportPriority Priority,
		[MaxLength(128)] string Title,
		[MaxLength(2048)] string Description,
		[MaxLength(128)] string? Contact
	);

	public sealed record UpdateProblemReportStatusRequest(
		ProblemReportStatus Status,
		[MaxLength(1024)] string? ResolutionNote
	);

	public sealed record ProblemReportsAvailabilityResponse(bool Enabled);
}
