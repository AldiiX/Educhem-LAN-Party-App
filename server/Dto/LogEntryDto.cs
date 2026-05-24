using server.Data.Entities;

namespace server.Dto;

public class LogEntryDto : AuditableEntityDto<int> {
    public required DateTime? Date { get; set; }
    public required LogType Type { get; set; }
    public required string ExactType { get; set; }
    public required string Message { get; set; }
}