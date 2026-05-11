namespace server.Dto;

public class ComputerDto : EntityDto<string> {
	public required string? ImageUrl { get; set; }
	public required RoomDto? Room { get; set; }
	public required bool Available { get; set; }
	public required bool IsTeachersComputer { get; set; }
	public string Label => Id;
}