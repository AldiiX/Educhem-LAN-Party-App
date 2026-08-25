using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace server.Data.Entities;

[Table("DiscordConnections", Schema = "public")]
[Index(nameof(DiscordId), IsUnique = true)]
public class DiscordConnection {
	[Key]
	public Guid AccountId { get; set; }

	[MaxLength(32)]
	public required string DiscordId { get; set; }

	[MaxLength(32)]
	public required string Username { get; set; }

	[MaxLength(2048)]
	public required string AccessToken { get; set; }

	[MaxLength(2048)]
	public required string RefreshToken { get; set; }

	[Column(TypeName = "timestamp with time zone")]
	public DateTime AccessTokenExpiresAtUtc { get; set; }

	[Column(TypeName = "timestamp with time zone")]
	public DateTime? LastValidatedUtc { get; set; }

	public required Account Account { get; set; }
}
