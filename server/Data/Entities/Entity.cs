using System.ComponentModel.DataAnnotations;

using server.Data.Attributes;

namespace server.Data.Entities;

public class Entity<TId> {
	[MaxLength(255)]
	public required TId Id { get; set; }
}
