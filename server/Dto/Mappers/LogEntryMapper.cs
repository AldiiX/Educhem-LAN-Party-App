using server.Data.Entities;

namespace server.Dto.Mappers;

public static class LogEntryMapper {
    public static LogEntryDto ToDto(this LogEntry log) {
        return new LogEntryDto {
            Id = log.Id,
            CreatedAtUtc = log.CreatedAtUtc,
            UpdatedAtUtc = log.UpdatedAtUtc,
            Date = log.Date,
            Message = log.Message,
            Type = log.Type,
            ExactType = log.ExactType,
        };
    }
}