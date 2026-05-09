namespace server.Data.Entities;

public class Entity<TId> {
	public required TId Id { get; set; }
}