using System.Text.Json.Serialization;
using server.Data.Entities;
using static server.Services.OAuth.ExternalAuthProviderBase.Models;
using static server.Services.OAuth.OAuthProviderBase.Models;
using static server.Services.OAuth.OAuthStateService.Models;

namespace server.Services.OAuth;

/// <summary>
/// implementuje google oauth flow a prevadi google userinfo response na normalizovany profil
/// </summary>
/// <param name="httpClientFactory">factory poskytujici http client pro google api</param>
/// <param name="logger">logger pouzity pro zaznam selhani google komunikace</param>
internal sealed class GoogleOAuthProvider(IHttpClientFactory httpClientFactory, ILogger<GoogleOAuthProvider> logger)
	: OAuthProviderBase(httpClientFactory, logger) {
	private const int AvatarSize = 512;

	/// <inheritdoc />
	internal override OAuthProvider Provider => OAuthProvider.Google;

	/// <inheritdoc />
	protected override string RouteSegment => "google";

	/// <inheritdoc />
	protected override ProviderConfig? GetConfig() => GetEnvironmentConfig(
		"GOOGLE",
		"https://accounts.google.com/o/oauth2/v2/auth",
		"https://oauth2.googleapis.com/token",
		"https://openidconnect.googleapis.com/v1/userinfo",
		"openid profile"
	);

	/// <inheritdoc />
	protected override Task<ProfileResult> GetProfileAsync(ProviderConfig config, TokenResponse tokens, StatePayload state, CancellationToken ct) =>
		string.IsNullOrWhiteSpace(tokens.AccessToken)
			? Task.FromResult(new ProfileResult(null, true))
			: GetBearerProfileAsync<GoogleUser>(config, tokens.AccessToken, ToProfile, ct);

	/// <summary>
	/// prevede google userinfo response na normalizovany oauth profil
	/// </summary>
	/// <param name="user">google userinfo response</param>
	/// <returns>normalizovany profil, nebo null pri chybejicim subject id</returns>
	private static Profile? ToProfile(GoogleUser? user) => user == null || string.IsNullOrWhiteSpace(user.Sub)
		? null
		: new Profile(user.Sub, string.IsNullOrWhiteSpace(user.Name) ? "Google účet" : user.Name, GetAvatarUrl(user.Picture), null);

	/// <summary>
	/// upravi google profilovou fotku na url pozadujici velikost 512 px
	/// </summary>
	/// <param name="pictureUrl">puvodni google picture url</param>
	/// <returns>upravena avatar url, nebo puvodni hodnota pri chybejici ci cizi url</returns>
	private static string? GetAvatarUrl(string? pictureUrl) {
		if (string.IsNullOrWhiteSpace(pictureUrl) || !Uri.TryCreate(pictureUrl, UriKind.Absolute, out var uri)) return pictureUrl;
		if (uri.Scheme != "https" || !uri.Host.EndsWith(".googleusercontent.com", StringComparison.OrdinalIgnoreCase)) return pictureUrl;

		var baseUrl = uri.GetLeftPart(UriPartial.Path);
		var parameterIndex = baseUrl.LastIndexOf('=');
		if (parameterIndex > baseUrl.LastIndexOf('/')) baseUrl = baseUrl[..parameterIndex];
		return $"{baseUrl}=s{AvatarSize}-c";
	}

	private sealed class GoogleUser {
		[JsonPropertyName("sub")]
		public string? Sub { get; init; }

		[JsonPropertyName("name")]
		public string? Name { get; init; }

		[JsonPropertyName("picture")]
		public string? Picture { get; init; }
	}
}