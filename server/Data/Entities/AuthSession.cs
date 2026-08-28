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
}
