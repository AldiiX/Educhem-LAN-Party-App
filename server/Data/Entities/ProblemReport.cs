using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace server.Data.Entities;

[Table("ProblemReports", Schema = "public")]
public class ProblemReport : AuditableEntity<Guid> {
	[ForeignKey(nameof(Reporter))]
	public required Guid ReporterId { get; set; }

	[DeleteBehavior(DeleteBehavior.Cascade)]
	public required Account Reporter { get; set; }

	public required ProblemReportCategory Category { get; set; } = ProblemReportCategory.TechnicalProblem;
	public required ProblemReportPriority Priority { get; set; } = ProblemReportPriority.Medium;
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
