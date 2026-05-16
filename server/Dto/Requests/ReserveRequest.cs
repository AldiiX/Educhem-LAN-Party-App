namespace server.Dto.Requests;

public sealed class ReserveRequest {
	public string Id { get; set; } = string.Empty;
	public string Type { get; set; } = string.Empty;
}