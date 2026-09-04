# EDUCHEM LAN Party App

<p align="center">
  <img src="client/public/images/logo/logo.png" alt="EDUCHEM LAN Party" width="160" />
</p>

<p align="center">
  Moderní webová aplikace pro organizaci školních EDUCHEM LAN party akcí.
  Řeší prezentační web, účty účastníků, administraci, profily, realtime rezervace míst, docházku a provozní statistiky.
</p>

<p align="center">
  <img alt="Next.js" src="https://img.shields.io/badge/Next.js-16-black?style=for-the-badge&logo=nextdotjs" />
  <img alt="React" src="https://img.shields.io/badge/React-19-20232A?style=for-the-badge&logo=react&logoColor=61DAFB" />
  <img alt="TypeScript" src="https://img.shields.io/badge/TypeScript-6-3178C6?style=for-the-badge&logo=typescript&logoColor=white" />
  <img alt=".NET" src="https://img.shields.io/badge/.NET-10-512BD4?style=for-the-badge&logo=dotnet&logoColor=white" />
  <img alt="SignalR" src="https://img.shields.io/badge/SignalR-realtime-512BD4?style=for-the-badge" />
  <img alt="PostgreSQL" src="https://img.shields.io/badge/PostgreSQL-18-4169E1?style=for-the-badge&logo=postgresql&logoColor=white" />
  <img alt="Redis" src="https://img.shields.io/badge/Redis-cache%20%26%20sessions-DC382D?style=for-the-badge&logo=redis&logoColor=white" />
</p>

---

## Obsah

