namespace server.Dto;

public class AnonymousReservationDto {
	public string Profile => "Anonymous";
	public required string? Note { get; set; }
	public required DateTime UpdatedAtUtc { get; set; }
	public required DateTime CreatedAtUtc { get; set; }
	public required RoomDto? Room { get; set; }
	public required ComputerDto? Computer { get; set; }
}