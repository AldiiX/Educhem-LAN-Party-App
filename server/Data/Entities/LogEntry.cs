using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using server.Data.Attributes;

namespace server.Data.Entities;

[UuidV7]
[Table("Logs", Schema = "public")]
public class LogEntry : AuditableEntity<Guid>
{
    public LogType Type { get; set; } = LogType.INFO;

    [MaxLength(32)]
    public string ExactType { get; set; } = "basic";

    [MaxLength(256)]
    public string Message { get; set; } = string.Empty;

    public DateTime Date { get; set; } = DateTime.UtcNow;
}

public enum LogType
{
    INFO,
    WARNING,
    ERROR,
}