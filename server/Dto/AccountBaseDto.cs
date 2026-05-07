using server.Data.Entities;

namespace server.Dto;

public class AccountBaseDto : EntityDto<Guid> {
	public required string FirstName { get; set; }
	public required string LastName { get; set; }
	public required string? Class { get; set; }
	public required School? School { get; set; }
	public required string? AvatarUrl { get; set; }
	public required string? BannerUrl { get; set; }
}