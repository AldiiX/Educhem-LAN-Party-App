using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using server.Data.Attributes;

namespace server.Data.Entities;

[Table("AuthSessions", Schema = "public")]
[Index(nameof(RefreshTokenHash), IsUnique = true)]
[Index(nameof(AccountId))]
[UuidV7]
public sealed class AuthSession : Entity<Guid> {
	public required Guid AccountId { get; set; }
	public Account Account { get; set; } = null!;

	[MaxLength(64)]
	public required string RefreshTokenHash { get; set; }

	[Column(TypeName = "timestamp with time zone")]
	public required DateTime CreatedAtUtc { get; set; }

	[Column(TypeName = "timestamp with time zone")]
	public required DateTime ExpiresAtUtc { get; set; }

	[Column(TypeName = "timestamp with time zone")]
	public DateTime? RevokedAtUtc { get; set; }

	public required bool IsPersistent { get; set; }

	[MaxLength(45)]
	public string? IpAddress { get; set; }

	[MaxLength(512)]
	public string? UserAgent { get; set; }

	[MaxLength(64)]
	public string? DeviceType { get; set; }

	[MaxLength(64)]
	public string? Browser { get; set; }

	[MaxLength(64)]
	public string? OperatingSystem { get; set; }

	[MaxLength(64)]
	public string? City { get; set; }

	[MaxLength(16)]
	public string? Country { get; set; }

	[Column(TypeName = "timestamp with time zone")]
	public DateTime LastActiveUtc { get; set; }
}
