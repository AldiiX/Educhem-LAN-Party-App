namespace server.Dto;

public class ComputerDto : EntityDto<string> {
	public required RoomDto? Room { get; set; }

	public required string? ImageUrl {
		get {
			if (field == null) return Room?.ImageUrl ?? null;
			return field;
		}

		set;
	}

	public required bool Available { get; set; }
	public required bool IsTeachersComputer { get; set; }
	public string Label => Id;
}