using server.Data.Entities;

namespace server.Dto;

public sealed class ReservationDto : AuditableEntityDto<Guid> {
	public required ProfileDto Profile { get; set; }
	public required string? Note { get; set; }
	public required RoomDto? Room { get; set; }
	public required ComputerDto? Computer { get; set; }
}
