using System.Globalization;
using System.Text.RegularExpressions;

namespace server.Infrastructure;

public static partial class AccountEmail {
	public static bool TryNormalize(string? value, out string email) {
		email = "";
		if (string.IsNullOrWhiteSpace(value) || value.Length > 254) return false;
		var parts = value.Trim().Split('@');
		if (parts.Length != 2 || parts[0].Length is < 1 or > 64
			|| parts[0].StartsWith('.') || parts[0].EndsWith('.') || parts[0].Contains("..")) return false;
		try {
			email = $"{parts[0]}@{new IdnMapping().GetAscii(parts[1])}".ToLowerInvariant();
		} catch (ArgumentException) { return false; }
		return email.Length <= 96 && EmailPattern().IsMatch(email);
	}

	public static string Mask(string email) {
		var parts = email.Split('@');
		if (parts.Length != 2) return "***";
		var domain = parts[1].Split('.');
		return $"{parts[0][..Math.Min(1, parts[0].Length)]}***@{domain[0][..Math.Min(1, domain[0].Length)]}***.{domain[^1]}";
	}

	[GeneratedRegex(@"^[a-z0-9!#$%&'*+/=?^_`{|}~.\-]+@(?:[a-z0-9](?:[a-z0-9\-]{0,61}[a-z0-9])?\.)+[a-z](?:[a-z0-9\-]{0,61}[a-z0-9])?$", RegexOptions.CultureInvariant, 100)]
	private static partial Regex EmailPattern();
}
