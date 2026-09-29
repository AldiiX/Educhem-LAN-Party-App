using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth.Claims;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using server.Data.Entities;
using server.Infrastructure;

namespace server.Services.OAuth.Platforms;

/// <summary>
/// integrace centralniho Netuvio Identity providera pres OpenID Connect
/// </summary>
internal sealed class NetuvioOAuthPlatform : IOAuthPlatform {
	private const string AuthenticationScheme = "Netuvio";

	/// <inheritdoc />
	public OAuthProvider Provider => OAuthProvider.Netuvio;

	/// <inheritdoc />
	public string Scheme => AuthenticationScheme;

	/// <inheritdoc />
	public bool IsConfigured => TryGetConfiguration(out _, out _, out _);

	/// <summary>
	/// registruje authorization-code flow s PKCE proti Netuvio Identity
	/// </summary>
	public static void ConfigureAuthentication(AuthenticationBuilder builder) {
		if (!TryGetConfiguration(out var authority, out var clientId, out var clientSecret)) return;

		builder.AddOpenIdConnect(AuthenticationScheme, "Netuvio", options => {
			options.Authority = authority;
			options.ClientId = clientId;
			options.ClientSecret = clientSecret;
			options.CallbackPath = "/api/v1/netuvio/callback";
			options.SignInScheme = AuthSchemes.ExternalCookie;
			options.ResponseType = OpenIdConnectResponseType.Code;
			options.UsePkce = true;
			options.SaveTokens = true;
			options.GetClaimsFromUserInfoEndpoint = true;
			options.RequireHttpsMetadata = authority.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
			options.Scope.Clear();
			options.Scope.Add("openid");
			options.Scope.Add("profile");
			options.Scope.Add("email");
			options.ClaimActions.MapUniqueJsonKey("picture", "picture");
			options.TokenValidationParameters = new TokenValidationParameters {
				NameClaimType = "name",
			};
			options.Events.OnRedirectToIdentityProvider = context => {
				context.ProtocolMessage.RedirectUri = FrontendUrl.BuildAbsolute(options.CallbackPath.Value!);
				return Task.CompletedTask;
			};
			options.Events.OnRemoteFailure = OAuthEventsHelper.HandleRemoteFailure;
		});
	}

	/// <inheritdoc />
	public ExtractedOAuthProfile ExtractProfile(ClaimsPrincipal principal, AuthenticationProperties properties) {
		var providerUserId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
			?? principal.FindFirst("sub")?.Value
			?? "";
		var username = principal.FindFirst("name")?.Value
			?? principal.FindFirst(ClaimTypes.Name)?.Value
			?? principal.FindFirst("preferred_username")?.Value
			?? "Netuvio účet";
		var avatarUrl = principal.FindFirst("picture")?.Value;

		return new ExtractedOAuthProfile(providerUserId, username, avatarUrl, null);
	}

	/// <inheritdoc />
	public Task<PlatformValidationResult> ValidateConnectionAsync(OAuthConnection connection, CancellationToken ct) =>
		Task.FromResult(new PlatformValidationResult(PlatformValidationStatus.Valid, connection.Username, connection.AvatarUrl));

	/// <inheritdoc />
	public Task RevokeConnectionAsync(OAuthConnection connection, CancellationToken ct) => Task.CompletedTask;

	private static bool TryGetConfiguration(out string authority, out string clientId, out string clientSecret) {
		authority = GetEnv("NETUVIO_AUTHORITY");
		clientId = GetEnv("NETUVIO_CLIENT_ID");
		clientSecret = GetEnv("NETUVIO_CLIENT_SECRET");
		if (string.IsNullOrWhiteSpace(authority) || string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret)) return false;
		if (!Uri.TryCreate(authority, UriKind.Absolute, out var uri) || !string.IsNullOrEmpty(uri.UserInfo) ||
			!string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment)) return false;
		if (uri.Scheme != Uri.UriSchemeHttps && !(uri.Scheme == Uri.UriSchemeHttp && uri.IsLoopback)) return false;
		authority = uri.GetLeftPart(UriPartial.Authority);
		return true;
	}

	private static string GetEnv(string key) =>
		Program.ENV.TryGetValue(key, out var value) ? value.Trim() : "";
}
