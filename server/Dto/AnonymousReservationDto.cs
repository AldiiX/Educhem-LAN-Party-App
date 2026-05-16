namespace server.Dto;

public class AnonymousReservationDto : AuditableEntityDto<Guid> {
	public string Profile => "Anonymous";
	public required string? Note { get; set; }
	public required RoomDto? Room { get; set; }
	public required ComputerDto? Computer { get; set; }
}
