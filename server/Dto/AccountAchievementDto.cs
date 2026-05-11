namespace server.Dto;

public sealed class AccountAchievementDto : EntityDto<Guid> {
	public required AchievementDto Achievement { get; set; }
	public required bool IsHidden { get; set; }
	public required DateTime CreatedAtUtc { get; set; }
}
