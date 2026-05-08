using server.Data.Entities;

namespace server.Dto;

public class AccountDto : ProfileDto, IAuditable {
	public required DateTime UpdatedAtUtc { get; set; }
	public required DateTime LastActiveUtc { get; set; }
	public required string Email { get; set; }
	public required bool EnableReservations { get; set; }
}