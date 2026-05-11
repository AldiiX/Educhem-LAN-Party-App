using server.Data.Entities;

namespace server.Dto;

public sealed class ReservationDto : IAuditable {
	public required ProfileDto Profile { get; set; }
	public required string? Note { get; set; }
	public required DateTime UpdatedAtUtc { get; set; }
	public required DateTime CreatedAtUtc { get; set; }
	public required RoomDto? Room { get; set; }
	public required ComputerDto? Computer { get; set; }
}