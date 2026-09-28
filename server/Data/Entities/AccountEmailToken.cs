using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace server.Data.Entities;

/// <summary>
/// eviduje platnost jednorazovyho tokenu z emailu pro prihlaseni nebo obnovu hesla
/// zaznam se maze po pouziti nebo pri zmene emailu ci hesla, aby starej token nesel pouzit znovu
/// </summary>
[Table("AccountEmailLinks", Schema = "public")]
[Index(nameof(AccountId))]
[Index(nameof(TokenHash), IsUnique = true)]
public sealed class AccountEmailToken {
	/// <summary>
	/// interni id zaznamu, ktery se do odkazu neposila
	/// </summary>
	[Key] public Guid Id { get; set; }

	/// <summary>
	/// SHA-256 nahodnyho tokenu; null zustava jen u starejch neplatnejch odkazu
	/// </summary>
	[MaxLength(64)] public string? TokenHash { get; set; }

	/// <summary>
	/// oddeluje prihlaseni od resetu, aby nesel token pouzit pro jinou akci
	/// </summary>
	public AccountEmailTokenPurpose Purpose { get; set; }

	/// <summary>
	/// id uctu, pro kterej byl token vydanej
	/// </summary>
	public Guid AccountId { get; set; }

	/// <summary>
	/// ucet, ke kterymu token patri
	/// </summary>
	public Account Account { get; set; } = null!;

	/// <summary>
	/// cas v UTC, od kteryho uz token nejde pouzit
	/// </summary>
	public DateTime ExpiresAtUtc { get; set; }
}

public enum AccountEmailTokenPurpose {
	Login = 1,
	PasswordReset = 2,
}
