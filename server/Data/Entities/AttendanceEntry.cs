using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using server.Data.Attributes;

namespace server.Data.Entities;

[Table("AttendanceEntries", Schema = "attendance")]
[Index(nameof(AccountId), nameof(CreatedAtUtc))]
[UuidV7]
public class AttendanceEntry : AuditableEntity<Guid> {
	[ForeignKey(nameof(Account))]
	public required Guid AccountId { get; set; }

	[AutoInclude]
	[DeleteBehavior(DeleteBehavior.Cascade)]
	public required Account Account { get; set; }

	[StringEnum]
	public required AttendanceEntryType Type { get; set; }

	[MaxLength(256)]
	public string? Reason { get; set; }

	[ForeignKey(nameof(CreatedBy))]
	public required Guid CreatedById { get; set; }

	[AutoInclude]
	[DeleteBehavior(DeleteBehavior.Restrict)]
	public required Account CreatedBy { get; set; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AttendanceEntryType {
	CheckIn,
	CheckOut
}
