using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using server.Data.Attributes;

namespace server.Data.Entities;

[Table("Logs", Schema = "administration")]
public class LogEntry : AuditableEntity<int>
{
    [StringEnum]
    [DefaultValue(LogType.Info)]
    public LogType Type { get; set; } = LogType.Info;

    [MaxLength(32)]
    [DefaultValue("basic")]
    public string ExactType { get; set; } = "basic";

    [MaxLength(256)]
    public string Message { get; set; } = string.Empty;

    [DefaultValueSql("NOW()")]
    public DateTime? Date { get; set; }
}

public enum LogType
{
    Info,
    Warn,
    Error,
}