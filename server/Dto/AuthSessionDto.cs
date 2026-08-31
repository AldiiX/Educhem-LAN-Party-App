namespace server.Dto;

public sealed class AuthSessionDto {
	public required Guid Id { get; set; }
	public string? DeviceType { get; set; }
	public string? Browser { get; set; }
	public string? OperatingSystem { get; set; }
	public string? IpAddress { get; set; }
	public string? City { get; set; }
	public string? Country { get; set; }
	public required DateTime CreatedAtUtc { get; set; }
	public required DateTime LastActiveUtc { get; set; }
	public required bool IsCurrent { get; set; }
}
