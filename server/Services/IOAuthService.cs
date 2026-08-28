using server.Data.Entities;

namespace server.Services;

/// <summary>
/// verejny oauth service kontrakt oddeluje controllery a autentizaci od protokolu jednotlivych provideru
/// sjednocuje zahajeni flow, zpracovani callbacku, kontrolu spojeni a synchronizaci profilove fotky
/// </summary>
public interface IOAuthService {
	/// <summary>
	/// overuje, zda ma dany provider nastavenou vsechnu potrebnou konfiguraci v prostredi
	/// </summary>
	/// <param name="provider">provider, jehoz konfigurace se overuje</param>
	/// <returns>true pokud je provider pripraven k pouziti, jinak false</returns>
	bool IsProviderConfigured(OAuthProvider provider);

	/// <summary>
	/// vraci nazev registrovaneho ASP.NET Core authentication scheme pro daneho providera
	/// </summary>
	/// <param name="provider">provider, pro ktereho se hleda scheme</param>
	/// <returns>nazev authentication scheme</returns>
	string GetAuthenticationScheme(OAuthProvider provider);

	/// <summary>
	/// nacte a normalizuje povoleny frontend origin z konfigurace aplikace
	/// </summary>
	/// <returns>schema a authority povoleneho frontend originu, nebo null pri neplatne konfiguraci</returns>
	string? GetFrontendOrigin();

	/// <summary>
	/// zpracuje vysledek externi autentizace po navratu z middleware a provede login nebo connect
	/// </summary>
	/// <param name="authResult">vysledek autentizace z docasneho cookies schematu</param>
	/// <param name="provider">provider, pres ktereho autentizace probehla</param>
	/// <param name="ct">token pro zruseni asynchronni operace</param>
	/// <returns>normalizovany vysledek flow</returns>
	Task<OAuthCompletion> CompleteExternalAuthAsync(Microsoft.AspNetCore.Authentication.AuthenticateResult authResult, OAuthProvider provider, CancellationToken ct = default);

	/// <summary>
	/// zrusi externi token, pokud to provider podporuje, a potom odstrani lokalni spojeni s uctem
	/// </summary>
	/// <param name="accountId">id uctu, od ktereho se platforma odpojuje</param>
	/// <param name="provider">provider odpojovane platformy</param>
	/// <param name="ct">token pro zruseni asynchronni operace</param>
	/// <returns>aktualizovany ucet, nebo null pokud ucet neexistuje</returns>
	Task<Account?> DisconnectAsync(Guid accountId, OAuthProvider provider, CancellationToken ct = default);

	/// <summary>
	/// podle potreby overi discord spojeni, obnovi expirovany access token a aktualizuje ulozeny profil
	/// </summary>
	/// <param name="accountId">id uctu s kontrolovanym discord spojenim</param>
	/// <param name="forceValidation">urcuje, zda se ma obejit casovy interval mezi kontrolami</param>
	/// <param name="ct">token pro zruseni asynchronni operace</param>
	/// <returns>ukol reprezentujici dokonceni kontroly spojeni</returns>
	Task EnsureDiscordConnectionAsync(Guid accountId, bool forceValidation, CancellationToken ct = default);

	/// <summary>
	/// podle potreby overi dostupnost propojeneho steam profilu a aktualizuje jeho verejna data
	/// </summary>
	/// <param name="accountId">id uctu s kontrolovanym steam spojenim</param>
	/// <param name="forceValidation">urcuje, zda se ma obejit casovy interval mezi kontrolami</param>
	/// <param name="ct">token pro zruseni asynchronni operace</param>
	/// <returns>ukol reprezentujici dokonceni kontroly spojeni</returns>
	Task EnsureSteamConnectionAsync(Guid accountId, bool forceValidation, CancellationToken ct = default);

	/// <summary>
	/// nastavi platformu pro synchronizaci profilove fotky a pri discordu nejdriv obnovi aktualni profil
	/// </summary>
	/// <param name="accountId">id uctu, kteremu se meni zdroj profilove fotky</param>
	/// <param name="platform">vybrany provider, nebo null pro vypnuti synchronizace</param>
	/// <param name="ct">token pro zruseni asynchronni operace</param>
	/// <returns>aktualizovany ucet, nebo null pri neexistujicim uctu ci nepodporovane platforme</returns>
	Task<Account?> SetAvatarSyncPlatformAsync(Guid accountId, OAuthProvider? platform, CancellationToken ct = default);
}

/// <summary>
/// urcuje ucel oauth flow a tim i zpusob zpracovani uspesneho callbacku
/// </summary>
public enum OAuthFlow {
	/// <summary>
	/// vyhleda existujici ucet podle uz propojene externi identity
	/// </summary>
	Login,

	/// <summary>
	/// pripoji externi identitu k uz prihlasenemu uctu
	/// </summary>
	Connect,
}

/// <summary>
/// sjednocuje vysledky rozdilnych provideru do stavu, kterym rozumi oauth controller
/// </summary>
public enum OAuthCompletionKind {
	/// <summary>
	/// oznacuje chybejici, expirovany nebo neshodny oauth state
	/// </summary>
	InvalidState,

	/// <summary>
	/// oznacuje flow zrusene uzivatelem nebo providerem
	/// </summary>
	Cancelled,

	/// <summary>
	/// oznacuje technicke selhani pri overeni externi identity
	/// </summary>
	Failed,

	/// <summary>
	/// oznacuje platnou externi identitu bez propojeneho lokalniho uctu
	/// </summary>
	LoginNotLinked,

	/// <summary>
	/// oznacuje externi identitu, ktera uz patri jinemu lokalnimu uctu
	/// </summary>
	AlreadyLinked,

	/// <summary>
	/// oznacuje uspesne prihlaseni pres uz propojenou externi identitu
	/// </summary>
	LoginSucceeded,

	/// <summary>
	/// oznacuje uspesne propojeni externi identity s prihlasenym uctem
	/// </summary>
	Connected,
}

/// <summary>
/// prenasi vysledek dokonceneho flow, overeny navratovy origin a pripadne id prihlaseneho uctu
/// </summary>
/// <param name="Kind">normalizovany stav dokonceni flow</param>
/// <param name="ReturnOrigin">frontend origin nacteny z drive overeneho a jednorazoveho state</param>
/// <param name="AccountId">id uctu pouzite pri uspesnem prihlaseni nebo propojeni</param>
/// <param name="Flow">typ flow nacteny z overeneho state</param>
public sealed record OAuthCompletion(OAuthCompletionKind Kind, string? ReturnOrigin = null, Guid? AccountId = null, OAuthFlow? Flow = null);