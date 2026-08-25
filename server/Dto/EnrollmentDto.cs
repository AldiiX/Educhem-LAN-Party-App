namespace server.Dto;

public sealed class EnrollmentDto {
	public required SchoolDto School { get; set; }
	public string? Class { get; set; }
}
