using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace server.Infrastructure;

/// <summary>
/// jednorazovy emailovy tokeny nemaji zadnej obsah; do databaze jde jen jejich hash
/// </summary>
public static class OneTimeToken {
	public static string Create() => Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(32));

	public static string? Hash(string? token) {
		if (token is not { Length: 43 } || token.Any(character => !char.IsAsciiLetterOrDigit(character) && character != '-' && character != '_'))
			return null;
		return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
	}
}
