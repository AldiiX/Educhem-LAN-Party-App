using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace server.Data.Entities;

[Table("Schools",  Schema = "public")]
[Index(nameof(Slug), IsUnique = true)]
public sealed class School : Entity<ushort> {
	[MaxLength(64)]
	public required string Slug { get; set; }

	[MaxLength(128)]
	public required string DisplayName { get; set; }

	[MaxLength(512)]
	public required string IconUrl { get; set; }
}