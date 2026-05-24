using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using server.Data;
using server.Data.Entities;
using server.Dto.Mappers;
using server.Services;

namespace server.Controllers;

[ApiController]
[Route("api/v1/adm")]
public sealed class AdminLogsControllerV1(
    IAuthService auth,
    AppDbContext db
) : Controller
{
    [HttpGet("logs")]
    public async Task<IActionResult> GetLogs(CancellationToken ct = default) {
        var acc = await auth.ReAuthFromContextOrNullAsync(ct);
        if(acc == null || acc.AccountType < AccountType.Admin)
            return new UnauthorizedObjectResult(new { success = false, message = "Nelze zobrazit logy, pokud nejsi přihlášený, nebo nemáš dostatečná práva." });

        var logs = await db.LogEntries
            .AsNoTracking()
            .OrderByDescending(l => l.Date)
            .Select(l => l.ToDto())
            .ToListAsync(ct);

        return Ok(logs);
    }
}
