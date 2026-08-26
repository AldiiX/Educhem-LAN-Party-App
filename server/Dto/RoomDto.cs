namespace server.Dto;

public sealed class RoomDto : EntityDto<string> {
	public required string Label { get; set; }
	public required string? ImageUrl { get; set; }
	public required ushort Capacity { get; set; }
	public required bool Available { get; set; }
}
