using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.WebUtilities;
using server.Data.Entities;
using AuthorizationContext = server.Services.OAuth.ExternalAuthProviderBase.Models.AuthorizationContext;
using AuthorizationResult = server.Services.OAuth.ExternalAuthProviderBase.Models.AuthorizationResult;
using AuthorizationStatus = server.Services.OAuth.ExternalAuthProviderBase.Models.AuthorizationStatus;
using Profile = server.Services.OAuth.ExternalAuthProviderBase.Models.Profile;
using StatePayload = server.Services.OAuth.OAuthStateService.Models.StatePayload;

namespace server.Services.OAuth;

/// <summary>
/// implementuje spolecny oauth flow pro providery s authorization, token a profile endpointem
/// </summary>
/// <param name="httpClientFactory">factory poskytujici sdileny http client pro provider requesty</param>
/// <param name="logger">logger pouzity pro zaznam selhani externi komunikace</param>
internal abstract class OAuthProviderBase(IHttpClientFactory httpClientFactory, ILogger logger) : ExternalAuthProviderBase {
	/// <summary>
	/// vraci enum identifikujici konkretniho providera
	/// </summary>
	internal abstract override OAuthProvider Provider { get; }

	/// <summary>
	/// vraci route segment konkretniho provider callbacku
	/// </summary>
	protected abstract override string RouteSegment { get; }

	/// <summary>
	/// urcuje, zda provider vyzaduje https frontend origin
	/// </summary>
	internal override bool RequiresHttps => false;

	/// <summary>
	/// urcuje, zda lze z prostredi sestavit platnou konfiguraci providera
	/// </summary>
	internal override bool IsConfigured => GetConfig() != null;

	/// <summary>
	/// vytvori sdileny http client pro externi oauth request
	/// </summary>
	protected HttpClient HttpClient => httpClientFactory.CreateClient(HttpClientName);

	/// <summary>
	/// nacte provider-specific client credentials, endpointy a scope
	/// </summary>
	/// <returns>konfiguraci providera, nebo null pri chybejicich povinnych hodnotach</returns>
	protected abstract Models.ProviderConfig? GetConfig();

	/// <summary>
	/// nacte a normalizuje profil uzivatele pomoci token response konkretniho providera
	/// </summary>
	/// <param name="config">aktivni konfigurace providera</param>
	/// <param name="tokens">token response z uspesne authorization code vymeny</param>
	/// <param name="state">overeny state pouzity pro provider-specific kontrolu identity</param>
	/// <param name="ct">token pro zruseni asynchronni operace</param>
	/// <returns>normalizovany profil nebo informace o neplatnem tokenu</returns>
	protected abstract Task<Models.ProfileResult> GetProfileAsync(Models.ProviderConfig config, Models.TokenResponse tokens, StatePayload state, CancellationToken ct);

	/// <summary>
	/// urcuje, zda authorization a token request pouziva pkce
	/// </summary>
	protected virtual bool UsesPkce => true;

	/// <summary>
	/// urcuje, zda token endpoint prijima client credentials pres basic authorization hlavicku
	/// </summary>
	protected virtual bool UsesBasicClientAuthentication => false;

	/// <summary>
	/// urcuje, zda uspesny callback rovnou oznaci ulozene spojeni jako validovane
	/// </summary>
	protected virtual bool MarksConnectionValidated => false;

	/// <summary>
	/// sestavi backend callback url pro konkretni route segment
	/// </summary>
	/// <param name="frontendOrigin">overeny frontend origin pouzity jako zaklad url</param>
	/// <param name="state">state hodnota dostupna provider-specific implementaci</param>
	/// <returns>absolutni callback url prijimanou oauth controllerem</returns>
	internal override string BuildCallbackUri(string frontendOrigin, string state) => $"{frontendOrigin}/api/v1/{RouteSegment}/callback";

	/// <summary>
	/// sestavi standardni authorization url vcetne callbacku, scope, state a volitelneho pkce challenge
	/// </summary>
	/// <param name="context">overeny kontext noveho oauth flow</param>
	/// <returns>authorization url, nebo null pri chybejici konfiguraci providera</returns>
	internal override Uri? CreateAuthorizationUri(AuthorizationContext context) {
		var config = GetConfig();
		if (config == null) return null;

		var query = new Dictionary<string, string?> {
			["client_id"] = config.ClientId,
			["response_type"] = "code",
			["redirect_uri"] = context.CallbackUri,
			["state"] = context.State,
			["scope"] = config.Scope,
		};
		if (UsesPkce) {
			query["code_challenge"] = context.CodeChallenge;
			query["code_challenge_method"] = "S256";
		}

		return new Uri(QueryHelpers.AddQueryString(config.AuthorizationEndpoint, query));
	}

