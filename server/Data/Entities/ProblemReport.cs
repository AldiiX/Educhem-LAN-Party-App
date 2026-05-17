using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using server.Data.Attributes;

namespace server.Data.Entities;

[Table("ProblemReports", Schema = "public")]
[UuidV7]
public class ProblemReport : AuditableEntity<Guid> {
	[ForeignKey(nameof(Reporter))]
	public required Guid ReporterId { get; set; }

	[AutoInclude]
	[DeleteBehavior(DeleteBehavior.Cascade)]
	public required Account Reporter { get; set; }

	[StringEnum]
	public required ProblemReportCategory Category { get; set; } = ProblemReportCategory.TechnicalProblem;
	[StringEnum]
	public required ProblemReportPriority Priority { get; set; } = ProblemReportPriority.Medium;
	[StringEnum]
	[DefaultValue(ProblemReportStatus.Pending)]
	public required ProblemReportStatus Status { get; set; } = ProblemReportStatus.Pending;

	[MaxLength(128)]
	public required string Title { get; set; }

	[MaxLength(2048)]
	public required string Description { get; set; }

	[MaxLength(128)]
	public string? Contact { get; set; }

	[MaxLength(1024)]
	public string? ResolutionNote { get; set; }

	[Column(TypeName = "timestamp with time zone")]
	public DateTime? ResolvedAtUtc { get; set; }

	[ForeignKey(nameof(ResolvedBy))]
	public Guid? ResolvedById { get; set; }

	[AutoInclude]
	[DeleteBehavior(DeleteBehavior.SetNull)]
	public Account? ResolvedBy { get; set; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ProblemReportCategory {
	TechnicalProblem,
	PhysicalDevicePc,
	PhysicalDevicePeripheral,
	Network,
	Reservation,
	Account,
	ApplicationBug,
	Tournament,
	Facility,
	Safety,
	Other
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ProblemReportPriority {
	Low,
	Medium,
	High,
	Critical
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ProblemReportStatus {
	Pending,
	Resolved,
	Unresolved
}
