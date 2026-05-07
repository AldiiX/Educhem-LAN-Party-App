namespace server.Dto;

public sealed class AccountSessionDto : AccountDto {
	public required string PasswordHash { get; set; }
}