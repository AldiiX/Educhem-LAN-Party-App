using server.Data.Entities;

namespace server.Dto;

public class ProfileDto : EntityDto<Guid> {
	public required string FirstName { get; set; }
	public required string LastName { get; set; }
	public string FullName => $"{FirstName} {LastName}";
	public required string? Class { get; set; }
	public required SchoolDto? School { get; set; }
	public required string? AvatarUrl { get; set; }
	public required string? BannerUrl { get; set; }
	public required Gender? Gender { get; set; }
	public required DateTime CreatedAtUtc { get; set; }
	public required AccountType AccountType { get; set; }
}