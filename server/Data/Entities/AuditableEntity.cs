using System.ComponentModel.DataAnnotations.Schema;

namespace server.Data.Entities;

public class AuditableEntity<TId> : Entity<TId>, IAuditable {
	[Column(TypeName = "timestamp with time zone")]
	public DateTime UpdatedAtUtc { get; set; }

	[Column(TypeName = "timestamp with time zone")]
	public DateTime CreatedAtUtc { get; set; }
}
