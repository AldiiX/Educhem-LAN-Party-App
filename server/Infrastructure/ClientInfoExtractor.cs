using System.Net;
using System.Text.RegularExpressions;

namespace server.Infrastructure;

public sealed record ClientInfo(
	string? IpAddress,
	string? UserAgent,
	string? DeviceType,
	string? Browser,
	string? OperatingSystem,
	string? City,
	string? Country
);

public static class ClientInfoExtractor {
	public static ClientInfo Extract(HttpContext? context) {
		if (context == null) {
			return new ClientInfo(null, null, null, null, null, null, null);
		}

		var ip = ExtractIpAddress(context);
		var city = Truncate(ExtractHeader(context, "CF-IPCity"), 64);
		var country = Truncate(ExtractHeader(context, "CF-IPCountry"), 16);

		var userAgent = context.Request.Headers.UserAgent.ToString();
		if (string.IsNullOrWhiteSpace(userAgent)) {
			return new ClientInfo(ip, null, "Unknown", "Unknown", "Unknown", city, country);
		}

		var safeUserAgent = Truncate(userAgent, 500);
		var (os, deviceType) = ParseOperatingSystemAndDevice(safeUserAgent!);
		var browser = ParseBrowser(safeUserAgent!);

		return new ClientInfo(
			ip,
			safeUserAgent,
			Truncate(deviceType, 64),
			Truncate(browser, 64),
			Truncate(os, 64),
			city,
			country
		);
	}

	private static string? ExtractIpAddress(HttpContext context) {
		// 1. Zkusit RemoteIpAddress (nastaveno UseForwardedHeaders z duveryhodneho nginxu)
		if (context.Connection.RemoteIpAddress is { } remoteIp) {
			return FormatIpAddress(remoteIp);
		}

		// 2. CF-Connecting-IP
		var cfConnectingIp = context.Request.Headers["CF-Connecting-IP"].ToString();
		if (TryParseIpCandidate(cfConnectingIp, out var parsedCfIp)) {
			return FormatIpAddress(parsedCfIp);
		}

		// 3. X-Real-IP
		var xRealIp = context.Request.Headers["X-Real-IP"].ToString();
		if (TryParseIpCandidate(xRealIp, out var parsedRealIp)) {
			return FormatIpAddress(parsedRealIp);
		}

		// 4. X-Forwarded-For (prvni IP v retezci)
		var forwardedFor = context.Request.Headers["X-Forwarded-For"].ToString();
		if (!string.IsNullOrWhiteSpace(forwardedFor)) {
			var firstIp = forwardedFor.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault();
			if (TryParseIpCandidate(firstIp, out var parsedForwardedIp)) {
				return FormatIpAddress(parsedForwardedIp);
			}
		}

		return null;
	}

	private static bool TryParseIpCandidate(string? value, out IPAddress parsedIp) {
		parsedIp = IPAddress.None;
		if (string.IsNullOrWhiteSpace(value) || value.Length > 64) return false;
		return IPAddress.TryParse(value.Trim(), out parsedIp!);
	}

	private static string FormatIpAddress(IPAddress ip) {
		if (ip.IsIPv4MappedToIPv6) ip = ip.MapToIPv4();
		var formatted = ip.ToString();
		return formatted == "::1" ? "127.0.0.1" : formatted;
	}

	private static string? ExtractHeader(HttpContext context, string headerName) {
		var value = context.Request.Headers[headerName].ToString();
		return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
	}

	private static string? Truncate(string? value, int maxLength) {
		if (string.IsNullOrWhiteSpace(value)) return null;
		var trimmed = value.Trim();
		return trimmed.Length > maxLength ? trimmed[..maxLength] : trimmed;
	}

	private static (string Os, string DeviceType) ParseOperatingSystemAndDevice(string ua) {
		if (ua.Contains("iPhone", StringComparison.OrdinalIgnoreCase)) {
			return ("iOS", "Mobile");
		}
		if (ua.Contains("iPad", StringComparison.OrdinalIgnoreCase)) {
			return ("iPadOS", "Tablet");
		}
		if (ua.Contains("Android", StringComparison.OrdinalIgnoreCase)) {
			var isTablet = !ua.Contains("Mobile", StringComparison.OrdinalIgnoreCase);
			return ("Android", isTablet ? "Tablet" : "Mobile");
		}
		if (ua.Contains("Windows", StringComparison.OrdinalIgnoreCase)) {
			return ("Windows", "Desktop");
		}
		if (ua.Contains("Macintosh", StringComparison.OrdinalIgnoreCase) || ua.Contains("Mac OS X", StringComparison.OrdinalIgnoreCase)) {
			return ("macOS", "Desktop");
		}
		if (ua.Contains("Linux", StringComparison.OrdinalIgnoreCase) || ua.Contains("X11", StringComparison.OrdinalIgnoreCase)) {
			return ("Linux", "Desktop");
		}
		if (ua.Contains("CrOS", StringComparison.OrdinalIgnoreCase)) {
			return ("Chrome OS", "Desktop");
		}

		return ("Ostatní", "Desktop");
	}

	private static string ParseBrowser(string ua) {
		if (ua.Contains("Edg/", StringComparison.OrdinalIgnoreCase) || ua.Contains("Edge/", StringComparison.OrdinalIgnoreCase)) {
			return "Microsoft Edge";
		}
		if (ua.Contains("OPR/", StringComparison.OrdinalIgnoreCase) || ua.Contains("Opera", StringComparison.OrdinalIgnoreCase)) {
			return "Opera";
		}
		if (ua.Contains("Brave", StringComparison.OrdinalIgnoreCase)) {
			return "Brave";
		}
		if (ua.Contains("Chrome/", StringComparison.OrdinalIgnoreCase) || ua.Contains("CriOS/", StringComparison.OrdinalIgnoreCase)) {
			return "Google Chrome";
		}
		if (ua.Contains("Firefox/", StringComparison.OrdinalIgnoreCase) || ua.Contains("FxiOS/", StringComparison.OrdinalIgnoreCase)) {
			return "Firefox";
		}
		if (ua.Contains("Safari/", StringComparison.OrdinalIgnoreCase) && !ua.Contains("Chrome", StringComparison.OrdinalIgnoreCase)) {
			return "Safari";
		}
		if (ua.Contains("Postman", StringComparison.OrdinalIgnoreCase)) {
			return "Postman";
		}
		if (ua.Contains("curl", StringComparison.OrdinalIgnoreCase)) {
			return "cURL";
		}

		return "Prohlížeč";
	}
}
