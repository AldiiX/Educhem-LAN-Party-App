using server.Data.Entities;

namespace server.Dto.Mappers;

public static class LogEntryMapper {
    extension(LogEntry log) {
        public LogEntryDto ToDto() {
            return new LogEntryDto {
                Id = log.Id,
                CreatedAtUtc = log.CreatedAtUtc,
                UpdatedAtUtc = log.UpdatedAtUtc,
                Date = log.Date,
                Message = log.Message
            };
        }
    }
}