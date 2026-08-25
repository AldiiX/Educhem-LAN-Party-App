using System.Text.Json.Serialization;
using server.Data.Entities;

namespace server.Dto;

public class ProfileDto : EntityDto<Guid> {
	public required string FirstName { get; set; }
	public required string LastName { get; set; }
	public string FullName => $"{FirstName} {LastName}".Trim();
	public required EnrollmentDto? Enrollment { get; set; }
	public required string? AvatarUrl { get; set; }
	public required string? BannerUrl { get; set; }
	public string? DiscordUsername { get; set; }
	[JsonPropertyName("githubUsername")]
	public string? GitHubUsername { get; set; }
	[JsonPropertyName("githubProfileUrl")]
	public string? GitHubProfileUrl { get; set; }
	public string? GoogleName { get; set; }
	public string? SteamUsername { get; set; }
	public string? SteamProfileUrl { get; set; }
	public required Gender? Gender { get; set; }
	public required DateTime CreatedAtUtc { get; set; }
	public required AccountType AccountType { get; set; }
	public IReadOnlyList<AccountAchievementDto> Achievements { get; set; } = new List<AccountAchievementDto>();
	public IReadOnlyList<AccountBadgeDto> Badges { get; set; } = new List<AccountBadgeDto>();
}
