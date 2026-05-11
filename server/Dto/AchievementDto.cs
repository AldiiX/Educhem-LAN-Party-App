using server.Data.Entities;

namespace server.Dto;

public sealed class AchievementDto : EntityDto<Guid> {
	public required string Key { get; set; }
	public required string Name { get; set; }
	public string? Description { get; set; }
	public string? IconUrl { get; set; }
	public bool IsHidden { get; set; }
}

