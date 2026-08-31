using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using server.Data;
using server.Data.Entities;
using server.Dto;
using server.Dto.Mappers;
using server.Infrastructure;

namespace server.Controllers;

[ApiController]
[Route("api/v1/adm")]
[Authorize(Policy = AuthPolicies.Admin)]
public sealed class AdminLogsControllerV1(AppDbContext db) : Controller {
    private const int AdministrationPageSize = 25;

    [HttpGet("logs")]
    public async Task<IActionResult> GetLogs(
        [FromQuery] int page = 1,
        [FromQuery(Name = "q")] string? search = null,
        [FromQuery] string? actorId = null,
        [FromQuery] string? targetId = null,
        [FromQuery] string[]? logType = null,
        [FromQuery] string[]? exactType = null,
        [FromQuery] DateTimeOffset? dateFrom = null,
        [FromQuery] DateTimeOffset? dateTo = null,
        CancellationToken ct = default
    ) {
        var query = db.LogEntries.AsNoTracking();
        var logTypes = EnumFilters.ParseEnumFilters<LogType>(logType);
        var exactTypes = exactType?
            .Select(value => value.Trim())
            .Where(value => value.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .ToArray() ?? [];

        if (!string.IsNullOrWhiteSpace(actorId)) {
            var normalizedActorId = actorId.Trim().ToLower();
            query = query.Where(log => log.ActorId.HasValue && log.ActorId.Value.ToString().ToLower().Contains(normalizedActorId));
        }

        if (!string.IsNullOrWhiteSpace(targetId)) {
            var normalizedTargetId = targetId.Trim().ToLower();
            query = query.Where(log => log.TargetId != null && log.TargetId.ToLower().Contains(normalizedTargetId));
        }

        if (logTypes.Length > 0) query = query.Where(log => logTypes.Contains(log.Type));
        if (exactTypes.Length > 0) query = query.Where(log => exactTypes.Contains(log.ExactType));
        if (dateFrom.HasValue) query = query.Where(log => log.Date >= dateFrom.Value.UtcDateTime);
        if (dateTo.HasValue) query = query.Where(log => log.Date <= dateTo.Value.UtcDateTime);

        if (!string.IsNullOrWhiteSpace(search)) {
            var normalizedSearch = search.Trim().ToLower();
            query = query.Where(log =>
                log.Message.ToLower().Contains(normalizedSearch)
                || log.ExactType.ToLower().Contains(normalizedSearch)
                || (log.ActorId.HasValue && log.ActorId.Value.ToString().ToLower().Contains(normalizedSearch))
                || (log.TargetId != null && log.TargetId.ToLower().Contains(normalizedSearch))
            );
        }

        var totalItems = await db.LogEntries.AsNoTracking().CountAsync(ct);
        var totalEntries = await query.CountAsync(ct);
        var totalPages = totalEntries == 0 ? 0 : (int)Math.Ceiling(totalEntries / (double)AdministrationPageSize);
        var currentPage = totalPages == 0 ? 1 : Math.Clamp(page, 1, totalPages);
        var logEntities = await query
            .OrderByDescending(log => log.Date)
            .ThenByDescending(log => log.Id)
            .Skip((currentPage - 1) * AdministrationPageSize)
            .Take(AdministrationPageSize)
            .ToListAsync(ct);
        var logTypeCounts = await db.LogEntries
            .AsNoTracking()
            .GroupBy(log => log.Type)
            .Select(group => new {Value = group.Key, Count = group.Count()})
            .ToListAsync(ct);
        var logTypeOptions = logTypeCounts
            .OrderBy(option => option.Value)
            .Select(option => new ValueCountDto<LogType>(option.Value, option.Count))
            .ToList();
        var exactTypeCounts = await db.LogEntries
            .AsNoTracking()
            .GroupBy(log => log.ExactType)
            .Select(group => new {Value = group.Key, Count = group.Count()})
            .ToListAsync(ct);
        var exactTypeOptions = exactTypeCounts
            .OrderBy(option => option.Value)
            .Select(option => new ValueCountDto<string>(option.Value, option.Count))
            .ToList();

        return Ok(new AdministrationLogsPageDto(
            logEntities.Select(log => log.ToDto()).ToList(),
            new PaginationDto(currentPage, AdministrationPageSize, totalEntries, totalPages),
            totalItems,
            new LogFilterOptionsDto(logTypeOptions, exactTypeOptions)
        ));
    }
}
