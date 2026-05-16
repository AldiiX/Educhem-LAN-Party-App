using server.Data.Entities;

namespace server.Dto;

public sealed class BadgeDto : EntityDto<Guid> {
	public required string Name { get; set; }
	public string? Description { get; set; }
	public string? IconUrl { get; set; }
}