- [Popis projektu](#popis-projektu)
- [Aktuální vývojáři](#aktuální-vývojáři)
- [Funkcionality](#funkcionality)
- [Rezervace](#rezervace)
- [Realtime a cache](#realtime-a-cache)
- [Bezpečnost, autentizace a šifrování](#bezpečnost-autentizace-a-šifrování)
- [Administrace, nastavení a docházka](#administrace-nastavení-a-docházka)
- [Technologie](#technologie)
- [Screenshoty z aplikace](#screenshoty-z-aplikace)
- [Struktura projektu](#struktura-projektu)
- [Instalace a spuštění dev verze](#instalace-a-spuštění-dev-verze)
- [Konfigurace prostředí](#konfigurace-prostředí)
- [Databáze a migrace](#databáze-a-migrace)
- [Produkce a Docker](#produkce-a-docker)
- [Adresy aplikace](#adresy-aplikace)
- [API a SignalR](#api-a-signalr)
- [Pravidla commitování](#pravidla-commitování)

## Popis projektu

**EDUCHEM LAN Party App** je full-stack aplikace pro přípravu a správu LAN party událostí na škole EDUCHEM. Projekt spojuje veřejnou prezentační část pro účastníky s přihlášenou aplikací pro studenty, organizátory a administrátory.

Veřejný web ukazuje informace o aktuální akci, historii, pravidla, harmonogram, FAQ a vstup do rezervací. Přihlášená část řeší dashboard, správu účtu, profily, administraci účastníků, bezpečnostní logy, nastavení aplikace, docházku účastníků a samotné rezervace počítačů nebo místností. Backend poskytuje REST API, SignalR hub pro realtime rezervace, JWT přihlašování s access a refresh cookies, PostgreSQL databázi, Redis cache, aplikační cache a HTML emaily renderované přes Razor šablony.

## Aktuální vývojáři

<table align="center">
  <tr>
    <td align="center" width="220">
      <a href="https://stanislavskudrna.cz">
        <img src="https://cloud02.emsio.cz/public/avatars/stanislavskudrna.png" alt="Stanislav Škudrna" width="120" style="border-radius: 50%;" />
      </a>
      <br />
      <strong>Stanislav Škudrna</strong>
      <br />
      <a href="https://stanislavskudrna.cz">Web</a>
      ·
      <a href="https://github.com/AldiiX">GitHub</a>
    </td>
    <td align="center" width="220">
      <a href="https://serhii.cz">
        <img src="https://cloud02.emsio.cz/public/avatars/serhii.png" alt="Serhii Yavorskyi" width="120" style="border-radius: 50%;" />
      </a>
      <br />
      <strong>Serhii Yavorskyi</strong>
      <br />
      <a href="https://serhii.cz">Web</a>
      ·
      <a href="https://github.com/Javornicek">GitHub</a>
    </td>
  </tr>
</table>

<p align="center">
  Aktuálně vyvíjejí a spravují aplikaci EDUCHEM LAN Party App.
</p>

## Funkcionality

- **Prezentační web události**: hlavní stránka, informace, historie, pravidla, harmonogram, FAQ a veřejný vstup do rezervací.
- **Uživatelské účty**: JWT přihlášení s databázovými refresh sessions, CSRF ochranou, správou přihlášených zařízení, změnou hesla, resetem hesla přes email a přihlašovacím linkem.
- **Profily účastníků**: vlastní profil, veřejné profily podle UUID, avatar, banner, třída, škola a role.
- **Administrace účtů**: vytváření, úprava, mazání, reset hesla, impersonace, odeslání přihlašovacích údajů a filtrování účtů.
- **Bezpečnostní logy**: databázové logování důležitých akcí, filtrování a administrátorský přehled v aplikaci.
- **Nastavení aplikace**: administrace globálních voleb, zapnutí chatu, otevření/uzavření rezervací, rezervační časovač a vyčištění aplikační cache.
- **Docházka**: check-in/check-out účastníků, důvod odchodu, přehled aktuálně přítomných a možnost zápisu za jiného účastníka pro organizátory.
- **Role a oprávnění**: `Student`, `Teacher`, `TeacherOrg`, `Admin` a `SuperAdmin`.
- **Rezervace míst**: realtime mapa počítačů a místností s možností rezervovat, změnit nebo zrušit vlastní rezervaci.
- **Dashboard a statistiky**: přehled účtů, aktivních uživatelů, povolených rezervací, staffu, tříd a kapacity.
- **Emailové šablony**: registrace, reset hesla a nové přihlašovací údaje přes Razor views.
- **Containerizace**: produkční Docker image s .NET backendem, Next.js standalone frontendem a Nginx reverse proxy.

## Rezervace

Rezervační část je nyní plnohodnotná součást aplikace a běží na adrese `/app/reservations`.

- **Interaktivní mapa**: mapa je posuvná a zoomovatelná přes komponentu `MovableMap`.
- **Více pater / zón**: aktuálně jsou připravené záložky `IT Hub (Spodní patro)` a `Spirála (Horní patro)`.
- **Počítače i místnosti**: uživatel může rezervovat konkrétní počítač nebo místnost s kapacitou pro vlastní setup.
- **Jedna rezervace na účet**: nová rezervace automaticky nahrazuje předchozí rezervaci stejného účtu.
- **Zrušení rezervace**: uživatel může vlastní rezervaci zrušit přes SignalR metodu `Unbook`.
- **Oprávnění přes účet**: rezervovat mohou jen účty s `EnableReservations = true`.
- **Globální stav rezervací**: administrace umí rezervace vynutit otevřené, zavřené nebo řídit podle časovače `UseTimer`.
- **Rezervační odpočet**: UI používá serverový čas a ukazuje začátek/konec rezervačního okna podle nastavení aplikace.
- **Učitelská místa**: počítače označené jako `IsTeachersComputer` jsou dostupné pouze pro účty s rolí alespoň `Teacher`.
- **Kapacity místností**: místnost lze obsadit jen do hodnoty `Room.Capacity`.
- **Ochrana proti souběhu**: zápis rezervace běží v serializable transakci a řeší kolize při rychlém souběžném kliknutí.
- **Stavy v UI**: mapa rozlišuje volné místo, obsazeno/nedostupné a vlastní rezervaci.
- **Pravý panel**: ukazuje statistiky, seznam rezervací, profily přihlášených účastníků a upozornění, když účet nemá rezervace povolené.
- **Stav připojení**: UI ukazuje připojeno, připojování, reconnect a ztrátu spojení.
- **Počet online klientů**: SignalR posílá do mapy aktuální počet připojených klientů.

Anonymní návštěvník vidí obsazenost bez detailních profilů. Přihlášený uživatel vidí u rezervací profily a může přejít na detail účastníka.

## Realtime a cache

Rezervace jsou postavené na kombinaci SignalR, PostgreSQL a aplikační cache:

- **SignalR hub**: `/hubs/reservations` posílá počáteční snapshot rezervací a následně jen změny.
- **Oddělená data podle přihlášení**: přihlášení klienti dostávají DTO s profily, anonymní klienti anonymizovaná DTO.
- **Delta update**: po rezervaci nebo zrušení se neposílá celý seznam znovu, ale jen `previousReservation` a nová `reservation`.
- **Client-side delta merge**: frontend změnu slepí do aktuálního seznamu lokálně přes `useReservationsHub`.
- **Connection status throttling**: počet připojených klientů se broadcastuje maximálně jednou za sekundu.
- **Memory cache pro rezervace**: `ReservationCacheService` drží zvlášť cache pro přihlášené a anonymní snapshoty.
- **Sdílená aplikační cache**: `AppCacheService` sjednocuje práci s `IMemoryCache` a umožňuje administrátorské kompletní vyčištění cache.
- **Cache pro mapová data**: místnosti a počítače se načítají přes cache klíč `reservations:rooms-and-computers`.
- **Krátká status cache**: souhrnný status rezervací se cachuje na 30 sekund.
- **Anti-stampede zámky**: cache používá `SemaphoreSlim`, aby se při prázdné cache nespustilo více stejných DB dotazů najednou.
- **Redis**: slouží pro perzistenci Data Protection keyringu (sdílené napříč restarty a instancemi) a připravenou distribuovanou cache; všechny klíče a kanály jsou bezpečně izolovány prefixem `REDIS_KEY_PREFIX` (výchozí `edulp:`).
- **Nginx cache headers**: statické Next.js assety z `/_next/static/` mají dlouhou immutable cache.
- **Build cache**: Docker build používá cache mounty pro npm i NuGet balíčky.

## Bezpečnost, autentizace a šifrování

Aplikace klade důraz na bezpečnost, ochranu proti zneužití a striktní oddělení kryptografických principů:

### 1. Autentizace: Podepisování (JWT) vs. Šifrování (Data Protection)

V architektuře je přesně rozlišen účel digitálního podpisu a šifrování:

- **Digitální podepisování (JWT s `JWT_SECRET`)**:
  - Krátkodobé **Access tokeny** (uložené v HttpOnly cookie `edulp_access` nebo v hlavičce `Authorization: Bearer`) slouží k bezestavovému ověření identity a rolí uživatele.
  - Data v tokenu **nejsou šifrovaná**, ale jsou **kryptograficky podepsaná** algoritmem HMAC-SHA256 s tajemstvím `JWT_SECRET`. Server tak okamžitě ověří integritu a autentičnost (uživatel nemůže token pozměnit ani si přidat roli `Admin`), aniž by musel při každém HTTP požadavku sahat do databáze.
  - Čitelná klientská cookie `edulp_access_expires` (v produkci `__Host-edlp_access_expires`) nese pouze čas expirace v unixových sekundách a slouží frontendu k naplánování automatické tiché obnovy tokenu.
- **Dlouhodobé sessions (Refresh Sessions)**:
  - Refresh tokeny jsou náhodné kryptografické řetězce navázané na záznam v PostgreSQL tabulce `AuthSessions` (včetně klientské IP adresy, User-Agentu a času expirace). Umožňují bezpečné vystavení nového access tokenu a správu či revokaci jednotlivých aktivních sessions přímo v profilu uživatele.
- **Obousměrné šifrování dat (ASP.NET Core Data Protection, AES-256)**:
  - Slouží k utajení citlivých dat, která nesmí nikdo nepovolaný přečíst:
    - **Discord OAuth tokeny v DB**: Access a refresh tokeny propojených Discord účtů se před uložením do tabulky `OAuthConnections` šifrují přes `IDataProtector` (`tokenProtector.Protect`). V databázi jsou uložena pouze šifrovaná data, takže ani při případném úniku DB nehrozí zneužití Discord účtů.
    - **Antiforgery (CSRF) cookies**: Cookie `X-XSRF-TOKEN` obsahuje šifrovaný stav a kryptografický nonce, které chrání formuláře před Cross-Site Request Forgery útoky.
    - **Dočasné OAuth cookies**: Cookie `educhemlanparty_external` chrání stav přihlašování třetích stran během přesměrování.
- **Šifrování keyringu v Redisu (`DataProtectionKeyEncryptor`)**:
  - XML keyring Data Protection obsahuje master klíče pro celou aplikaci. Před uložením do Redisu se XML payload šifruje pomocí **AES-256-GCM**.
  - Šifrovací klíč se bezpečně odvozuje pomocí **HKDF** (SHA-256) z existujícího `JWT_SECRET`, takže v konfiguraci není nutné spravovat další tajnou proměnnou.
- **Uživatelská hesla a jednorázové odkazy**:
  - Uživatelská hesla jsou bezpečně hashována přes **BCrypt** (`EnhancedHashPassword` s SHA-384 a work factorem 12).
  - Jednorázové přihlašovací a resetovací odkazy (Magic linky) se evidují v PostgreSQL tabulce `AccountEmailTokens` s kryptografickým hashem (`SHA-256`), účelem a časem expirace. Při použití se v serializable transakci atomicky ověří a smažou.

### 2. Ochrana proti zneužití a Rate Limiting

Aplikace implementuje víceúrovňovou ochranu proti útokům hrubou silou, botům a přetížení:

- **ASP.NET Core Rate Limiter** (pro HTTP endpointy, při překročení vrací HTTP `429 Too Many Requests`):
  - `auth-login`: 60 požadavků / 1 min (podle IP adresy klienta)
  - `auth-forgot-password`: 10 požadavků / 15 min (podle IP adresy klienta)
  - `auth-change-password`: 10 požadavků / 1 hodina (podle uživatele nebo IP adresy)
  - `email-change`: 30 požadavků / 1 min (podle uživatele nebo IP adresy)
- **Brute-force ochrana přihlášení (`AuthService`)**:
  - Sledování neúspěšných pokusů o heslo podle kombinace klientské IP adresy a identifikátoru účtu v cache. Po 5 neúspěšných pokusech se přihlášení pro daný účet z dané IP dočasně zablokuje.
- **SignalR Rate Limiting (`HubRateLimitManager`)**:
  - Plovoucí okno (sliding window) pro mutace rezervací (`Reserve`, `Unbook`): max. 5 požadavků za 10 sekund na jednoho uživatele. Chrání rezervační hub před klikacími makry a spamováním serveru.
- **Antiforgery validace**:
  - Globální `AntiforgeryValidationMiddleware` validuje přítomnost a platnost `X-XSRF-TOKEN` hlavičky u všech stav měnících HTTP metod (`POST`, `PUT`, `DELETE`, `PATCH`).

### 3. Izolace a jmenné prostory v Redisu

Při sdílení stejného Redis serveru mezi více aplikacemi nebo prostředími (dev / staging / prod):
- **Konfigurovatelný prefix (`REDIS_KEY_PREFIX`)**: Výchozí hodnota je `edulp:`. Pokud uživatel zadá prefix bez dvojtečky (např. `edulp`), aplikace ji automaticky doplní na `edulp:`.
- **Data Protection keyring**: Ukládá se pod názvem `${REDIS_KEY_PREFIX}DataProtection-Keys`, což brání kolizi s obecným Microsoft výchozím klíčem `"DataProtection-Keys"`.
- **Distribuovaná cache**: Běží pod jmenným prostorem `${REDIS_KEY_PREFIX}cache:`.
- **Pub/Sub kanály**: Konfigurace StackExchange.Redis automaticky aplikuje `ChannelPrefix = RedisChannel.Literal(redisPrefix)`.
- **Logické databáze (`REDIS_DATABASE` / `REDIS_DB`)**: Volitelná podpora pro výběr Redis databáze (0–15).

## Administrace, nastavení a docházka

Administrace na `/app/administration` je rozdělená do záložek:

- **Uživatelé**: správa účtů, filtrování podle role, pohlaví, třídy, školy a povolených rezervací, reset hesla a impersonace dostupná podle role.
- **Bezpečnostní logy**: přehled databázových logů z `administration.Logs`; endpoint je dostupný pro role `Admin` a `SuperAdmin`.
- **Nastavení aplikace**: stav rezervací `Closed`, `Open` nebo `UseTimer`, časové okno rezervací, přepínač chatu a tlačítko pro vyčištění memory cache; dostupné pro role `Admin` a `SuperAdmin`.

Docházka běží na `/app/attendance` a zapisuje záznamy do schématu `attendance`. Přihlášený uživatel zapisuje vlastní příchod/odchod, u odchodu musí vyplnit důvod. Role od `TeacherOrg` výš může zapisovat docházku za spravovatelné účty a stránka průběžně ukazuje počty přítomných, nepřítomných a celkový seznam účastníků s povolenými rezervacemi.

## Technologie

### Frontend

- [Next.js 16](https://nextjs.org/) s App Routerem a standalone buildem
- [React 19](https://react.dev/)
- [TypeScript 6](https://www.typescriptlang.org/)
- [@microsoft/signalr](https://www.npmjs.com/package/@microsoft/signalr) pro realtime rezervace
- [SWR](https://swr.vercel.app/) pro klientský data fetching
- [Zustand](https://zustand-demo.pmnd.rs/) pro lokální stav výběru rezervace
- [Sass](https://sass-lang.com/) pro globální i modulové styly
- `react-hot-toast` pro klientské hlášky
- produkční hashování CSS module tříd přes custom `next.config.ts`

### Backend

- [.NET 10](https://dotnet.microsoft.com/)
- ASP.NET Core controllers a SignalR hub
- Entity Framework Core 10
- PostgreSQL 18, mimo jiné kvůli `uuidv7()`
- Redis pro perzistenci Data Protection klíčů a distribuovanou cache s jmenným prostorem a prefixovou izolací
- ASP.NET Core Data Protection s AES-256-GCM šifrováním master keyringu odvozeného z `JWT_SECRET` přes HKDF
- ASP.NET Core Rate Limiter pro ochranu auth endpointů a HubRateLimitManager pro SignalR plovoucí okna
- IMemoryCache pro rychlé rezervační snapshoty a brute-force ochranu
- MailKit pro SMTP emaily
- BCrypt (Enhanced SHA-384) pro bezpečné hashování hesel
- Czech vocative data pro oslovení uživatelů
- Razor view rendering pro HTML emailové šablony

### Infrastruktura

- Docker multi-stage build
- Nginx reverse proxy s podporou WebSocket upgrade
- Node.js 24 build stage
- PostgreSQL
- Redis

## Screenshoty z aplikace

<table>
  <tr>
    <td width="50%" align="center">
      <img src="https://cloud02.emsio.cz/public/img/edulp/1.png" alt="Hlavní stránka" width="100%" />
      <br />
      <sub>Hlavní stránka</sub>
    </td>
    <td width="50%" align="center">
      <img src="https://cloud02.emsio.cz/public/img/edulp/2.png" alt="Dashboard aplikace" width="100%" />
      <br />
      <sub>Dashboard aplikace</sub>
    </td>
  </tr>
  <tr>
    <td width="50%" align="center">
      <img src="https://cloud02.emsio.cz/public/img/edulp/3.png" alt="Realtime rezervace" width="100%" />
      <br />
      <sub>Realtime rezervace</sub>
    </td>
    <td width="50%" align="center">
      <img src="https://cloud02.emsio.cz/public/img/edulp/4.png" alt="Administrace účtů" width="100%" />
      <br />
      <sub>Administrace účtů</sub>
    </td>
  </tr>
</table>

## Struktura projektu

```text
.
|-- client/                                  # Next.js frontend
|   |-- public/                              # obrázky, ikony a fonty
|   |-- src/app/(presentation)/              # veřejné prezentační routy
|   |-- src/app/%5Fapi/payment-qr/route.ts   # serverový handler pro platební QR
|   |-- src/app/app/(withlayout)/account/    # účet, nastavení, achievementy a sessions
|   |-- src/app/app/(withlayout)/reservations # přihlášené rezervace
|   |-- src/app/app/(withlayout)/attendance  # evidence příchodů a odchodů
|   |-- src/app/app/(withlayout)/administration # správa uživatelů, logů a nastavení
|   |-- src/components/reservation_areas/    # mapové oblasti pro rezervace
|   |-- src/hooks/useSignalRHub.ts           # obecný SignalR hook
|   |-- src/lib/apiClient.ts                 # CSRF, refresh a opakování API požadavků
|   |-- src/schemas/                         # Zod schémata API odpovědí
|   `-- package.json
|-- server/                                  # ASP.NET Core backend
|   |-- Controllers/                         # REST API v1
|   |-- Data/Entities/                       # databázové entity všech domén
|   |-- Dto/                                 # datové modely API
|   |-- Hubs/ReservationsHub.cs              # realtime rezervace
|   |-- Infrastructure/AuthConstants.cs      # JWT konfigurace, cookies a auth policies
|   |-- Migrations/                          # EF Core migrace
|   |-- Services/AuthService.cs              # access tokeny a refresh sessions
|   |-- Services/AppSettingsService.cs       # globální nastavení aplikace
|   |-- Services/DbLoggerService.cs          # databázové bezpečnostní logy
|   |-- Services/OAuth/                      # jednotlivé OAuth platformy
|   |-- Services/ReservationCacheService.cs  # cache rezervačních dat
|   |-- Views/Emails/                        # HTML emailové šablony
|   `-- server.csproj
|-- Dockerfile                               # produkční build celé aplikace
|-- nginx.conf                               # proxy pro /api, /hubs a Next.js frontend
|-- start.sh                                 # start backendu, frontendu a nginxu
`-- Educhem LAN Party App.slnx
```

## Instalace a spuštění dev verze

### 1. Klonování repozitáře

```bash
git clone https://github.com/AldiiX/Educhem-LAN-Party-App.git
cd Educhem-LAN-Party-App
```

### 2. Instalace požadovaného softwaru

- [.NET SDK 10](https://dotnet.microsoft.com/download)
- [Node.js 24](https://nodejs.org/)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) pro PostgreSQL a Redis
- `dotnet-ef` pro práci s migracemi:

```bash
dotnet tool restore
```

### 3. Instalace frontend závislostí

```bash
cd client
npm install
```

### 4. Spuštění databáze a Redis

Projekt používá PostgreSQL 18 a Redis. Pro lokální vývoj stačí:

```bash
docker run --name edulp-postgres -e POSTGRES_DB=edulp_dev -e POSTGRES_USER=edulp -e POSTGRES_PASSWORD=edulp -p 5432:5432 -d postgres:18
docker run --name edulp-redis -p 6379:6379 -d redis:8
```

### 5. Vytvoření backend `.env`

Vytvoř soubor `server/.env`:

Nejdřív v PowerShellu vygeneruj náhodný JWT secret:

```powershell
$bytes = [byte[]]::new(32)
[Security.Cryptography.RandomNumberGenerator]::Fill($bytes)
[Convert]::ToBase64String($bytes)
```

Výstup vlož do `JWT_SECRET`:

```dotenv
PSQL_DB_HOST=localhost
PSQL_DB_PORT=5432
PSQL_DB_NAME=edulp_dev
PSQL_DB_USER=edulp
PSQL_DB_PASSWORD=edulp

REDIS_IP=localhost
REDIS_PORT=6379
REDIS_PASSWORD=
REDIS_KEY_PREFIX=edulp:
# REDIS_DATABASE=0

JWT_SECRET=vloz-sem-vygenerovanou-base64-hodnotu
WEB_URL=http://localhost:3547

STEAM_WEB_API_KEY=change-me

# apple je ted vypnuty, tyhle hodnoty se budou hodit az pak
# APPLE_CLIENT_ID=cz.example.educhemlanparty.web
# APPLE_TEAM_ID=change-me
# APPLE_KEY_ID=change-me
# APPLE_PRIVATE_KEY_BASE64=change-me

SMTP_HOST=smtp.example.com
SMTP_PORT=465
SMTP_EMAIL_USERNAME=lanparty@example.com
SMTP_EMAIL_PASSWORD=change-me
```

Pokud nechceš lokálně posílat emaily, SMTP hodnoty nech jako placeholdery. Funkce, které email odesílají, při špatné konfiguraci vrátí chybu do logu, ale aplikace zůstane běžet.

### 6. Spuštění backendu

```bash
cd server
dotnet restore
dotnet ef database update
dotnet run
```

Backend běží podle `server/appsettings.json` na:

```text
http://localhost:8080
```

### 7. Spuštění frontendu

V druhém terminálu:

```bash
cd client
npm run dev
```

Frontend běží na:

```text
http://localhost:3547
```

Next.js v dev režimu proxyuje API volání z `/api/*` na backend `http://localhost:8080/api/*`. SignalR hub je dostupný na `/hubs/reservations`.

## Konfigurace prostředí

Backend načítá proměnné z `server/.env` přes `dotenv.net`.

| Proměnná | Popis |
| --- | --- |
| `PSQL_DB_HOST` | Host PostgreSQL serveru |
| `PSQL_DB_PORT` | Port PostgreSQL serveru |
| `PSQL_DB_NAME` | Název databáze |
| `PSQL_DB_USER` | Uživatel databáze |
| `PSQL_DB_PASSWORD` | Heslo databáze |
| `REDIS_IP` | Host Redis serveru |
| `REDIS_PORT` | Port Redis serveru |
| `REDIS_PASSWORD` | Redis heslo, může být prázdné |
| `REDIS_KEY_PREFIX` | Prefix klíčů v Redisu (výchozí `edulp:`) pro izolaci od ostatních aplikací |
| `REDIS_DATABASE` | Volitelný index logické databáze v Redisu (výchozí `0`), lze použít i alias `REDIS_DB` |
| `JWT_SECRET` | Náhodný Base64 secret pro podepisování access JWT; po dekódování musí mít alespoň 32 bajtů |
| `WEB_URL` | Pevný veřejný HTTP(S) origin aplikace pro emailové odkazy a OAuth callbacky |
| `STEAM_WEB_API_KEY` | Steam Web API klíč pro načtení jména a avataru propojeného Steam účtu |
| `APPLE_CLIENT_ID` | Apple Services ID použité jako OAuth `client_id` |
| `APPLE_TEAM_ID` | Team ID z Apple Developer účtu |
| `APPLE_KEY_ID` | ID privátního klíče s povoleným Sign in with Apple |
| `APPLE_PRIVATE_KEY_BASE64` | Celý Apple `.p8` privátní klíč zakódovaný v Base64; neukládat do Gitu |
| `SMTP_HOST` | SMTP server |
| `SMTP_PORT` | SMTP port, typicky `465` |
| `SMTP_EMAIL_USERNAME` | Odesílací email a SMTP login |
| `SMTP_EMAIL_PASSWORD` | SMTP heslo |

Nastavení jako `ChatEnabled`, `ReservationsStatus`, `ReservationsEnabledFrom`, `ReservationsEnabledTo` a `ReservationsEnabledRightNow` se ukládají do databázové tabulky `administration.AppSettings` a při startu aplikace se seedují výchozí hodnoty.

### Sign in with Apple

Integrace je v kódu připravená a v UI záměrně vypnutá. Pro pozdější zpřístupnění změň u Apple platformy v `client/src/data/platforms.ts` hodnotu `disabled` na `false` a nastav níže popsané `APPLE_*` proměnné. Backend Apple provider automaticky zaregistruje, jakmile najde úplnou konfiguraci.

Sign in with Apple neposkytuje profilovou fotku ani URL avataru. Apple proto zůstává mimo nabídku synchronizace avataru a propojení může sloužit jen k přihlášení a identifikaci účtu.

Apple webové přihlášení vyžaduje členství v Apple Developer Programu, primární App ID s povoleným Sign in with Apple, navázané Services ID a privátní `.p8` klíč. V Apple Developer portálu zaregistruj produkční doménu a přesnou návratovou URL:

```text
https://tvoje-domena.cz/api/v1/apple/callback
```

Apple nepovoluje jako návratovou URL `localhost`, IP adresu ani nezabezpečené HTTP. Proto Apple přihlášení při lokálním `WEB_URL=http://localhost:3547` záměrně vrátí stav `503`; ověřuje se až přes registrovanou HTTPS doménu.

Hodnotu `APPLE_PRIVATE_KEY_BASE64` vytvoř z obsahu staženého `.p8` klíče, například v PowerShellu:

```powershell
[Convert]::ToBase64String([IO.File]::ReadAllBytes("AuthKey_CHANGE_ME.p8"))
```

## Databáze a migrace

Projekt používá EF Core migrace v `server/Migrations`.

Migrace se při startu aplikace automaticky neaplikují. Před spuštěním nové verze je potřeba aktualizovat databázi ručně. Startup pouze doplňuje chybějící výchozí nastavení do již existujícího schématu.

Vytvoření nebo aktualizace databáze:

```bash
cd server
dotnet ef database update
```

Vytvoření nové migrace:

```bash
cd server
dotnet ef migrations add NazevMigrace
```

Aktuální model obsahuje hlavně:

- `Accounts` pro uživatelské účty, role, profily a povolení rezervací.
- `AuthSessions` pro refresh sessions, jejich expiraci a revokaci přihlášených zařízení.
- `EmailChangeRequests` pro čekající změnu e-mailu a `EmailChangeAttempts` pro limity žádostí a odesílání.
- `AccountEmailLinks` (entita `AccountEmailToken`) pro jednorázové přihlašovací a resetovací tokeny, které změna hesla nebo e-mailu zneplatní.
- `OAuthConnections` pro propojené Discord, Google, GitHub, Steam a připravené Apple účty.
- `Enrollments` a `Schools` pro školu a volitelnou třídu zobrazovanou u profilu.
- `Achievements`, `Badges`, `AccountAchievements` a `AccountBadges` pro achievement systém.
- `Computers` v databázovém schématu `reservations`.
- `Rooms` v databázovém schématu `reservations`.
- `Reservations` jako společný základ pro `ComputerReservation` a `RoomReservation`.
- `ProblemReports` pro hlášení problémů a jejich stav.
- `AttendanceEntries` ve schématu `attendance` pro příchody a odchody účastníků.
- `Logs` ve schématu `administration` pro bezpečnostní a provozní logy.
- `AppSettings` ve schématu `administration` pro globální nastavení aplikace.
- Unikátní index na `Reservation.AccountId`, takže jeden účet může mít jen jednu aktivní rezervaci.
- PostgreSQL enumy `AccountGender` a `AccountType`.

## Produkce a Docker

Produkce se balí do jednoho image:

- frontend se sestaví jako Next.js standalone aplikace,
- backend se publikuje jako .NET aplikace,
- Nginx slouží jako reverse proxy na portu `80`,
- `/api/*` jde na ASP.NET Core backend,
- `/hubs/*` a SignalR komunikace používají proxy s WebSocket upgrade hlavičkami,
- ostatní routy jdou na Next.js frontend.

Build image:

```bash
docker build -t educhem-lan-party-app .
```

Při buildu se do image zkopíruje `server/.env`, pokud existuje. Pokud chceš předat konfiguraci bezpečněji přes BuildKit secret, Dockerfile podporuje secret `BACKEND_ENV_B64`.

Spuštění image:

```bash
docker run --name educhem-lan-party-app -p 80:80 educhem-lan-party-app
```

## Adresy aplikace

### Veřejná část

- `http://localhost:3547/` - hlavní stránka
- `http://localhost:3547/info` - informace o akci
- `http://localhost:3547/history` - historie LAN party
- `http://localhost:3547/reservation` - veřejná rezervační stránka
- `http://localhost:3547/rules` - pravidla
- `http://localhost:3547/schedule` - harmonogram
- `http://localhost:3547/faq` - často kladené otázky
- `http://localhost:3547/organizers` - organizátoři akce
- `http://localhost:3547/login` - prezentační vstup do přihlášení

### Přihlášená aplikace

- `http://localhost:3547/app/login` - přihlašovací formulář
- `http://localhost:3547/app/login-link` - dokončení přihlášení přes jednorázový odkaz z emailu
- `http://localhost:3547/app/reset-password` - formulář pro nastavení nového hesla z emailového odkazu
- `http://localhost:3547/app/change-email` - potvrzení nebo zrušení změny emailové adresy z odkazu
- `http://localhost:3547/app` - dashboard
- `http://localhost:3547/app/announcements` - oznámení
- `http://localhost:3547/app/map` - mapa rezervací bez hlavního rezervačního panelu
- `http://localhost:3547/app/account` - přehled účtu
- `http://localhost:3547/app/account/settings` - nastavení profilu, hesla a propojených platforem
- `http://localhost:3547/app/account/achievements` - achievementy a odznaky účtu
- `http://localhost:3547/app/account/devices` - správa aktivních přihlášení
- `http://localhost:3547/app/profile` - vlastní profil
- `http://localhost:3547/app/profile/{uuid}` - veřejný profil účastníka
- `http://localhost:3547/app/reservations` - realtime rezervace míst
- `http://localhost:3547/app/attendance` - docházka účastníků
- `http://localhost:3547/app/tournaments` - turnaje
- `http://localhost:3547/app/support` - nahlášení problému; původní `/app/problem` sem přesměruje
- `http://localhost:3547/app/administration/users` - administrace účtů
- `http://localhost:3547/app/administration/logs` - bezpečnostní logy
- `http://localhost:3547/app/administration/settings` - globální nastavení aplikace
- `http://localhost:3547/app/system-disabled` - systémová stránka pro vypnutou aplikaci

## API a SignalR

### REST API

- `GET /api/v1/account` - aktuálně přihlášený účet
- `GET /api/v1/account/dashboard` - dashboard statistiky
- `GET /api/v1/account/all` - seznam účtů pro organizátory
- `POST /api/v1/account` - vytvoření účtu
- `PUT /api/v1/account/{id}` - úprava účtu
- `DELETE /api/v1/account/{id}` - smazání účtu
- `POST /api/v1/account/{id}/reset-password` - reset hesla účtu administrátorem
- `POST /api/v1/account/{id}/impersonate` - přihlášení jako jiný účet podle oprávnění
- `GET /api/v1/auth/csrf` - vystavení CSRF tokenu
- `POST /api/v1/auth/login` - přihlášení a vytvoření access a refresh cookies
- `POST /api/v1/auth/refresh` - obnova access tokenu přes refresh session
- `POST /api/v1/auth/logout` - odhlášení a revokace aktuální refresh session
- `GET /api/v1/account/login-link` - přesměrování z odkazu v emailu na frontend
- `POST /api/v1/account/login-link/preview` - náhled cílového účtu pro jednorázový přihlašovací odkaz
- `POST /api/v1/account/login-link` - přihlášení přes jednorázový odkaz z emailu
- `POST /api/v1/account/forgot-password` - odeslání reset odkazu
- `POST /api/v1/account/reset-password/preview` - náhled cílového účtu pro reset hesla
- `POST /api/v1/account/reset-password` - potvrzení resetu hesla
- `GET /api/v1/account/email-change` - stav rozpracované žádosti o změnu emailu
- `POST /api/v1/account/email-change` - zahájení dvoufázové změny emailu
- `POST /api/v1/account/email-change/resend` - opětovné odeslání potvrzovacích emailů
- `POST /api/v1/account/email-change/cancel` - zrušení žádosti o změnu emailu
- `POST /api/v1/account/email-change/preview` - veřejný náhled tokenu z odkazu v emailu
- `POST /api/v1/account/email-change/confirm` - potvrzení nebo zrušení změny přes token z odkazu
- `PUT /api/v1/account/me` - úprava vlastního účtu
- `PUT /api/v1/account/avatar-sync-platform` - nastavení synchronizace avataru z propojené platformy
- `POST /api/v1/account/me/password` - změna vlastního hesla
- `GET /api/v1/account/sessions` - seznam aktivních sessions účtu
- `DELETE /api/v1/account/sessions/{id}` - revokace vybrané session
- `DELETE /api/v1/account/sessions/other` - revokace všech ostatních sessions
- `GET /api/v1/{provider}/login` - zahájení přihlášení přes externí platformu
- `GET /api/v1/{provider}/connect` - propojení platformy s přihlášeným účtem
- `DELETE /api/v1/{provider}/connection` - odpojení platformy od účtu
- `PUT /api/v1/account/me/achievements/{id}` - nastavení viditelnosti / skrytí achievementu
- `PUT /api/v1/account/me/badges/{id}` - připnutí nebo odepnutí odznaku (badge) na profilu
- `GET /api/v1/profile` - profil aktuálního uživatele
- `GET /api/v1/profile/{uuid}` - veřejný profil podle UUID
- `GET /api/v1/reservations/rooms-and-computers` - místnosti a počítače pro mapu
- `GET /api/v1/reservations/computers-and-rooms` - alias pro mapová data
- `GET /api/v1/reservations/status` - souhrnný stav kapacity a povolených rezervací
- `GET /api/v1/reservations` - seznam rezervací pouze v debug buildu; v produkci rezervace probíhají přes socket
- `GET /api/v1/attendance` - přehled docházky přihlášeného uživatele nebo organizátora
- `POST /api/v1/attendance` - zápis příchodu/odchodu
- `GET /api/v1/adm/logs` - bezpečnostní logy pro `Admin` a `SuperAdmin`
- `GET /api/v1/appsettings` - nastavení aplikace pro `Admin` a `SuperAdmin`
- `PUT /api/v1/appsettings` - úprava nastavení aplikace pro `Admin` a `SuperAdmin`
- `POST /api/v1/appsettings/cache/clear` - vyčištění memory cache pro `Admin` a `SuperAdmin`
- `GET /api/v1/problem-reports` - seznam hlášení problémů
- `GET /api/v1/problem-reports/availability` - dostupnost vytváření hlášení
- `POST /api/v1/problem-reports` - vytvoření hlášení problému
- `PUT /api/v1/problem-reports/{id}/status` - změna stavu hlášení
- `DELETE /api/v1/problem-reports/{id}` - smazání hlášení

Hodnota `{provider}` může být `discord`, `google`, `github`, `steam` nebo `apple`; Apple endpointy jsou dostupné až po zapnutí a úplné konfiguraci provideru.

### SignalR hub

Hub běží na:

```text
/hubs/reservations
```

Access JWT zůstává v HttpOnly cookie. Samostatná čitelná cookie `edlp_access_expires` (v produkci `__Host-edlp_access_expires`) obsahuje pouze čas expirace v unixových sekundách a slouží klientovi k naplánování obnovy. Při odhlášení se maže společně s tokeny; sama o sobě nepovoluje přístup.

Server ukončí přihlášené spojení po expiraci access tokenu. Před prvním připojením i automatickým reconnectem přihlášený klient zkontroluje čas expirace. Refresh volá jen při chybějícím údaji, zbývající platnosti nejvýše 15 sekund nebo odpovědi `401` při vyjednání spojení. Platný token se při běžném reconnectu nemění. Souběžné obnovy přes API a SignalR v jedné kartě sdílejí jeden požadavek.

Přihlášený klient přidává `requireAuthentication=true`, takže chybějící nebo neplatný JWT vede k `401` místo tichého připojení jako anonym. Neplatná refresh session ukončí opakování a zobrazí odkaz na přihlášení; síťové chyby používají běžné opakování spojení. Anonymní návštěvníci refresh nevolají. Každé nové spojení dostane aktuální snapshot rezervací.

Serverové metody volané klientem:

- `Reserve({ id, type })` - rezervuje počítač nebo místnost, kde `type` je `computer` nebo `room`.
- `Unbook()` - zruší aktuální rezervaci přihlášeného účtu.

Události posílané klientům:

- `ReceiveReservations` - počáteční snapshot rezervací po připojení.
- `ReservationsChanged` - delta změna po rezervaci nebo zrušení.
- `ReceiveStatus` - počet aktuálně připojených klientů.
- `ReceiveError` - chybová hláška pro volajícího klienta.

## Pravidla commitování

Doporučené předpony commitu:

- `FEAT` - přidání nové funkcionality
- `FIX` - oprava chyby
- `DOCS` - úprava dokumentace
- `STYLE` - formátování bez změny chování kódu
- `REFACTOR` - úprava kódu bez přidání funkce nebo opravy chyby
- `TEST` - testy
- `CHORE` - údržba projektu
- `BUILD` - build system nebo závislosti
- `CI` - kontinuální integrace
- `PERF` - výkonnostní zlepšení
- `REVERT` - návrat změn

---

<p align="center">
  Vytvořili Stanislav Škudrna, Serhii Yavorskyi pro Střední školu EDUCHEM, a.s. v roce 2026.
</p>

> Původní projekt byl vytvořen v roce 2024: https://github.com/aldiix/EDUCHEM-LAN-Party-Web
