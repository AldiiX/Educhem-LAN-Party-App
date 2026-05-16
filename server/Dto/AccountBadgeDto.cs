namespace server.Dto;

public sealed class AccountBadgeDto : EntityDto<Guid> {
	public required BadgeDto Badge { get; set; }
	public required bool IsTakenOut { get; set; }
}

