namespace server.Data.Entities;

public class AuditableEntity<TId> : Entity<TId>, IAuditable {
	public DateTime UpdatedAtUtc { get; set; }
	public DateTime CreatedAtUtc { get; set; }
}