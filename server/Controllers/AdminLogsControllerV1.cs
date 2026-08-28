using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using server.Data;
using server.Dto.Mappers;
using server.Infrastructure;

namespace server.Controllers;

[ApiController]
[Route("api/v1/adm")]
[Authorize(Policy = AuthPolicies.Admin)]
public sealed class AdminLogsControllerV1(AppDbContext db) : Controller {
    [HttpGet("logs")]
    public async Task<IActionResult> GetLogs(
        [FromQuery] Guid? actorId = null,
        [FromQuery] string? targetId = null,
        CancellationToken ct = default
    ) {
        var query = db.LogEntries.AsNoTracking();

        if (actorId.HasValue) {
            query = query.Where(l => l.ActorId == actorId.Value);
        }

        if (!string.IsNullOrWhiteSpace(targetId)) {
            var trimmedTarget = targetId.Trim();
            query = query.Where(l => l.TargetId == trimmedTarget);
        }

        var logs = await query
            .OrderByDescending(l => l.Date)
            .Select(l => l.ToDto())
            .ToListAsync(ct);

        return Ok(logs);
    }
}
