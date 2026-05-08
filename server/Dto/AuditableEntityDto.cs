using server.Data.Entities;

namespace server.Dto;

public class AuditableEntityDto<TId> : EntityDto<TId>, IAuditable {
	public required DateTime UpdatedAtUtc { get; set; }
	public required DateTime CreatedAtUtc { get; set; }
}