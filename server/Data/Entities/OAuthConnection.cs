using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using server.Data.Attributes;

namespace server.Data.Entities;

[Table("OAuthConnections", Schema = "public")]
[Index(nameof(Provider), nameof(ProviderUserId), IsUnique = true)]
public class OAuthConnection {
	public Guid AccountId { get; set; }

	[StringEnum]
	public OAuthProvider Provider { get; set; }

	[MaxLength(255)]
	public required string ProviderUserId { get; set; }

	[MaxLength(255)]
	public required string Username { get; set; }

	[MaxLength(512)]
	public string? ProfileUrl { get; set; }

	[MaxLength(512)]
	public string? AvatarUrl { get; set; }

	[MaxLength(2048)]
	public string? AccessToken { get; set; }

	[MaxLength(2048)]
	public string? RefreshToken { get; set; }

	[Column(TypeName = "timestamp with time zone")]
	public DateTime? AccessTokenExpiresAtUtc { get; set; }

	[Column(TypeName = "timestamp with time zone")]
	public DateTime? LastValidatedUtc { get; set; }

	public required Account Account { get; set; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OAuthProvider {
	Discord,
	GitHub,
	Google,
	Steam,
}
