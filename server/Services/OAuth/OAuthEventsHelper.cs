using Microsoft.AspNetCore.Authentication;

namespace server.Services.OAuth;

/// <summary>
/// zachytava selhani nebo zruseni v ASP.NET Core OAuth middleware pipeline a presmerovava na frontend
/// </summary>
public static class OAuthEventsHelper {
	public static Task HandleRemoteFailure(RemoteFailureContext context) {
		var returnOrigin = context.Properties?.Items.TryGetValue("origin", out var origin) == true && !string.IsNullOrWhiteSpace(origin)
			? origin
			: (Program.ENV.TryGetValue("WEB_URL", out var webUrl) ? webUrl : "/");
		var flow = context.Properties?.Items.TryGetValue("flow", out var f) == true ? f : "login";
		var provider = context.Properties?.Items.TryGetValue("provider", out var p) == true ? p : "oauth";

		var isCancelled = context.Failure?.Message?.Contains("access_denied", StringComparison.OrdinalIgnoreCase) == true
			|| context.Request.Query.ContainsKey("error")
			|| context.Request.Query["error"].ToString().Contains("access_denied", StringComparison.OrdinalIgnoreCase);
		var status = isCancelled ? "cancelled" : "error";

		var targetPath = flow == "connect"
			? $"/app/account/settings?{provider}={status}"
			: $"/app/login?{provider}={status}";

		var redirectUrl = new Uri(new Uri(returnOrigin), targetPath).ToString();
		context.Response.Redirect(redirectUrl);
		context.HandleResponse();
		return Task.CompletedTask;
	}
}