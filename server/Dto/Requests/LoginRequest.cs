using System.ComponentModel.DataAnnotations;

namespace server.Dto.Requests;

public sealed class LoginRequest {
	[Length(1,255), Required]
	public required string Email { get; init; }

	[Length(1,255), Required]
	public required string PasswordPlain { get; init; }
}