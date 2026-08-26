using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using server.Data.Entities;
using static server.Services.OAuth.ExternalAuthProviderBase.Models;
using static server.Services.OAuth.OAuthProviderBase.Models;
using static server.Services.OAuth.OAuthStateService.Models;

namespace server.Services.OAuth;

/// <summary>
/// implementuje sign in with apple flow a vytvari profil z overeneho podepsaneho id tokenu
/// </summary>
/// <param name="httpClientFactory">factory poskytujici http client pro apple public key endpoint</param>
/// <param name="logger">logger pouzity pro zaznam selhani apple token validace</param>
internal sealed class AppleOAuthProvider(IHttpClientFactory httpClientFactory, ILogger<AppleOAuthProvider> logger)
	: OAuthProviderBase(httpClientFactory, logger) {
	private const string AppleIssuer = "https://appleid.apple.com";
	private const string AppleJwksEndpoint = "https://appleid.apple.com/auth/keys";

	/// <inheritdoc />
	internal override OAuthProvider Provider => OAuthProvider.Apple;

	/// <inheritdoc />
	protected override string RouteSegment => "apple";

	/// <inheritdoc />
	internal override bool RequiresHttps => true;

	/// <inheritdoc />
	protected override bool UsesPkce => false;

	/// <inheritdoc />
	internal override Uri? CreateAuthorizationUri(AuthorizationContext context) {
		var config = GetConfig();
		if (config == null) return null;
		return new Uri(QueryHelpers.AddQueryString(config.AuthorizationEndpoint, new Dictionary<string, string?> {
			["client_id"] = config.ClientId,
			["response_type"] = "code",
			["response_mode"] = "query",
			["redirect_uri"] = context.CallbackUri,
			["state"] = context.State,
			["nonce"] = context.Nonce,
		}));
	}

	/// <inheritdoc />
	protected override ProviderConfig? GetConfig() {
		// apple zustava vypnuty, dokud nebude hotove nastaveni v apple developer portalu
		/*
		// sign in with apple neposila profilovku, proto avatar zustava null a synchronizace neni dostupna
		if (!Program.ENV.TryGetValue("APPLE_CLIENT_ID", out var clientId) || string.IsNullOrWhiteSpace(clientId)) return null;
		var clientSecret = CreateClientSecret(clientId);
		return clientSecret == null
			? null
			: new ProviderConfig(clientId, clientSecret, "https://appleid.apple.com/auth/authorize", "https://appleid.apple.com/auth/token", "", "");
		*/
		return null;
	}

	/// <summary>
	/// overi apple id token pomoci aktualnich public keys a prevede jeho claims na normalizovany profil
	/// </summary>
	/// <param name="config">apple konfigurace obsahujici service id pouzite jako audience</param>
	/// <param name="tokens">token response obsahujici podepsany id token</param>
	/// <param name="state">overeny oauth state obsahujici puvodni nonce</param>
	/// <param name="ct">token pro zruseni asynchronni operace</param>
	/// <returns>normalizovany apple profil nebo informace o neplatnem tokenu</returns>
	protected override async Task<ProfileResult> GetProfileAsync(ProviderConfig config, TokenResponse tokens, StatePayload state, CancellationToken ct) {
		if (tokens.IdToken is not { Length: > 0 } idToken) return new ProfileResult(null, true);

		try {
			using var request = new HttpRequestMessage(HttpMethod.Get, AppleJwksEndpoint);
			request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
			using var response = await HttpClient.SendAsync(request, ct);
			if (!response.IsSuccessStatusCode) return new ProfileResult(null, false);

			var keySet = new JsonWebKeySet(await response.Content.ReadAsStringAsync(ct));
			var validation = await new JsonWebTokenHandler().ValidateTokenAsync(idToken, new TokenValidationParameters {
				ValidateIssuerSigningKey = true,
				IssuerSigningKeys = keySet.Keys,
				ValidAlgorithms = [SecurityAlgorithms.RsaSha256],
				ValidateIssuer = true,
				ValidIssuer = AppleIssuer,
				ValidateAudience = true,
				ValidAudience = config.ClientId,
				ValidateLifetime = true,
				RequireExpirationTime = true,
				RequireSignedTokens = true,
				ClockSkew = TimeSpan.FromMinutes(2),
			});
			if (!validation.IsValid) {
				logger.LogWarning(validation.Exception, "Apple identity token validation failed");
				return new ProfileResult(null, true);
			}

			var userId = validation.Claims.TryGetValue("sub", out var subject) ? subject?.ToString() : null;
			var nonce = validation.Claims.TryGetValue("nonce", out var nonceClaim) ? nonceClaim?.ToString() : null;
			if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(nonce)) return new ProfileResult(null, true);
			if (!CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(nonce), Encoding.UTF8.GetBytes(state.Nonce))) return new ProfileResult(null, true);

			return new ProfileResult(new Profile(userId, "Apple účet", null, null), false);
		} catch (HttpRequestException exception) {
			logger.LogWarning(exception, "Apple public key request failed");
			return new ProfileResult(null, false);
		} catch (JsonException exception) {
			logger.LogWarning(exception, "Apple public key response was invalid");
			return new ProfileResult(null, false);
		} catch (ArgumentException exception) {
			logger.LogWarning(exception, "Apple public key response was invalid");
			return new ProfileResult(null, false);
		} catch (SecurityTokenException exception) {
			logger.LogWarning(exception, "Apple identity token was invalid");
			return new ProfileResult(null, true);
		} catch (OperationCanceledException exception) when (!ct.IsCancellationRequested) {
			logger.LogWarning(exception, "Apple identity token validation timed out");
			return new ProfileResult(null, false);
		}
	}

	/// <summary>
	/// vytvori kratkodoby podepsany apple client secret z developer credentials
	/// </summary>
	/// <param name="clientId">apple service id pouzite jako subject tokenu</param>
	/// <returns>podepsany client secret, nebo null pri chybejici ci neplatne konfiguraci</returns>
	private static string? CreateClientSecret(string clientId) {
		if (!Program.ENV.TryGetValue("APPLE_TEAM_ID", out var teamId) || string.IsNullOrWhiteSpace(teamId)) return null;
		if (!Program.ENV.TryGetValue("APPLE_KEY_ID", out var keyId) || string.IsNullOrWhiteSpace(keyId)) return null;
		if (!Program.ENV.TryGetValue("APPLE_PRIVATE_KEY_BASE64", out var encodedPrivateKey) || string.IsNullOrWhiteSpace(encodedPrivateKey)) return null;

		try {
			var privateKey = Encoding.UTF8.GetString(Convert.FromBase64String(encodedPrivateKey));
			using var ecdsa = ECDsa.Create();
			ecdsa.ImportFromPem(privateKey);
			var now = DateTime.UtcNow;
			return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor {
				Issuer = teamId,
				Audience = AppleIssuer,
				Claims = new Dictionary<string, object> { ["sub"] = clientId },
				IssuedAt = now,
				Expires = now.AddMinutes(5),
				SigningCredentials = new SigningCredentials(new ECDsaSecurityKey(ecdsa) { KeyId = keyId }, SecurityAlgorithms.EcdsaSha256),
			});
		} catch (FormatException) {
			return null;
		} catch (CryptographicException) {
			return null;
		} catch (ArgumentException) {
			return null;
		}
	}
}