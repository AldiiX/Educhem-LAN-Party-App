using server.Data.Entities;

namespace server.Dto;

public class ProblemReportDto : AuditableEntityDto<Guid> {
	public required ProblemReportCategory Category { get; set; }
	public required ProblemReportPriority Priority { get; set; }
	public required ProblemReportStatus Status { get; set; }
	public required string Title { get; set; }
	public required string Description { get; set; }
	public required string? Contact { get; set; }
	public required string? ResolutionNote { get; set; }
	public required DateTime? ResolvedAtUtc { get; set; }
	public required AccountDto Reporter { get; set; }
	public required AccountDto? ResolvedBy { get; set; }
}
