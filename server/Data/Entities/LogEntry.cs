using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using server.Data.Attributes;

namespace server.Data.Entities;

[Table("Logs", Schema = "administration")]
public sealed class LogEntry : AuditableEntity<int> {
    [StringEnum]
    [DefaultValue(LogType.Info)]
    public LogType Type { get; set; } = LogType.Info;

    [MaxLength(32)]
    [DefaultValue("basic")]
    public string ExactType { get; set; } = "basic";

    [MaxLength(256)]
    public string Message { get; set; } = string.Empty;

    public Guid? ActorId { get; set; }

    [MaxLength(64)]
    public string? TargetId { get; set; }

    [DefaultValueSql("NOW()")]
    public DateTime? Date { get; set; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum LogType {
    Info,
    Warn,
    Error,
}
