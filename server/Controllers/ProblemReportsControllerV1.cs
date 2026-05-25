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
public sealed class ProblemReportsControllerV1(
	IAuthService auth,
	AppDbContext db,
	IAppSettingsService appSettings
) : Controller {

	[HttpGet]
	public async Task<IActionResult> GetProblemReports(CancellationToken ct = default) {
		var acc = await auth.ReAuthAsync(ct);
		if(acc == null) return new UnauthorizedResult();

		var query = db.ProblemReportsEf().AsNoTracking();
		if(!HasRoleAtLeast(acc, AccountType.TeacherOrg)) {
			query = query.Where(report => report.ReporterId == acc.Id);
		}

		var reports = (await query.ToListAsync(ct))
			.Select(report => report.ToDto())
			.ToList();

		return Ok(reports);
	}

	[HttpGet("availability")]
	public async Task<IActionResult> GetAvailability(CancellationToken ct = default) {
		var acc = await auth.ReAuthAsync(ct);
		if(acc == null) return new UnauthorizedResult();

		return Ok(new ProblemReportsAvailabilityResponse(
			await appSettings.GetProblemReportsEnabledAsync(ct)
		));
	}

	[HttpPost]
	public async Task<IActionResult> CreateProblemReport([FromBody] CreateProblemReportRequest request, CancellationToken ct = default) {
		var acc = await auth.ReAuthAsync(ct);
		if(acc == null) return new UnauthorizedResult();
		if(!await appSettings.GetProblemReportsEnabledAsync(ct)) {
			return StatusCode(StatusCodes.Status423Locked, "Hlaseni problemu je momentalne uzamcene.");
		}

		if(string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Description))
			return BadRequest("Missing required problem report fields.");

		var reporter = await db.Accounts.FirstOrDefaultAsync(a => a.Id == acc.Id, ct);
		if(reporter == null) return new UnauthorizedResult();

		var report = new ProblemReport {
			Id = Guid.Empty,
			ReporterId = acc.Id,
			Reporter = reporter,
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
	public async Task<IActionResult> UpdateProblemReportStatus(Guid id, [FromBody] UpdateProblemReportStatusRequest request, CancellationToken ct = default) {
		var acc = await auth.ReAuthAsync(ct);
		if(acc == null) return new UnauthorizedResult();
		if(!HasRoleAtLeast(acc, AccountType.TeacherOrg))
			return Forbid();

		var report = await db.ProblemReports.FirstOrDefaultAsync(r => r.Id == id, ct);
		if(report == null) return NotFound();

		report.Status = request.Status;
		report.ResolutionNote = NormalizeOptional(request.ResolutionNote);

		if(request.Status == ProblemReportStatus.Pending) {
			report.ResolvedAtUtc = null;
			report.ResolvedById = null;
		} else {
			report.ResolvedAtUtc = DateTime.UtcNow;
			report.ResolvedById = acc.Id;
		}

		await db.SaveChangesAsync(ct);

		var updated = await db.ProblemReportsEf()
			.AsNoTracking()
			.FirstAsync(r => r.Id == report.Id, ct);

		return Ok(updated.ToDto());
	}

	[HttpDelete("{id:guid}")]
	public async Task<IActionResult> DeleteProblemReport(Guid id, CancellationToken ct = default) {
		var acc = await auth.ReAuthAsync(ct);
		if(acc == null) return new UnauthorizedResult();
		if(!HasRoleAtLeast(acc, AccountType.TeacherOrg))
			return Forbid();

		var report = await db.ProblemReports.FirstOrDefaultAsync(r => r.Id == id, ct);
		if(report == null) return NotFound();

		db.ProblemReports.Remove(report);
		await db.SaveChangesAsync(ct);

		return NoContent();
	}

	private static bool HasRoleAtLeast(Account account, AccountType accountType) {
		return account.AccountType >= accountType;
	}

	private static string? NormalizeOptional(string? value) {
		return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
	}

	public sealed record CreateProblemReportRequest(
		ProblemReportCategory Category,
		ProblemReportPriority Priority,
		string Title,
		string Description,
		string? Contact
	);

	public sealed record UpdateProblemReportStatusRequest(
		ProblemReportStatus Status,
		string? ResolutionNote
	);

	public sealed record ProblemReportsAvailabilityResponse(bool Enabled);
}
