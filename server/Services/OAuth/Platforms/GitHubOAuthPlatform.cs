using System.Security.Claims;
using AspNet.Security.OAuth.GitHub;
using Microsoft.AspNetCore.Authentication;
using server.Data.Entities;

namespace server.Services.OAuth.Platforms;

/// <summary>
/// implementuje integraci pro platformu GitHub
/// </summary>
internal sealed class GitHubOAuthPlatform : IOAuthPlatform {
	/// <inheritdoc />
	public OAuthProvider Provider => OAuthProvider.GitHub;

	/// <inheritdoc />
	public string Scheme => GitHubAuthenticationDefaults.AuthenticationScheme;

	/// <inheritdoc />
	public bool IsConfigured => HasEnv("GITHUB_CLIENT_ID") && HasEnv("GITHUB_CLIENT_SECRET");

	/// <summary>
	/// registruje github providera do ASP.NET Core authentication builderu
	/// </summary>
	/// <param name="builder">authentication builder pro registraci handleru</param>
	public static void ConfigureAuthentication(AuthenticationBuilder builder) {
		if (!Program.ENV.TryGetValue("GITHUB_CLIENT_ID", out var clientId) || string.IsNullOrWhiteSpace(clientId) ||
			!Program.ENV.TryGetValue("GITHUB_CLIENT_SECRET", out var clientSecret) || string.IsNullOrWhiteSpace(clientSecret)) {
			return;
		}

		builder.AddGitHub(options => {
			options.ClientId = clientId;
			options.ClientSecret = clientSecret;
			options.CallbackPath = "/api/v1/github/callback";
			options.SignInScheme = "ExternalCookie";
			options.SaveTokens = true;
			options.Scope.Add("read:user");
			options.Events.OnRemoteFailure = OAuthEventsHelper.HandleRemoteFailure;
		});
	}

	/// <inheritdoc />
	public ExtractedOAuthProfile ExtractProfile(ClaimsPrincipal principal, AuthenticationProperties properties) {
		var providerUserId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";
		var username = principal.FindFirst(ClaimTypes.Name)?.Value ?? "GitHub účet";
		var avatarUrl = principal.FindFirst("urn:github:avatar")?.Value
			?? principal.FindFirst("urn:github:avatar_url")?.Value
			?? principal.FindFirst(ClaimTypes.Uri)?.Value;
		var profileUrl = principal.FindFirst("urn:github:url")?.Value
			?? principal.FindFirst("urn:github:html_url")?.Value
			?? $"https://github.com/{username}";

		return new ExtractedOAuthProfile(providerUserId, username, avatarUrl, profileUrl);
	}

	/// <inheritdoc />
	public Task<PlatformValidationResult> ValidateConnectionAsync(OAuthConnection connection, CancellationToken ct) =>
		Task.FromResult(new PlatformValidationResult(PlatformValidationStatus.Valid, connection.Username, connection.AvatarUrl, connection.ProfileUrl));

	/// <inheritdoc />
	public Task RevokeConnectionAsync(OAuthConnection connection, CancellationToken ct) => Task.CompletedTask;

	/// <summary>
	/// overuje pritomnost neprazdne promenne v konfiguraci prostredi
	/// </summary>
	/// <param name="key">nazev environment promenne</param>
	/// <returns>true pokud promenna existuje a neni prazdna</returns>
	private static bool HasEnv(string key) => Program.ENV.TryGetValue(key, out var val) && !string.IsNullOrWhiteSpace(val);
}
