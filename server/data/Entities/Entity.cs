namespace server.Data.Entities;

public class Entity<TId> : Auditable {
	public required TId Id { get; set; }
}