	/// <summary>
	/// vymeni authorization code za tokeny a nacte normalizovany profil prihlaseneho uzivatele
	/// </summary>
	/// <param name="request">callback request dostupny provider-specific implementaci</param>
	/// <param name="state">drive ulozeny a overeny oauth state</param>
	/// <param name="code">authorization code vraceny providerem</param>
	/// <param name="error">chyba nebo zruseni vracene providerem</param>
	/// <param name="ct">token pro zruseni asynchronni operace</param>
	/// <returns>normalizovany vysledek provider autentizace</returns>
	internal override async Task<AuthorizationResult> CompleteAuthorizationAsync(HttpRequest request, StatePayload state, string? code, string? error, CancellationToken ct) {
		if (!string.IsNullOrWhiteSpace(error) || string.IsNullOrWhiteSpace(code)) {
			return new AuthorizationResult(AuthorizationStatus.Cancelled);
		}

		var config = GetConfig();
		if (config == null) return new AuthorizationResult(AuthorizationStatus.Failed);

		var tokenContent = new Dictionary<string, string> {
			["grant_type"] = "authorization_code",
			["code"] = code,
			["redirect_uri"] = state.CallbackUri,
		};
		if (UsesPkce) tokenContent["code_verifier"] = state.CodeVerifier;

		var tokenResult = await RequestTokenAsync(config, tokenContent, ct);
		if (tokenResult.Tokens == null) return new AuthorizationResult(AuthorizationStatus.Failed);

		var profileResult = await GetProfileAsync(config, tokenResult.Tokens, state, ct);
		if (profileResult.Profile == null) return new AuthorizationResult(AuthorizationStatus.Failed);

		return new AuthorizationResult(
			AuthorizationStatus.Succeeded,
			profileResult.Profile.UserId,
			profileResult.Profile,
			tokenResult.Tokens,
			MarksConnectionValidated
		);
	}

	/// <summary>
	/// odesle token request se zvolenym zpusobem client autentizace a namapuje token response
	/// </summary>
	/// <param name="config">konfigurace token endpointu a client credentials</param>
	/// <param name="content">form data konkretniho token nebo refresh requestu</param>
	/// <param name="ct">token pro zruseni asynchronni operace</param>
	/// <returns>token response a informaci, zda provider potvrdil neplatny request</returns>
	protected async Task<Models.TokenResult> RequestTokenAsync(Models.ProviderConfig config, Dictionary<string, string> content, CancellationToken ct) {
		using var request = new HttpRequestMessage(HttpMethod.Post, config.TokenEndpoint);
		request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

		if (UsesBasicClientAuthentication) {
			request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{config.ClientId}:{config.ClientSecret}")));
		} else {
			content["client_id"] = config.ClientId;
			content["client_secret"] = config.ClientSecret;
		}
		request.Content = new FormUrlEncodedContent(content);

		try {
			using var response = await HttpClient.SendAsync(request, ct);
			if (!response.IsSuccessStatusCode) {
				return new Models.TokenResult(null, response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden);
			}
			return new Models.TokenResult(await response.Content.ReadFromJsonAsync<Models.TokenResponse>(cancellationToken: ct), false);
		} catch (HttpRequestException exception) {
			logger.LogWarning(exception, "{Provider} token request failed", Provider);
			return new Models.TokenResult(null, false);
		}
	}

