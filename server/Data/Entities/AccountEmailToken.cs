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
public sealed class AccountEmailToken {
	/// <summary>
	/// identifikator ulozenej v chranenym emailovym tokenu, podle kteryho se overuje jeho platnost
	/// </summary>
	[Key] public Guid Id { get; set; }

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
