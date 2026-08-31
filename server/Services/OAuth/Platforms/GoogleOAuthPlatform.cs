using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using server.Data.Entities;

namespace server.Services.OAuth.Platforms;

/// <summary>
/// implementuje integraci pro platformu Google
/// </summary>
internal sealed class GoogleOAuthPlatform : IOAuthPlatform {
	/// <inheritdoc />
	public OAuthProvider Provider => OAuthProvider.Google;

	/// <inheritdoc />
	public string Scheme => GoogleDefaults.AuthenticationScheme;

	/// <inheritdoc />
	public bool IsConfigured => HasEnv("GOOGLE_CLIENT_ID") && HasEnv("GOOGLE_CLIENT_SECRET");

	/// <summary>
	/// registruje google providera do ASP.NET Core authentication builderu
	/// </summary>
	/// <param name="builder">authentication builder pro registraci handleru</param>
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

	/// <inheritdoc />
	public ExtractedOAuthProfile ExtractProfile(ClaimsPrincipal principal, AuthenticationProperties properties) {
		var providerUserId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";
		var username = principal.FindFirst(ClaimTypes.Name)?.Value ?? "Google účet";
		var pic = principal.FindFirst("image")?.Value
			?? principal.FindFirst("picture")?.Value
			?? principal.FindFirst("urn:google:image")?.Value
			?? principal.FindFirst("urn:google:picture")?.Value;

		return new ExtractedOAuthProfile(providerUserId, username, FormatGoogleAvatar(pic), null);
	}

	/// <inheritdoc />
	public Task<PlatformValidationResult> ValidateConnectionAsync(OAuthConnection connection, CancellationToken ct) =>
		Task.FromResult(new PlatformValidationResult(PlatformValidationStatus.Valid, connection.Username, connection.AvatarUrl));

	/// <inheritdoc />
	public Task RevokeConnectionAsync(OAuthConnection connection, CancellationToken ct) => Task.CompletedTask;

	/// <summary>
	/// upravi rozliseni google avataru z vychozi male velikosti na kvalitni 512px
	/// </summary>
	/// <param name="pictureUrl">puvodni picture url z google claimu</param>
	/// <returns>upravena url ve vysokem rozliseni</returns>
	private static string? FormatGoogleAvatar(string? pictureUrl) {
		if (string.IsNullOrWhiteSpace(pictureUrl) || !Uri.TryCreate(pictureUrl, UriKind.Absolute, out var uri)) return pictureUrl;
		if (uri.Scheme != "https" || !uri.Host.EndsWith(".googleusercontent.com", StringComparison.OrdinalIgnoreCase)) return pictureUrl;

		var baseUrl = uri.GetLeftPart(UriPartial.Path);
		var parameterIndex = baseUrl.LastIndexOf('=');
		if (parameterIndex > baseUrl.LastIndexOf('/')) baseUrl = baseUrl[..parameterIndex];
		return $"{baseUrl}=s512-c";
	}

	/// <summary>
	/// overuje pritomnost neprazdne promenne v konfiguraci prostredi
	/// </summary>
	/// <param name="key">nazev environment promenne</param>
	/// <returns>true pokud promenna existuje a neni prazdna</returns>
	private static bool HasEnv(string key) => Program.ENV.TryGetValue(key, out var val) && !string.IsNullOrWhiteSpace(val);
}