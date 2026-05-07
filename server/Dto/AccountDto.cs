using server.Data.Entities;

namespace server.Dto;

public class AccountDto : AccountBaseDto, IAuditable {
	public required DateTime UpdatedAtUtc { get; set; }
	public required DateTime CreatedAtUtc { get; set; }
	public required DateTime LastActiveUtc { get; set; }
	public required Gender?  Gender { get; set; }
	public required string Email { get; set; }
	public required bool EnableReservations { get; set; }
	public required AccountType AccountType { get; set; }
}