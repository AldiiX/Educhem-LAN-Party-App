using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

using server.Data.Attributes;

namespace server.Data.Entities;

[Table(("Reservations"), Schema = "reservations")]
[Index(nameof(AccountId), IsUnique = true)]
[UuidV7]
public class Reservation : AuditableEntity<Guid> {
	[ForeignKey(nameof(Account))]
	public required Guid AccountId { get; set; }
	[AutoInclude]
	public required Account Account { get; set; }
	public required string? Note { get; set; }
}
