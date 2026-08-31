using server.Data;
using server.Data.Entities;

namespace server.Services;

internal sealed class DbLoggerService(AppDbContext db) : IDbLoggerService {
    private const int MaxExactTypeLength = 32;
    private const int MaxMessageLength = 256;
    private const int MaxTargetIdLength = 64;

    public async Task<bool> LogAsync(
        LogType type,
        string message,
        string exactType = "basic",
        Guid? actorId = null,
        string? targetId = null,
        CancellationToken ct = default
    ) {
        var entry = new LogEntry {
            Id = 0,
            Type = type,
            ExactType = TrimToMaxLength(exactType, MaxExactTypeLength),
            Message = TrimToMaxLength(message, MaxMessageLength),
            ActorId = actorId,
            TargetId = targetId == null ? null : TrimToMaxLength(targetId, MaxTargetIdLength),
            Date = DateTime.UtcNow
        };

        db.LogEntries.Add(entry);
        return await db.SaveChangesAsync(ct) > 0;
    }

    public async Task<bool> LogErrorAsync(
        string message,
        string exactType = "basic",
        Guid? actorId = null,
        string? targetId = null,
        CancellationToken ct = default
    ) => await LogAsync(LogType.Error, message, exactType, actorId, targetId, ct);

    public async Task<bool> LogInfoAsync(
        string message,
        string exactType = "basic",
        Guid? actorId = null,
        string? targetId = null,
        CancellationToken ct = default
    ) => await LogAsync(LogType.Info, message, exactType, actorId, targetId, ct);

    public async Task<bool> LogWarnAsync(
        string message,
        string exactType = "basic",
        Guid? actorId = null,
        string? targetId = null,
        CancellationToken ct = default
    ) => await LogAsync(LogType.Warn, message, exactType, actorId, targetId, ct);

    private static string TrimToMaxLength(string value, int maxLength) {
        if (value.Length <= maxLength) {
            return value;
        }

        return value[..Math.Max(0, maxLength - 1)] + "…";
    }
}
