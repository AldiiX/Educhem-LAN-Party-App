using server.Data.Entities;

namespace server.Dto;

public class EntityDto<TId> {
	public required TId Id {get; set;}
}