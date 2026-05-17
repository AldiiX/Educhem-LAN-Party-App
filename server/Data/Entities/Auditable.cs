using System.ComponentModel.DataAnnotations.Schema;

using server.Data.Attributes;

namespace server.Data.Entities;

public abstract class Auditable : IAuditable {
	[Column(TypeName = "timestamp with time zone")]
	[DefaultValueSql("now()")]
	public DateTime UpdatedAtUtc { get; set; }

	[Column(TypeName = "timestamp with time zone")]
	[DefaultValueSql("now()")]
	public DateTime CreatedAtUtc { get; set; }
}
