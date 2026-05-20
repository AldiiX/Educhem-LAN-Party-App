using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using server.Data.Attributes;

namespace server.Data.Entities;

[Table("Logs", Schema = "administration")]
public class LogEntry : AuditableEntity<int>
{
    [StringEnum]
    [DefaultValue(LogType.INFO)]
    public LogType Type { get; set; } = LogType.INFO;

    [MaxLength(32)]
    [DefaultValue("basic")]
    public string ExactType { get; set; } = "basic";

    [MaxLength(256)]
    public string Message { get; set; } = string.Empty;

    [DefaultValueSql("CURRENT_TIMESTAMP")]
    public DateTime? Date { get; set; }
}

public enum LogType
{
    INFO,
    WARN,
    ERROR,
}