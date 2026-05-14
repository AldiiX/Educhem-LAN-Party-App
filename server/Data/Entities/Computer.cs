using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace server.Data.Entities;

[Table(("Computers"), Schema = "reservations")]
public sealed class Computer : Entity<string> {
	[MaxLength(255)]
	public required string? ImageUrl { get; set; }

	public required Room? Room { get; set; }

	public required bool Available { get; set; }

	public required bool IsTeachersComputer { get; set; }
}