using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel;
using Microsoft.EntityFrameworkCore;

using server.Data.Attributes;

namespace server.Data.Entities;

[Table(("Computers"), Schema = "reservations")]
public sealed class Computer : Entity<string> {
	[MaxLength(255)]
	public required string? ImageUrl { get; set; }

	[MaxLength(64)]
	public required string? Label { get; set; }

	[AutoInclude]
	[DeleteBehavior(DeleteBehavior.Cascade)]
	public required Room? Room { get; set; }

	[DefaultValue(true)]
	public required bool Available { get; set; }

	[DefaultValue(true)]
	public required bool IsTeachersComputer { get; set; }
}
