using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace server.Data.Entities;

[Table(("Reservations"), Schema = "reservations")]
[Index(nameof(AccountId), IsUnique = true)]
public class Reservation : AuditableEntity<Guid> {
	[ForeignKey(nameof(Account))]
	public required Guid AccountId { get; set; }
	public required Account Account { get; set; }
	public required string? Note { get; set; }
}
