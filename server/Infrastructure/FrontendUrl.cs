namespace server.Infrastructure;

public static class FrontendUrl {
	public static string GetOrigin() {
		if (!Program.ENV.TryGetValue("WEB_URL", out var webUrl) || !Uri.TryCreate(webUrl, UriKind.Absolute, out var uri)) {
			throw new InvalidOperationException("WEB_URL musi byt nastavene jako absolutni URL.");
		}
		if (uri.Scheme is not ("http" or "https") || !string.IsNullOrEmpty(uri.UserInfo) || !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment)) {
			throw new InvalidOperationException("WEB_URL musi obsahovat jen HTTP(S) originu.");
		}

		return uri.GetLeftPart(UriPartial.Authority);
	}

	public static string BuildAbsolute(string pathAndQuery) => $"{GetOrigin()}{pathAndQuery}";

	public static string BuildAbsoluteUrl(string pathAndQuery) => BuildAbsolute(pathAndQuery);
}