	/// <summary>
	/// nacte json profil pres bearer token a prevede ho na spolecny profil pomoci dodaneho mapperu
	/// </summary>
	/// <typeparam name="TUser">provider-specific typ json profilu</typeparam>
	/// <param name="config">konfigurace obsahujici profile endpoint</param>
	/// <param name="accessToken">bearer token pro autorizaci requestu</param>
	/// <param name="mapper">funkce prevadejici provider response na normalizovany profil</param>
	/// <param name="ct">token pro zruseni asynchronni operace</param>
	/// <returns>normalizovany profil a informaci, zda endpoint odmitl token</returns>
	protected async Task<Models.ProfileResult> GetBearerProfileAsync<TUser>(Models.ProviderConfig config, string accessToken, Func<TUser?, Profile?> mapper, CancellationToken ct) {
		using var request = new HttpRequestMessage(HttpMethod.Get, config.UserEndpoint);
		request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
		request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

		try {
			using var response = await HttpClient.SendAsync(request, ct);
			if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden) return new Models.ProfileResult(null, true);
			if (!response.IsSuccessStatusCode) return new Models.ProfileResult(null, false);
			return new Models.ProfileResult(mapper(await response.Content.ReadFromJsonAsync<TUser>(cancellationToken: ct)), false);
		} catch (HttpRequestException exception) {
			logger.LogWarning(exception, "{Provider} profile request failed", Provider);
			return new Models.ProfileResult(null, false);
		}
	}

	/// <summary>
	/// nacte standardni oauth client konfiguraci z environment hodnot se zadanym prefixem
	/// </summary>
	/// <param name="prefix">prefix environment promennych client id a client secret</param>
	/// <param name="authorizationEndpoint">authorization endpoint providera</param>
	/// <param name="tokenEndpoint">token endpoint providera</param>
	/// <param name="userEndpoint">profile endpoint providera</param>
	/// <param name="scope">scope pozadovany authorization flow</param>
	/// <returns>kompletni konfiguraci, nebo null pri chybejicim client id ci client secret</returns>
	protected static Models.ProviderConfig? GetEnvironmentConfig(
		string prefix,
		string authorizationEndpoint,
		string tokenEndpoint,
		string userEndpoint,
		string scope
	) {
		if (!Program.ENV.TryGetValue($"{prefix}_CLIENT_ID", out var clientId) || string.IsNullOrWhiteSpace(clientId)) return null;
		if (!Program.ENV.TryGetValue($"{prefix}_CLIENT_SECRET", out var clientSecret) || string.IsNullOrWhiteSpace(clientSecret)) return null;
		return new Models.ProviderConfig(clientId, clientSecret, authorizationEndpoint, tokenEndpoint, userEndpoint, scope);
	}

	/// <summary>
	/// sdruzuje modely pouzivane standardnim oauth authorization code flow
	/// </summary>
	internal new static class Models {
		/// <summary>
		/// sdruzuje konfiguraci potrebnou pro standardni oauth authorization code flow
		/// </summary>
		/// <param name="ClientId">verejny identifikator oauth aplikace</param>
		/// <param name="ClientSecret">tajny klic oauth aplikace</param>
		/// <param name="AuthorizationEndpoint">endpoint pro zahajeni autorizace v prohlizeci</param>
		/// <param name="TokenEndpoint">endpoint pro vymenu code nebo refresh tokenu</param>
		/// <param name="UserEndpoint">endpoint pro nacteni profilu prihlaseneho uzivatele</param>
		/// <param name="Scope">mezernikem oddeleny seznam pozadovanych opravneni</param>
		internal sealed record ProviderConfig(
			string ClientId,
			string ClientSecret,
			string AuthorizationEndpoint,
			string TokenEndpoint,
			string UserEndpoint,
			string Scope
		);

		/// <summary>
		/// prenasi vysledek token requestu a rozlisuje neplatne credentials od docasne chyby
		/// </summary>
		/// <param name="Tokens">token response pri uspesnem requestu</param>
		/// <param name="Invalid">urcuje, zda provider potvrdil neplatnost code nebo credentials</param>
		internal sealed record TokenResult(TokenResponse? Tokens, bool Invalid);

		/// <summary>
		/// prenasi vysledek nacteni externiho profilu a informaci o neplatnem tokenu
		/// </summary>
		/// <param name="Profile">normalizovany profil pri uspesnem requestu</param>
		/// <param name="TokenInvalid">urcuje, zda profilovy endpoint odmitl access token</param>
		internal sealed record ProfileResult(Profile? Profile, bool TokenInvalid);

		/// <summary>
		/// mapuje standardni json token response vracenou oauth providerem
		/// </summary>
		internal sealed class TokenResponse {
			/// <summary>
			/// vraci kratkodoby token pro volani provider api
			/// </summary>
			[JsonPropertyName("access_token")]
			public string? AccessToken { get; init; }

			/// <summary>
			/// vraci dlouhodoby token pro obnovu access tokenu
			/// </summary>
			[JsonPropertyName("refresh_token")]
			public string? RefreshToken { get; init; }

			/// <summary>
			/// vraci pocet sekund do expirace access tokenu
			/// </summary>
			[JsonPropertyName("expires_in")]
			public int? ExpiresIn { get; init; }

			/// <summary>
			/// vraci podepsany identity token, pokud ho provider podporuje
			/// </summary>
			[JsonPropertyName("id_token")]
			public string? IdToken { get; init; }
		}
	}
}