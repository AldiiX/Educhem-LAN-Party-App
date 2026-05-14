using System.ComponentModel.DataAnnotations;

namespace server.Data.Entities;

public class Entity<TId> {
	[MaxLength(255)]
	public required TId Id { get; set; }
}