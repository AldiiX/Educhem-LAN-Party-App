using System.Security.Claims;
using AspNet.Security.OAuth.Apple;
using Microsoft.AspNetCore.Authentication;
using server.Data.Entities;

namespace server.Services.OAuth.Platforms;

/// <summary>
/// implementuje integraci pro platformu Apple (Sign in with Apple)
/// </summary>
internal sealed class AppleOAuthPlatform : IOAuthPlatform {
	public OAuthProvider Provider => OAuthProvider.Apple;
	public string Scheme => AppleAuthenticationDefaults.AuthenticationScheme;
	public bool IsConfigured => HasEnv("APPLE_CLIENT_ID") && HasEnv("APPLE_TEAM_ID") && HasEnv("APPLE_KEY_ID") && (HasEnv("APPLE_PRIVATE_KEY") || HasEnv("APPLE_PRIVATE_KEY_BASE64"));

	public static void ConfigureAuthentication(AuthenticationBuilder builder) {
		if (!Program.ENV.TryGetValue("APPLE_CLIENT_ID", out var appleId) || string.IsNullOrWhiteSpace(appleId) ||
			!Program.ENV.TryGetValue("APPLE_TEAM_ID", out var appleTeamId) || string.IsNullOrWhiteSpace(appleTeamId) ||
			!Program.ENV.TryGetValue("APPLE_KEY_ID", out var appleKeyId) || string.IsNullOrWhiteSpace(appleKeyId)) {
			return;
		}

		builder.AddApple(options => {
			options.ClientId = appleId;
			options.TeamId = appleTeamId;
			options.KeyId = appleKeyId;
			options.CallbackPath = "/api/v1/apple/callback";
			options.SignInScheme = "ExternalCookie";
			options.SaveTokens = true;
			options.PrivateKey = (keyId, ct) => {
				if (Program.ENV.TryGetValue("APPLE_PRIVATE_KEY", out var pk) && !string.IsNullOrWhiteSpace(pk)) {
					return Task.FromResult(pk.AsMemory());
				}
				if (Program.ENV.TryGetValue("APPLE_PRIVATE_KEY_BASE64", out var pk64) && !string.IsNullOrWhiteSpace(pk64)) {
					var decoded = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(pk64));
					return Task.FromResult(decoded.AsMemory());
				}
				return Task.FromResult(ReadOnlyMemory<char>.Empty);
			};
			options.Events.OnRemoteFailure = OAuthEventsHelper.HandleRemoteFailure;
		});
	}

	public ExtractedOAuthProfile ExtractProfile(ClaimsPrincipal principal, AuthenticationProperties properties) {
		var providerUserId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";
		var username = principal.FindFirst(ClaimTypes.Name)?.Value ?? "Apple účet";

		return new ExtractedOAuthProfile(providerUserId, username, null, null);
	}

	public Task<PlatformValidationResult> ValidateConnectionAsync(OAuthConnection connection, CancellationToken ct) =>
		Task.FromResult(new PlatformValidationResult(PlatformValidationStatus.Valid, connection.Username, connection.AvatarUrl));

	public Task RevokeConnectionAsync(OAuthConnection connection, CancellationToken ct) => Task.CompletedTask;

	private static bool HasEnv(string key) => Program.ENV.TryGetValue(key, out var val) && !string.IsNullOrWhiteSpace(val);
}
