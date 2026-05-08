namespace server.Dto;

public class SchoolDto : EntityDto<ushort> {
	public required string Slug  { get; set; }
	public required string ShortName { get; set; }
	public required string DisplayName { get; set; }
	public required string IconUrl { get; set; }
}
