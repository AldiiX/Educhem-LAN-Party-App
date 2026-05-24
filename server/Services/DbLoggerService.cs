using server.Data;
using server.Data.Entities;

namespace server.Services;

public class DbLoggerService(AppDbContext db) : IDbLoggerService {
    private const int MaxExactTypeLength = 32;
    private const int MaxMessageLength = 256;

    public async Task<bool> LogAsync(LogType type, string message, string exactType = "basic", CancellationToken ct = default) {
        var entry = new LogEntry {
            Id = 0,
            Type = type,
            ExactType = TrimToMaxLength(exactType, MaxExactTypeLength),
            Message = TrimToMaxLength(message, MaxMessageLength),
            Date = DateTime.UtcNow
        };

        db.LogEntries.Add(entry);
        return await db.SaveChangesAsync(ct) > 0;
    }

    public async Task<bool> LogErrorAsync(string message, string exactType = "basic", CancellationToken ct = default) => await LogAsync(LogType.Error, message, exactType, ct);

    public async Task<bool> LogInfoAsync(string message, string exactType = "basic", CancellationToken ct = default) => await LogAsync(LogType.Info, message, exactType, ct);

    public async Task<bool> LogWarnAsync(string message, string exactType = "basic", CancellationToken ct = default) => await LogAsync(LogType.Warn, message, exactType, ct);

    private static string TrimToMaxLength(string value, int maxLength)
    {
        if (value.Length <= maxLength)
        {
            return value;
        }

        return value[..Math.Max(0, maxLength - 1)] + "…";
    }
}
