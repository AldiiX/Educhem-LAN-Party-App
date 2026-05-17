using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using System.ComponentModel;

namespace server.Data.Entities;

[Table(("Rooms"), Schema = "reservations")]
public sealed class Room : Entity<string> {
	[MaxLength(64)]
	public required string? Label { get; set; }

	[MaxLength(255)]
	public required string? ImageUrl { get; set; }

	[DefaultValue(1)]
	public required ushort Capacity { get; set; }

	[DefaultValue(true)]
	public required bool Available { get; set; }
}
