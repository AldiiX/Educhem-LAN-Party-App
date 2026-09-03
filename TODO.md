# Bezpečnostní TODO & Technický dluh

Seznam bezpečnostních zranitelností, rizik a doporučených úprav identifikovaných při auditu kódu (autentizace, změna e-mailu, práce s tokeny a SignalR).

---

## 🔴 Vysoká priorita

- [ ] **1. Zabezpečení hlavičky `X-Forwarded-For` a Rate Limiteru**
  - **Soubor:** `server/Program.cs`
  - **Popis:** V `ForwardedHeadersOptions` se volá `KnownIPNetworks.Clear()` a `KnownProxies.Clear()`, což způsobuje, že aplikace důvěřuje hlavičce `X-Forwarded-For` od libovolného klienta. Útočník může rotovat podvržené IP adresy v hlavičce a zcela tím obejít rate limit pro nepřihlášené uživatele (`RemoteIpAddress`). Zároveň při chybějící IP všichni sdílejí klíč `"anonymous"`, což umožňuje DoS legitimních uživatelů.
  - **Úkol:**
    - Omezit `KnownProxies` / `KnownIPNetworks` na konkrétní IP adresu lokální reverzní proxy (Nginx / Docker síť), nebo zajistit striktní přepis hlavičky na úrovni Nginxu.
    - Zkontrolovat a ošetřit fallback klíče v rate limiteru pro neautentizované požadavky.

- [ ] **2. Doplnění Rate Limitingu na autentizační endpointy (Brute-Force & DoS ochrana)**
  - **Soubory:** `server/Controllers/AuthControllerV1.cs`, `server/Controllers/AccountControllerV1.cs`
  - **Popis:** Zatímco nová změna e-mailu má rate limiting a sledování pokusů, ostatní klíčové endpointy ochranu postrádají:
    - `POST /api/v1/auth/login` nemá rate limit ani lockout. BCrypt je výpočetně náročný (~100–200 ms), nekontrolovaný proud požadavků způsobuje CPU DoS a umožňuje hádání hesel.
    - `POST /api/v1/account/forgot-password` nemá rate limit – útočník může spamovat e-mailové schránky uživatelů, plnit tabulku `AccountEmailLinks` a vyčerpat SMTP kvóty.
    - `POST /api/v1/account/me/password` nesleduje neúspěšné pokusy o staré heslo (na rozdíl od `EmailChangeService.StartAsync`).
  - **Úkol:**
    - Aplikovat rate limiting politiky na `login`, `forgot-password` i změnu vlastního hesla.
    - Zvážit evidenci neúspěšných pokusů o heslo (account lockout / delay).

- [ ] **3. Sjednocení normalizace e-mailů a podpora IDN (Punycode)**
  - **Soubory:** `server/Infrastructure/AccountEmail.cs`, `server/Services/AuthService.cs`, `server/Controllers/AccountControllerV1.cs`
  - **Popis:** `AccountEmail.TryNormalize` převádí mezinárodní domény do Punycode (`IdnMapping().GetAscii`) a výsledek ukládá do DB jako např. `novak@xn--hky-tma69a.cz`. V `AuthService.LoginAsync` a `AccountControllerV1.ForgotPassword` se však e-mail porovnává přímo jako `item.Email == identifier.Trim().ToLower()`, což neprojde přes Punycode normalizaci. Uživatel s diakritikou v doméně se nepřihlásí ani neobnoví heslo. Navíc `ToLower()` závisí na kultuře vlákna (problém v např. tureckém locale `tr-TR` s písmenem `I/ı`).
  - **Úkol:**
    - Před vyhledáním účtu v `LoginAsync` i `ForgotPassword` normalizovat vstup přes `AccountEmail.TryNormalize(identifier, out var normalized)` a dotazovat se na `normalized` (případně konzistentně používat `ToLowerInvariant()`).

---

## 🟡 Střední priorita

- [ ] **4. Ochrana přihlašovacího odkazu (`login-link`) před Login CSRF a předčasným spálením skenery**
  - **Soubor:** `server/Controllers/AccountControllerV1.cs` (`LoginLink`)
  - **Popis:** Endpoint `GET /api/v1/account/login-link` při pouhém GET požadavku ihned smaže token a vystaví session cookies.
    - **Zneplatnění skenery:** E-mailové bezpečnostní brány (SafeLinks, Gmail, antiviry) automaticky prefetchují odkazy v e-mailech přes GET. Tím token spálí dříve, než uživatel stihne kliknout.
    - **Login CSRF:** Útočník může přes odkaz či `<img>` donutit prohlížeč oběti provést GET a přihlásit ji ke svému účtu.
  - **Úkol:**
    - Změnit přihlašovací flow podobně jako u změny e-mailu: odkaz v e-mailu nasměrovat na frontendovou stránku a samotné ověření a přihlášení provést přes `POST` dotaz (vyžadující interakci uživatele).

- [ ] **5. Odstranění časového postranního kanálu (Timing Attack) u `forgot-password`**
  - **Soubor:** `server/Controllers/AccountControllerV1.cs` (`ForgotPassword`)
  - **Popis:** Ačkoliv endpoint vždy vrací 200 OK, při existujícím e-mailu synchronně odesílá SMTP e-mail (latence 300–1500 ms), zatímco při neexistujícím e-mailu skončí za 1–3 ms. Útočník může měřením latence přesně zjišťovat, které e-maily v databázi existují.
  - **Úkol:**
    - Přesunout odesílání e-mailů do asynchronní fronty / background úlohy (např. `Channel<T>` nebo `IBackgroundTaskQueue`), aby endpoint vracel odpověď v konstantním čase bez čekání na SMTP server.

---

## 🟢 Nízká priorita & Architektonická čistota

- [ ] **6. Odstranění závislosti na side-effectu loggeru v `EmailChangeService`**
  - **Soubor:** `server/Services/EmailChangeService.cs` (`ConfirmAsync`, `CancelAsync`)
  - **Popis:** V metodách `ConfirmAsync` a `CancelAsync` chybí explicitní `await db.SaveChangesAsync(ct);`. Změny entit se ukládají pouze jako vedlejší efekt volání `await LogAsync(...)`, které volá `db.SaveChangesAsync()`. Pokud by se logování změnilo na asynchronní/bufferované nebo bylo v testech vypnuto, změny se do databáze neuloží.
  - **Úkol:**
    - Doplnit explicitní `await db.SaveChangesAsync(ct);` před potvrzením transakce `transaction.CommitAsync(ct);`.

- [ ] **7. Přechod z URL Query parametrů na URL Fragment u citlivých tokenů**
  - **Soubory:** `server/Controllers/AccountControllerV1.cs`, e-mailové šablony (`UserForgotPassword.cshtml`, `UserRegistered.cshtml`)
  - **Popis:** U resetu hesla a přihlašovacího odkazu se tokeny posílají v query stringu (`?token=...`), což vede k jejich zápisu do access logů webových serverů, proxy a historie prohlížeče. U změny e-mailu byl zvolen bezpečnější přístup s URL fragmentem (`#token=...`), který se v HTTP požadavku neodesílá.
  - **Úkol:**
    - Zvážit sjednocení předávání tokenů přes URL fragment (`#token=...`) i pro reset hesla.
