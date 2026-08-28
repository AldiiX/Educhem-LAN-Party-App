using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using server.Data.Entities;

namespace server.Services.OAuth.Platforms;

/// <summary>
/// implementuje integraci pro platformu Google
/// </summary>
internal sealed class GoogleOAuthPlatform : IOAuthPlatform {
	public OAuthProvider Provider => OAuthProvider.Google;
	public string Scheme => GoogleDefaults.AuthenticationScheme;
	public bool IsConfigured => HasEnv("GOOGLE_CLIENT_ID") && HasEnv("GOOGLE_CLIENT_SECRET");

	public static void ConfigureAuthentication(AuthenticationBuilder builder) {
		if (!Program.ENV.TryGetValue("GOOGLE_CLIENT_ID", out var clientId) || string.IsNullOrWhiteSpace(clientId) ||
			!Program.ENV.TryGetValue("GOOGLE_CLIENT_SECRET", out var clientSecret) || string.IsNullOrWhiteSpace(clientSecret)) {
			return;
		}

		builder.AddGoogle(options => {
			options.ClientId = clientId;
			options.ClientSecret = clientSecret;
			options.CallbackPath = "/api/v1/google/callback";
			options.SignInScheme = "ExternalCookie";
			options.SaveTokens = true;
			options.Events.OnRemoteFailure = OAuthEventsHelper.HandleRemoteFailure;
		});
	}

	public ExtractedOAuthProfile ExtractProfile(ClaimsPrincipal principal, AuthenticationProperties properties) {
		var providerUserId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";
		var username = principal.FindFirst(ClaimTypes.Name)?.Value ?? "Google účet";
		var pic = principal.FindFirst("image")?.Value
			?? principal.FindFirst("picture")?.Value
			?? principal.FindFirst("urn:google:image")?.Value
			?? principal.FindFirst("urn:google:picture")?.Value;

		return new ExtractedOAuthProfile(providerUserId, username, FormatGoogleAvatar(pic), null);
	}

	public Task<PlatformValidationResult> ValidateConnectionAsync(OAuthConnection connection, CancellationToken ct) =>
		Task.FromResult(new PlatformValidationResult(PlatformValidationStatus.Valid, connection.Username, connection.AvatarUrl));

	public Task RevokeConnectionAsync(OAuthConnection connection, CancellationToken ct) => Task.CompletedTask;

	private static string? FormatGoogleAvatar(string? pictureUrl) {
		if (string.IsNullOrWhiteSpace(pictureUrl) || !Uri.TryCreate(pictureUrl, UriKind.Absolute, out var uri)) return pictureUrl;
		if (uri.Scheme != "https" || !uri.Host.EndsWith(".googleusercontent.com", StringComparison.OrdinalIgnoreCase)) return pictureUrl;

		var baseUrl = uri.GetLeftPart(UriPartial.Path);
		var parameterIndex = baseUrl.LastIndexOf('=');
		if (parameterIndex > baseUrl.LastIndexOf('/')) baseUrl = baseUrl[..parameterIndex];
		return $"{baseUrl}=s512-c";
	}

	private static bool HasEnv(string key) => Program.ENV.TryGetValue(key, out var val) && !string.IsNullOrWhiteSpace(val);
}
