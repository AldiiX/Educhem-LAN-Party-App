using server.Data.Entities;

namespace server.Services;

public interface IDbLoggerService {
    Task<bool> LogAsync(LogType type, string message, string exactType = "basic", Guid? actorId = null, string? targetId = null, CancellationToken ct = default);
    Task<bool> LogErrorAsync(string message, string exactType = "basic", Guid? actorId = null, string? targetId = null, CancellationToken ct = default);
    Task<bool> LogInfoAsync(string message, string exactType = "basic", Guid? actorId = null, string? targetId = null, CancellationToken ct = default);
    Task<bool> LogWarnAsync(string message, string exactType = "basic", Guid? actorId = null, string? targetId = null, CancellationToken ct = default);
}