using System.Text.Json.Serialization;
using server.Data.Entities;
using static server.Services.OAuth.ExternalAuthProviderBase.Models;
using static server.Services.OAuth.OAuthProviderBase.Models;
using static server.Services.OAuth.OAuthStateService.Models;

namespace server.Services.OAuth;

/// <summary>
/// implementuje github oauth flow a prevadi github user response na normalizovany profil
/// </summary>
/// <param name="httpClientFactory">factory poskytujici http client pro github api</param>
/// <param name="logger">logger pouzity pro zaznam selhani github komunikace</param>
internal sealed class GitHubOAuthProvider(IHttpClientFactory httpClientFactory, ILogger<GitHubOAuthProvider> logger)
	: OAuthProviderBase(httpClientFactory, logger) {
	/// <inheritdoc />
	internal override OAuthProvider Provider => OAuthProvider.GitHub;

	/// <inheritdoc />
	protected override string RouteSegment => "github";

	/// <inheritdoc />
	protected override ProviderConfig? GetConfig() => GetEnvironmentConfig(
		"GITHUB",
		"https://github.com/login/oauth/authorize",
		"https://github.com/login/oauth/access_token",
		"https://api.github.com/user",
		"read:user"
	);

	/// <inheritdoc />
	protected override Task<ProfileResult> GetProfileAsync(ProviderConfig config, TokenResponse tokens, StatePayload state, CancellationToken ct) =>
		string.IsNullOrWhiteSpace(tokens.AccessToken)
			? Task.FromResult(new ProfileResult(null, true))
			: GetBearerProfileAsync<GitHubUser>(config, tokens.AccessToken, ToProfile, ct);

	/// <summary>
	/// prevede github user response na normalizovany oauth profil
	/// </summary>
	/// <param name="user">github user response</param>
	/// <returns>normalizovany profil, nebo null pri chybejicim loginu</returns>
	private static Profile? ToProfile(GitHubUser? user) => user == null || string.IsNullOrWhiteSpace(user.Login)
		? null
		: new Profile(user.Id.ToString(), user.Login, user.AvatarUrl, user.HtmlUrl);

	private sealed class GitHubUser {
		[JsonPropertyName("id")]
		public long Id { get; init; }

		[JsonPropertyName("login")]
		public string? Login { get; init; }

		[JsonPropertyName("avatar_url")]
		public string? AvatarUrl { get; init; }

		[JsonPropertyName("html_url")]
		public string? HtmlUrl { get; init; }
	}
}