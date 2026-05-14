using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace server.Data.Entities;

[Table(("Reservations"), Schema = "reservations")]
[PrimaryKey(nameof(AccountId))]
public class Reservation : Auditable {
	[ForeignKey(nameof(Account))]
	public required Guid AccountId { get; set; }
	public required Account Account { get; set; }
	public required string? Note { get; set; }
}