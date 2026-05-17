using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using server.Data.Attributes;

namespace server.Data.Entities;

[Table("Accounts", Schema = "public")]
[Index(nameof(Email), IsUnique = true)]
[UuidV7]
public class Account : AuditableEntity<Guid> {
	[MaxLength(32)]
	public required string FirstName { get; set; }

	[MaxLength(32)]
	public required string LastName { get; set; }

	[MaxLength(96)]
	public required string Email { get; set; }

	[MaxLength(512)]
	public required string PasswordHash { get; set; }

	[MaxLength(16)]
	public string? Class { get; set; }

	public Gender? Gender { get; set; }

	[AutoInclude]
	public School? School { get; set; }

	[Column(TypeName = "timestamp with time zone")]
	[DefaultValueSql("now()")]
	public DateTime LastActiveUtc { get; set; } = DateTime.UtcNow;

	[DefaultValue(AccountType.Student)]
	public AccountType AccountType { get; set; } = AccountType.Student;

	[MaxLength(512)]
	public string? AvatarUrl  { get; set; }

	[MaxLength(512)]
	public string? BannerUrl  { get; set; }

	[DefaultValue(false)]
	public bool EnableReservations { get; set; } = false;

	public ICollection<AccountAchievement> AccountAchievements { get; set; } = new List<AccountAchievement>();
	public ICollection<AccountBadge> AccountBadges { get; set; } = new List<AccountBadge>();
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Gender {
	Male,
	Female,
	Other
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AccountType {
	Student = 0,
	Teacher = 1,
	TeacherOrg = 2,
	Admin = 3,
	SuperAdmin = 4
}
