using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using server.Data.Entities;

namespace server.Services.OAuth;

/// <summary>
/// sjednocuje kontrakt pro konkretni externi autentizacni platformu
/// </summary>
public interface IOAuthPlatform {
	/// <summary>
	/// typ providera
	/// </summary>
	OAuthProvider Provider { get; }

	/// <summary>
	/// nazev ASP.NET Core authentication scheme
	/// </summary>
	string Scheme { get; }

	/// <summary>
	/// urcuje, zda je platforma v prostredi plne nakonfigurovana
	/// </summary>
	bool IsConfigured { get; }

	/// <summary>
	/// extrahuje normalizovana data profilu a tokeny z overeneho principalu a properties
	/// </summary>
	ExtractedOAuthProfile ExtractProfile(ClaimsPrincipal principal, AuthenticationProperties properties);

	/// <summary>
	/// provede validaci nebo obnovu tokenu a profilu u existujiciho spojeni
	/// </summary>
	Task<PlatformValidationResult> ValidateConnectionAsync(OAuthConnection connection, CancellationToken ct);

	/// <summary>
	/// zrusi token u providera pri odpojeni uctu
	/// </summary>
	Task RevokeConnectionAsync(OAuthConnection connection, CancellationToken ct);
}

/// <summary>
/// normalizovana data profilu ziskana z externi platformy
/// </summary>
public sealed record ExtractedOAuthProfile(
	string UserId,
	string Username,
	string? AvatarUrl,
	string? ProfileUrl,
	string? AccessToken = null,
	string? RefreshToken = null,
	DateTime? ExpiresAtUtc = null
);

/// <summary>
/// stav vysledku validace spojeni
/// </summary>
public enum PlatformValidationStatus {
	Valid,
	Invalid,
	Unavailable,
}

/// <summary>
/// vysledek periodicke validace nebo obnovy ulozeneho spojeni
/// </summary>
public sealed record PlatformValidationResult(
	PlatformValidationStatus Status,
	string? Username = null,
	string? AvatarUrl = null,
	string? ProfileUrl = null
);

public static class OAuthServiceCollectionExtensions {
	public static IServiceCollection AddOAuthPlatforms(this IServiceCollection services, AuthenticationBuilder authBuilder) {
		services.AddScoped<IOAuthPlatform, Platforms.DiscordOAuthPlatform>();
		services.AddScoped<IOAuthPlatform, Platforms.GoogleOAuthPlatform>();
		services.AddScoped<IOAuthPlatform, Platforms.GitHubOAuthPlatform>();
		services.AddScoped<IOAuthPlatform, Platforms.SteamOAuthPlatform>();
		services.AddScoped<IOAuthPlatform, Platforms.AppleOAuthPlatform>();
		services.AddScoped<IOAuthService, OAuthService>();

		Platforms.DiscordOAuthPlatform.ConfigureAuthentication(authBuilder);
		Platforms.GoogleOAuthPlatform.ConfigureAuthentication(authBuilder);
		Platforms.GitHubOAuthPlatform.ConfigureAuthentication(authBuilder);
		Platforms.SteamOAuthPlatform.ConfigureAuthentication(authBuilder);
		Platforms.AppleOAuthPlatform.ConfigureAuthentication(authBuilder);

		return services;
	}
}