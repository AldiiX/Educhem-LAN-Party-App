using server.Data.Entities;

namespace server.Dto;

public sealed class AttendanceEntryDto : AuditableEntityDto<Guid> {
	public required AttendanceEntryType Type { get; set; }
	public required string? Reason { get; set; }
	public required ProfileDto Profile { get; set; }
	public required ProfileDto CreatedBy { get; set; }
}

public sealed record AttendanceParticipantDto(
	ProfileDto Profile,
	AttendanceEntryType? CurrentState,
	AttendanceEntryDto? LatestEntry
);

public sealed record AttendanceStatsDto(
	int Present,
	int Away,
	int Total
);

public sealed record AttendanceOverviewDto(
	IReadOnlyList<AttendanceEntryDto> Entries,
	IReadOnlyList<AttendanceParticipantDto> Participants,
	AttendanceStatsDto Stats,
	bool AttendanceEnabled
);

public sealed record AttendanceDeltaDto(
	AttendanceEntryDto Entry,
	AttendanceParticipantDto Participant,
	AttendanceStatsDto Stats
);
