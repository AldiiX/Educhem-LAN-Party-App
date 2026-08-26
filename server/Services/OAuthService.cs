using Microsoft.EntityFrameworkCore;
using server.Data;
using server.Data.Entities;
using server.Services.OAuth;
using static server.Services.OAuth.ExternalAuthProviderBase.Models;
using static server.Services.OAuth.OAuthProviderBase.Models;
using static server.Services.OAuth.OAuthStateService.Models;

namespace server.Services;

/// <summary>
/// funguje jako oauth flow orchestrator mezi controllerem, bezpecnostnim state ulozistem, databazi a provider implementacemi
/// spolecna pravidla pro login, connect, disconnect a validaci drzi na jednom miste, zatimco provider-specific komunikaci deleguje konkretnim providerum
/// </summary>
/// <param name="db">databazovy context pro ucty a jejich oauth spojeni</param>
/// <param name="providerImplementations">kolekce implementaci registrovanych pro jednotlive providery</param>
/// <param name="stateService">service pro vytvareni, ulozeni a jednorazove overeni oauth state</param>
/// <param name="dbLogger">service pro zapis udalosti o propojeni a odpojeni platformy</param>
internal sealed class OAuthService(
    AppDbContext db,
    IEnumerable<ExternalAuthProviderBase> providerImplementations,
    OAuthStateService stateService,
    IDbLoggerService dbLogger
) : IOAuthService {
    private static readonly TimeSpan ValidationInterval = TimeSpan.FromMinutes(15);
    /// <summary>
    /// mapuje provider enum na odpovidajici implementaci a odstranuje potrebu velkeho provider switchu ve spolecnem flow
    /// </summary>
    private readonly IReadOnlyDictionary<OAuthProvider, ExternalAuthProviderBase> providers = providerImplementations.ToDictionary(provider => provider.Provider);

    /// <summary>
    /// overi dostupnost providera a frontend origin, vytvori jednorazovy bezpecnostni state a vrati authorization url
    /// </summary>
    /// <param name="accountId">id prihlaseneho uctu pro connect flow; pri login flow zustava null</param>
    /// <param name="provider">provider pouzity pro zahajeni flow</param>
    /// <param name="flow">login nebo connect varianta oauth flow</param>
    /// <param name="request">aktualni request pouzity pro ulozeni state cookie</param>
    /// <param name="ct">token pro zruseni asynchronni operace</param>
    /// <returns>authorization url, nebo null pokud flow nesplni konfiguracni ci bezpecnostni podminky</returns>
    public async Task<Uri?> CreateAuthorizationUrlAsync(Guid? accountId, OAuthProvider provider, OAuthFlow flow, HttpRequest request, CancellationToken ct = default) {
        if (!providers.TryGetValue(provider, out var providerImplementation) || !providerImplementation.IsConfigured) return null;
        if (flow == OAuthFlow.Connect && accountId == null) return null;

        // provider s pozadavkem na https se nespusti nad nezabezpecenym frontend originem
        var frontendOrigin = stateService.GetFrontendOrigin();
        if (frontendOrigin == null || (providerImplementation.RequiresHttps && !OAuthStateService.IsHttps(frontendOrigin))) return null;

        var parameters = OAuthStateService.CreateParameters();
        var callbackUri = providerImplementation.BuildCallbackUri(frontendOrigin, parameters.State);
        var payload = new StatePayload(accountId, provider, flow, callbackUri, frontendOrigin, parameters.CodeVerifier, parameters.Nonce);
        await stateService.StoreAsync(request, parameters.State, payload, ct);

        return providerImplementation.CreateAuthorizationUri(new AuthorizationContext(
            callbackUri,
            frontendOrigin,
            parameters.State,
            parameters.CodeVerifier,
            parameters.CodeChallenge,
            parameters.Nonce
        ));
    }

    /// <summary>
    /// spotrebuje a overi state, necha provider zpracovat callback a potom dokonci login nebo connect flow
    /// </summary>
    /// <param name="request">callback request s provider-specific query parametry</param>
    /// <param name="provider">provider, jehoz implementace callback zpracuje</param>
    /// <param name="state">state vraceny providerem</param>
    /// <param name="code">authorization code vraceny oauth providerem</param>
    /// <param name="error">chyba nebo zruseni vracene providerem</param>
    /// <param name="ct">token pro zruseni asynchronni operace</param>
    /// <returns>normalizovany completion vysledek vcetne overeneho frontend originu</returns>
    public async Task<OAuthCompletion> CompleteAuthorizationAsync(HttpRequest request, OAuthProvider provider, string? state, string? code, string? error, CancellationToken ct = default) {
        var oauthState = await stateService.ConsumeAsync(request, provider, state, ct);
        if (oauthState == null) return new OAuthCompletion(OAuthCompletionKind.InvalidState);
        if (!providers.TryGetValue(provider, out var providerImplementation)) return new OAuthCompletion(OAuthCompletionKind.Failed, oauthState.ReturnOrigin);

        var providerResult = await providerImplementation.CompleteAuthorizationAsync(request, oauthState, code, error, ct);
        if (providerResult.Status == AuthorizationStatus.Cancelled) return new OAuthCompletion(OAuthCompletionKind.Cancelled, oauthState.ReturnOrigin);
        if (providerResult.Status != AuthorizationStatus.Succeeded || string.IsNullOrWhiteSpace(providerResult.ProviderUserId)) {
            return new OAuthCompletion(OAuthCompletionKind.Failed, oauthState.ReturnOrigin);
        }

        return oauthState.Flow == OAuthFlow.Login
            ? await CompleteLoginAsync(providerImplementation, oauthState, providerResult, ct)
            : await CompleteConnectionAsync(providerImplementation, oauthState, providerResult, ct);
    }

    /// <summary>
    /// nacte lokalni spojeni, umozni provider implementaci zrusit dlouhodoby token a potom spojeni odstrani
    /// </summary>
    /// <param name="accountId">id uctu s odpojovanou platformou</param>
    /// <param name="provider">provider odpojovane platformy</param>
    /// <param name="ct">token pro zruseni asynchronni operace</param>
    /// <returns>aktualizovany ucet, nebo null pokud ucet neexistuje</returns>
    public async Task<Account?> DisconnectAsync(Guid accountId, OAuthProvider provider, CancellationToken ct = default) {
        var connection = await db.OAuthConnections
            .Include(item => item.Account)
            .FirstOrDefaultAsync(item => item.AccountId == accountId && item.Provider == provider, ct);
        if (connection == null) return await db.Accounts.FirstOrDefaultAsync(item => item.Id == accountId, ct);

        if (providers.TryGetValue(provider, out var providerImplementation)) {
            await providerImplementation.RevokeConnectionAsync(connection, ct);
        }

        await RemoveConnectionAsync(connection, false, ct);
        return connection.Account;
    }

    /// <summary>
    /// spusti spolecnou kontrolu spojeni pro discord a podle potreby obnovi token i profil
    /// </summary>
    /// <param name="accountId">id uctu s kontrolovanym spojenim</param>
    /// <param name="forceValidation">urcuje, zda se ma ignorovat bezny interval mezi validacemi</param>
    /// <param name="ct">token pro zruseni asynchronni operace</param>
    /// <returns>ukol reprezentujici dokonceni validace</returns>
    public Task EnsureDiscordConnectionAsync(Guid accountId, bool forceValidation, CancellationToken ct = default) =>
        EnsureConnectionAsync(accountId, OAuthProvider.Discord, forceValidation, ct);

    /// <summary>
    /// spusti spolecnou kontrolu spojeni pro steam a podle dostupnosti aktualizuje verejny profil
    /// </summary>
    /// <param name="accountId">id uctu s kontrolovanym spojenim</param>
    /// <param name="forceValidation">urcuje, zda se ma ignorovat bezny interval mezi validacemi</param>
    /// <param name="ct">token pro zruseni asynchronni operace</param>
    /// <returns>ukol reprezentujici dokonceni validace</returns>
    public Task EnsureSteamConnectionAsync(Guid accountId, bool forceValidation, CancellationToken ct = default) =>
        EnsureConnectionAsync(accountId, OAuthProvider.Steam, forceValidation, ct);

    /// <summary>
    /// ulozi zdroj profilove fotky, vycisti puvodni override a nacte avatar z vybraneho spojeni
    /// </summary>
    /// <param name="accountId">id uctu, kteremu se meni zdroj profilove fotky</param>
    /// <param name="platform">provider pouzity jako zdroj avataru, nebo null pro vypnuti synchronizace</param>
    /// <param name="ct">token pro zruseni asynchronni operace</param>
    /// <returns>aktualizovany ucet, nebo null pri neexistujicim uctu ci apple provideru bez avatar podpory</returns>
    public async Task<Account?> SetAvatarSyncPlatformAsync(Guid accountId, OAuthProvider? platform, CancellationToken ct = default) {
        if (platform == OAuthProvider.Apple) return null;
        var account = await db.Accounts
            .Include(item => item.OAuthConnections)
            .FirstOrDefaultAsync(item => item.Id == accountId, ct);
        if (account == null) return null;

        account.AvatarSyncPlatform = platform;
        account.AvatarUrl = null;
        if (platform is { } provider && provider != OAuthProvider.Discord) {
            account.AvatarUrl = account.OAuthConnections.FirstOrDefault(item => item.Provider == provider)?.AvatarUrl;
        }
        await db.SaveChangesAsync(ct);

        if (platform != OAuthProvider.Discord || !account.OAuthConnections.Any(item => item.Provider == OAuthProvider.Discord)) return account;
        // discord se znovu overi, aby se ulozil aktualni avatar a pripadne obnovene tokeny
        await EnsureDiscordConnectionAsync(account.Id, true, ct);
        return await db.Accounts.FirstOrDefaultAsync(item => item.Id == accountId, ct);
    }

    /// <summary>
    /// dokonci login pouze pro ucet, ktery uz ma stejnou externi identitu ulozenou v oauth spojeni
    /// </summary>
    /// <param name="providerImplementation">implementace providera, ktera callback uspesne overila</param>
    /// <param name="oauthState">overeny state s navratovym originem a typem flow</param>
    /// <param name="providerResult">normalizovana identita a profil vraceny providerem</param>
    /// <param name="ct">token pro zruseni asynchronni operace</param>
    /// <returns>uspesny login completion nebo stav oznamujici, ze identita neni propojena</returns>
    private async Task<OAuthCompletion> CompleteLoginAsync(ExternalAuthProviderBase providerImplementation, StatePayload oauthState, AuthorizationResult providerResult, CancellationToken ct) {
        var providerUserId = providerResult.ProviderUserId;
        if (string.IsNullOrWhiteSpace(providerUserId)) return new OAuthCompletion(OAuthCompletionKind.Failed, oauthState.ReturnOrigin);
        var connection = await db.OAuthConnections
            .Include(item => item.Account)
            .FirstOrDefaultAsync(item => item.Provider == providerImplementation.Provider && item.ProviderUserId == providerUserId, ct);
        if (connection == null) return new OAuthCompletion(OAuthCompletionKind.LoginNotLinked, oauthState.ReturnOrigin);

        if (providerResult.Profile != null) {
            ApplyConnection(connection.Account, connection, providerImplementation, providerResult.Profile, providerResult.Credentials, providerResult.MarkValidated);
            await db.SaveChangesAsync(ct);
        }

        return new OAuthCompletion(OAuthCompletionKind.LoginSucceeded, oauthState.ReturnOrigin, connection.AccountId);
    }

    /// <summary>
    /// pripoji externi identitu k prihlasenemu uctu a zabrani jejimu sdileni mezi vice ucty
    /// </summary>
    /// <param name="providerImplementation">implementace providera, ktera callback uspesne overila</param>
    /// <param name="oauthState">overeny state obsahujici id prihlaseneho uctu</param>
    /// <param name="providerResult">normalizovana identita, profil a pripadne credentials</param>
    /// <param name="ct">token pro zruseni asynchronni operace</param>
    /// <returns>completion popisujici uspesne propojeni, konflikt identity nebo chybu</returns>
    private async Task<OAuthCompletion> CompleteConnectionAsync(ExternalAuthProviderBase providerImplementation, StatePayload oauthState, AuthorizationResult providerResult, CancellationToken ct) {
        if (oauthState.AccountId == null) return new OAuthCompletion(OAuthCompletionKind.InvalidState, oauthState.ReturnOrigin);
        if (providerResult.Profile == null) return new OAuthCompletion(OAuthCompletionKind.Failed, oauthState.ReturnOrigin);
        var providerUserId = providerResult.ProviderUserId;
        if (string.IsNullOrWhiteSpace(providerUserId)) return new OAuthCompletion(OAuthCompletionKind.Failed, oauthState.ReturnOrigin);

        var account = await db.Accounts
            .Include(item => item.OAuthConnections)
            .FirstOrDefaultAsync(item => item.Id == oauthState.AccountId.Value, ct);
        if (account == null) return new OAuthCompletion(OAuthCompletionKind.Failed, oauthState.ReturnOrigin);

        var alreadyConnected = await db.OAuthConnections.AsNoTracking()
            .FirstOrDefaultAsync(item => item.Provider == providerImplementation.Provider && item.ProviderUserId == providerUserId && item.AccountId != account.Id, ct);
        if (alreadyConnected != null) return new OAuthCompletion(OAuthCompletionKind.AlreadyLinked, oauthState.ReturnOrigin);

        var connection = account.OAuthConnections.FirstOrDefault(item => item.Provider == providerImplementation.Provider);
        var isNewConnection = connection == null;
        if (connection == null) {
            connection = new OAuthConnection {
                AccountId = account.Id,
                Account = account,
                Provider = providerImplementation.Provider,
                ProviderUserId = providerUserId,
                Username = providerResult.Profile.Username,
            };
            db.OAuthConnections.Add(connection);
        }

        ApplyConnection(account, connection, providerImplementation, providerResult.Profile, providerResult.Credentials, providerResult.MarkValidated);
        await db.SaveChangesAsync(ct);
        // auditni udalost se zapisuje az po uspesnem ulozeni noveho spojeni
        if (isNewConnection) {
            await dbLogger.LogInfoAsync($"Účet {FormatAccount(account)} propojil platformu {providerImplementation.Provider} jako {providerResult.Profile.Username}.", "platform-connect", ct);
        }

        return new OAuthCompletion(OAuthCompletionKind.Connected, oauthState.ReturnOrigin, account.Id);
    }

    /// <summary>
    /// podle casoveho intervalu spusti provider validator a zpracuje platne, neplatne i docasne nedostupne spojeni
    /// </summary>
    /// <param name="accountId">id uctu s kontrolovanym spojenim</param>
    /// <param name="provider">provider, jehoz spojeni se validuje</param>
    /// <param name="forceValidation">urcuje, zda se ma validace provest bez ohledu na posledni kontrolu</param>
    /// <param name="ct">token pro zruseni asynchronni operace</param>
    /// <returns>ukol reprezentujici dokonceni kontroly a pripadne aktualizace spojeni</returns>
    private async Task EnsureConnectionAsync(Guid accountId, OAuthProvider provider, bool forceValidation, CancellationToken ct) {
        if (!providers.TryGetValue(provider, out var providerImplementation) || !providerImplementation.IsConfigured) return;

        var connection = await db.OAuthConnections
            .Include(item => item.Account)
            .FirstOrDefaultAsync(item => item.AccountId == accountId && item.Provider == provider, ct);
        if (connection == null) return;

        var nowUtc = DateTime.UtcNow;
        if (!forceValidation && connection.LastValidatedUtc is { } lastValidated && lastValidated >= nowUtc - ValidationInterval) return;

        var result = await providerImplementation.ValidateConnectionAsync(connection, ct);
        // docasna nedostupnost zachova lokalni spojeni, potvrzena neplatnost ho odstrani
        if (result.Status == ConnectionValidationStatus.Invalid) {
            await RemoveConnectionAsync(connection, true, ct);
            return;
        }
        if (result.Status != ConnectionValidationStatus.Valid || result.Profile == null) return;

        ApplyConnection(connection.Account, connection, providerImplementation, result.Profile, result.Credentials, true);
        await db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// prenese normalizovany profil a credentials do databazove entity bez znalosti zdrojoveho provider api
    /// </summary>
    /// <param name="account">ucet vlastnici aktualizovane oauth spojeni</param>
    /// <param name="connection">databazove spojeni, do ktereho se profil zapisuje</param>
    /// <param name="providerImplementation">implementace pouzita pro provider-specific ulozeni credentials</param>
    /// <param name="profile">normalizovany profil vraceny providerem</param>
    /// <param name="credentials">nove tokeny nebo jine credentials, pokud je provider vratil</param>
    /// <param name="markValidated">urcuje, zda se ma aktualizovat cas posledni uspesne validace</param>
    private static void ApplyConnection(
        Account account,
        OAuthConnection connection,
        ExternalAuthProviderBase providerImplementation,
        Profile profile,
        TokenResponse? credentials,
        bool markValidated
    ) {
        connection.ProviderUserId = profile.UserId;
        connection.Username = profile.Username;
        connection.ProfileUrl = profile.ProfileUrl;
        if (!string.IsNullOrWhiteSpace(profile.AvatarUrl)) {
            connection.AvatarUrl = profile.AvatarUrl;
            if (account.AvatarSyncPlatform == connection.Provider) account.AvatarUrl = profile.AvatarUrl;
        }
        if (credentials != null) {
            providerImplementation.ApplyCredentials(connection, credentials);
        }
        if (markValidated) connection.LastValidatedUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// odstrani lokalni oauth spojeni, upravi avatar sync a zapise auditni udalost
    /// </summary>
    /// <param name="connection">spojeni urcene k odstraneni</param>
    /// <param name="automatic">urcuje, zda odstraneni vyvolala validace misto uzivatelske akce</param>
    /// <param name="ct">token pro zruseni asynchronni operace</param>
    /// <returns>ukol reprezentujici ulozeni zmen a auditniho zaznamu</returns>
    private async Task RemoveConnectionAsync(OAuthConnection connection, bool automatic, CancellationToken ct) {
        var account = connection.Account;
        var provider = connection.Provider;
        var username = connection.Username;
        db.OAuthConnections.Remove(connection);
        if (!automatic && account.AvatarSyncPlatform == provider) {
            account.AvatarSyncPlatform = null;
            account.AvatarUrl = null;
        }
        await db.SaveChangesAsync(ct);
        var mode = automatic ? "automaticky odpojena" : "odpojena";
        await dbLogger.LogInfoAsync($"Platforma {provider} ({username}) byla {mode} u účtu {FormatAccount(account)}.", "platform-disconnect", ct);
    }

    /// <summary>
    /// sestavi kratky a jednoznacny popis uctu pro auditni log
    /// </summary>
    /// <param name="account">ucet zapisovany do logu</param>
    /// <returns>cele jmeno doplnene internim id uctu</returns>
    private static string FormatAccount(Account account) => $"{account.FirstName} {account.LastName} ({account.Id})";
}
