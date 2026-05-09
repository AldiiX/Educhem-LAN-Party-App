using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace server.Data.Entities;

[Table("Accounts", Schema = "public")]
[Index(nameof(Email), IsUnique = true)]
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
	public School? School { get; set; }
	public DateTime LastActiveUtc { get; set; } = DateTime.UtcNow;

	public AccountType AccountType { get; set; } = AccountType.Student;

	[MaxLength(512)]
	public string? AvatarUrl  { get; set; }

	[MaxLength(512)]
	public string? BannerUrl  { get; set; }

	public bool EnableReservations { get; set; } = false;
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
