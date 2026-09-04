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
		var city = ExtractHeader(context, "CF-IPCity");
		var country = ExtractHeader(context, "CF-IPCountry");

		var userAgent = context.Request.Headers.UserAgent.ToString();
		if (string.IsNullOrWhiteSpace(userAgent)) {
			return new ClientInfo(ip, null, "Unknown", "Unknown", "Unknown", city, country);
		}

		// Zkrátit User-Agent pokud je delší než 500 znaků
		var safeUserAgent = userAgent.Length > 500 ? userAgent[..500] : userAgent;

		var (os, deviceType) = ParseOperatingSystemAndDevice(safeUserAgent);
		var browser = ParseBrowser(safeUserAgent);

		return new ClientInfo(ip, safeUserAgent, deviceType, browser, os, city, country);
	}

	private static string? ExtractIpAddress(HttpContext context) {
		var cfConnectingIp = context.Request.Headers["CF-Connecting-IP"].ToString();
		if (!string.IsNullOrWhiteSpace(cfConnectingIp)) {
			return cfConnectingIp.Trim();
		}

		var remoteIp = context.Connection.RemoteIpAddress?.ToString();
		if (remoteIp == "::1") return "127.0.0.1";
		if (!string.IsNullOrWhiteSpace(remoteIp)) return remoteIp;

		var xRealIp = context.Request.Headers["X-Real-IP"].ToString();
		if (!string.IsNullOrWhiteSpace(xRealIp)) {
			return xRealIp.Trim();
		}

		var forwardedFor = context.Request.Headers["X-Forwarded-For"].ToString();
		if (!string.IsNullOrWhiteSpace(forwardedFor)) {
			var firstIp = forwardedFor.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault();
			if (!string.IsNullOrWhiteSpace(firstIp)) {
				return firstIp;
			}
		}

		return null;
	}

	private static string? ExtractHeader(HttpContext context, string headerName) {
		var value = context.Request.Headers[headerName].ToString();
		return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
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
