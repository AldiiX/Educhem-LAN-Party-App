using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace server.Data.Entities;

[Table(("Computers"), Schema = "reservations")]
public sealed class Computer : Entity<string> {
	[MaxLength(255)]
	public required string? ImageUrl { get; set; }

	[MaxLength(64)]
	public required string? Label { get; set; }

	[DeleteBehavior(DeleteBehavior.Cascade)]
	public required Room? Room { get; set; }

	public required bool Available { get; set; }

	public required bool IsTeachersComputer { get; set; }
}