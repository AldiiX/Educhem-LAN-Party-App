# Bezpečnostní TODO & Technický dluh

Seznam otevřených bezpečnostních zranitelností, rizik a doporučených úprav k vyřešení (SignalR, tokeny, hlavičky a relace).

---

## 🔴 Vysoká priorita

- [ ] **1. Rate Limiting na SignalR Hubu (`ReservationsHub`) & DoS ochrana databáze**
  - **Soubor:** `server/Hubs/ReservationsHub.cs`
  - **Popis:** WebSocket spojení v ASP.NET Core neprochází standardním endpointovým rate limiterem (`[EnableRateLimiting]`). Klient může po připojení k socketu `/hubs/reservations` posílat neomezené množství volání `Reserve` a `Unbook`.
  - **Riziko:** Metoda `Reserve` otevírá transakci s `IsolationLevel.Serializable` a provádí vícenásobné čtení a zápisy. Útočník může jednoduchým SignalR skriptem vyčerpat connection pool PostgreSQL, způsobit zámky a shodit rezervační systém.
  - **Úkol:** Implementovat throttling / rate limiting pro SignalR volání (např. přes `IHubFilter` nebo `IMemoryCache` na `Reserve` a `Unbook` dle uživatele/IP).

- [ ] **2. Okamžité zneplatnění JWT Access Tokenů po změně hesla / odvolání relací (Revocation Check)**
  - **Soubory:** `server/Program.cs` (`AddJwtBearer`), `server/Services/AuthService.cs`
  - **Popis:** Při změně hesla nebo volání `RevokeAllSessionsAsync` se relace zneplatní v tabulce `AuthSessions`, ale existující JWT Access Tokeny jsou self-contained a Bearer handler v `Program.cs` kontroluje pouze kryptografický podpis a expiraci (10 minut).
  - **Riziko:** Po kompromitaci účtu a následné změně hesla má útočník se starým Access Tokenem přístup ještě po celých 10 minut.
  - **Úkol:** Doplnit událost `OnTokenValidated` do `JwtBearerEvents`, která zkontroluje stav relace (např. přes cache klíč `user:revoked:{accountId}` vůči claimu `iat` v tokenu).

---

## 🟡 Střední priorita

- [ ] **3. Ochrana přihlašovacího odkazu (`login-link`) před Login CSRF a předčasným spálením skenery**
  - **Soubor:** `server/Controllers/AccountControllerV1.cs` (`LoginLink`)
  - **Popis:** Endpoint `GET /api/v1/account/login-link` při pouhém GET požadavku ihned smaže token a vystaví session cookies.
    - **Zneplatnění skenery:** E-mailové bezpečnostní brány (SafeLinks, Gmail, antiviry) automaticky prefetchují odkazy v e-mailech přes GET. Tím token spálí dříve, než uživatel stihne kliknout.
    - **Login CSRF:** Útočník může přes odkaz či `<img>` donutit prohlížeč oběti provést GET a přihlásit ji ke svému účtu.
  - **Úkol:**
    - Změnit přihlašovací flow podobně jako u změny e-mailu: odkaz v e-mailu nasměrovat na frontendovou stránku a samotné ověření a přihlášení provést přes `POST` dotaz (vyžadující interakci uživatele).

- [ ] **4. Odstranění časového postranního kanálu (Timing Attack) u `forgot-password`**
  - **Soubor:** `server/Controllers/AccountControllerV1.cs` (`ForgotPassword`)
  - **Popis:** Ačkoliv endpoint vždy vrací 200 OK, při existujícím e-mailu synchronně odesílá SMTP e-mail (latence 300–1500 ms), zatímco při neexistujícím e-mailu skončí za 1–3 ms. Útočník může měřením latence přesně zjišťovat, které e-maily v databázi existují.
  - **Úkol:**
    - Přesunout odesílání e-mailů do asynchronní fronty / background úlohy (např. `Channel<T>` nebo `IBackgroundTaskQueue`), aby endpoint vracel odpověď v konstantním čase bez čekání na SMTP server.

---

## 🟢 Nízká priorita & Architektonická čistota

- [ ] **5. Přechod z URL Query parametrů na URL Fragment u citlivých tokenů**
  - **Soubory:** `server/Controllers/AccountControllerV1.cs`, e-mailové šablony (`UserForgotPassword.cshtml`, `UserRegistered.cshtml`)
  - **Popis:** U resetu hesla a přihlašovacího odkazu se tokeny posílají v query stringu (`?token=...`), což vede k jejich zápisu do access logů webových serverů, proxy a historie prohlížeče. U změny e-mailu byl zvolen bezpečnější přístup s URL fragmentem (`#token=...`), který se v HTTP požadavku neodesílá.
  - **Úkol:**
    - Zvážit sjednocení předávání tokenů přes URL fragment (`#token=...`) i pro reset hesla.
