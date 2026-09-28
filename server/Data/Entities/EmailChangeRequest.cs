using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace server.Data.Entities;

[Table("EmailChangeRequests", Schema = "public")]
[Index(nameof(AccountId), IsUnique = true)]
[Index(nameof(OldTokenHash), IsUnique = true)]
[Index(nameof(NewTokenHash), IsUnique = true)]
[Index(nameof(CancelTokenHash), IsUnique = true)]
public sealed class EmailChangeRequest {
	[Key] public Guid Id { get; set; } = Guid.NewGuid();
	public Guid AccountId { get; set; }
	public Account Account { get; set; } = null!;
	[MaxLength(96)] public required string OldEmail { get; set; }
	[MaxLength(96)] public required string NewEmail { get; set; }
	[MaxLength(64)] public string? OldTokenHash { get; set; }
	[MaxLength(64)] public string? NewTokenHash { get; set; }
	[MaxLength(64)] public required string CancelTokenHash { get; set; }
	public DateTime CreatedAtUtc { get; set; }
	public DateTime ExpiresAtUtc { get; set; }
	public DateTime? OldConfirmedAtUtc { get; set; }
	public DateTime? NewConfirmedAtUtc { get; set; }
	public DateTime? CancelledAtUtc { get; set; }
	public DateTime? CompletedAtUtc { get; set; }
}

[Table("EmailChangeAttempts", Schema = "public")]
[Index(nameof(AccountId), nameof(CreatedAtUtc))]
public sealed class EmailChangeAttempt {
	[Key] public Guid Id { get; set; } = Guid.NewGuid();
	public Guid AccountId { get; set; }
	public Account Account { get; set; } = null!;
	public DateTime CreatedAtUtc { get; set; }
}
