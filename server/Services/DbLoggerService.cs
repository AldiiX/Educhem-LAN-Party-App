using server.Data;
using server.Data.Entities;

namespace server.Services;

public class DbLoggerService(AppDbContext db) : IDbLoggerService {
    public async Task<bool> LogAsync(IDbLoggerService.LogType type, string message, string exactType = "basic", CancellationToken ct = default) {
        var entry = new LogEntry {
            Id = 0,
            Type = type switch {
                IDbLoggerService.LogType.ERROR => LogType.Error,
                IDbLoggerService.LogType.WARN => LogType.Warn,
                _ => LogType.Info
            },
            ExactType = exactType,
            Message = message,
            Date = DateTime.UtcNow
        };

        db.LogEntries.Add(entry);
        return await db.SaveChangesAsync(ct) > 0;
    }

    public async Task<bool> LogErrorAsync(string message, string exactType = "basic", CancellationToken ct = default) => await LogAsync(IDbLoggerService.LogType.ERROR, message, exactType, ct);

    public async Task<bool> LogInfoAsync(string message, string exactType = "basic", CancellationToken ct = default) => await LogAsync(IDbLoggerService.LogType.INFO, message, exactType, ct);

    public async Task<bool> LogWarnAsync(string message, string exactType = "basic", CancellationToken ct = default) => await LogAsync(IDbLoggerService.LogType.WARN, message, exactType, ct);
}
