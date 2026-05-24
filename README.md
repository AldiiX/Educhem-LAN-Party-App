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

Veřejný web ukazuje informace o aktuální akci, historii, pravidla, harmonogram, FAQ a vstup do rezervací. Přihlášená část řeší dashboard, správu účtu, profily, administraci účastníků, bezpečnostní logy, nastavení aplikace, docházku účastníků a samotné rezervace počítačů nebo místností. Backend poskytuje REST API, SignalR hub pro realtime rezervace, session přihlašování, PostgreSQL databázi, Redis sessions, aplikační cache a HTML emaily renderované přes Razor šablony.

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
- **Uživatelské účty**: session přihlášení, odhlášení, změna hesla, reset hesla přes email a přihlašovací link.
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
- **Redis**: používá se pro ASP.NET session cache a Data Protection keys, aby byly session a cookies stabilní i při restartu aplikace.
- **Nginx cache headers**: statické Next.js assety z `/_next/static/` mají dlouhou immutable cache.
- **Build cache**: Docker build používá cache mounty pro npm i NuGet balíčky.

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
- Redis pro session cache a Data Protection keys
- IMemoryCache pro rychlé rezervační snapshoty
- MailKit pro SMTP emaily
- BCrypt pro hashování hesel
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
|   |-- public/                              # obrázky, ikony, fonty, PDF
|   |-- src/app/                             # App Router routy
|   |-- src/app/app/(withlayout)/reservations # přihlášené rezervace
|   |-- src/app/app/(withlayout)/attendance  # evidence příchodů a odchodů
|   |-- src/app/app/(withlayout)/administration # uživatelé, logy a nastavení
|   |-- src/components/reservation_areas/    # mapové oblasti pro rezervace
|   |-- src/hooks/useSignalRHub.ts           # obecný SignalR hook
|   |-- src/schemas/                         # Zod schémata API odpovědí
|   `-- package.json
|-- server/                                  # ASP.NET Core backend
|   |-- Controllers/                         # REST API v1
|   |-- Data/Entities/                       # účty, školy, místnosti, počítače, rezervace
|   |-- Dto/                                 # datové modely API
|   |-- Hubs/ReservationsHub.cs              # realtime rezervace
|   |-- Migrations/                          # EF Core migrace
|   |-- Services/AppSettingsService.cs       # globální nastavení aplikace
|   |-- Services/DbLoggerService.cs          # databázové bezpečnostní logy
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

```dotenv
PSQL_DB_HOST=localhost
PSQL_DB_PORT=5432
PSQL_DB_NAME=edulp_dev
PSQL_DB_USER=edulp
PSQL_DB_PASSWORD=edulp

REDIS_IP=localhost
REDIS_PORT=6379
REDIS_PASSWORD=

WEB_URL=http://localhost:3547

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
| `WEB_URL` | Veřejná URL aplikace pro emailové odkazy |
| `SMTP_HOST` | SMTP server |
| `SMTP_PORT` | SMTP port, typicky `465` |
| `SMTP_EMAIL_USERNAME` | Odesílací email a SMTP login |
| `SMTP_EMAIL_PASSWORD` | SMTP heslo |

Nastavení jako `ChatEnabled`, `ReservationsStatus`, `ReservationsEnabledFrom`, `ReservationsEnabledTo` a `ReservationsEnabledRightNow` se ukládají do databázové tabulky `administration.AppSettings` a při startu aplikace se seedují výchozí hodnoty.

## Databáze a migrace

Projekt používá EF Core migrace v `server/Migrations`.

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
- `Schools` pro školy zobrazované u profilu.
- `Computers` v databázovém schématu `reservations`.
- `Rooms` v databázovém schématu `reservations`.
- `Reservations` jako společný základ pro `ComputerReservation` a `RoomReservation`.
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

### Přihlášená aplikace

- `http://localhost:3547/app/login` - přihlášení
- `http://localhost:3547/app` - dashboard
- `http://localhost:3547/app/announcements` - oznámení
- `http://localhost:3547/app/map` - mapa rezervací bez hlavního rezervačního panelu
- `http://localhost:3547/app/account` - nastavení účtu
- `http://localhost:3547/app/profile` - vlastní profil
- `http://localhost:3547/app/profile/{uuid}` - veřejný profil účastníka
- `http://localhost:3547/app/reservations` - realtime rezervace míst
- `http://localhost:3547/app/attendance` - docházka účastníků
- `http://localhost:3547/app/tournaments` - turnaje
- `http://localhost:3547/app/problem` - nahlášení problému
- `http://localhost:3547/app/administration` - administrace účtů
- `http://localhost:3547/app/reset-password` - reset hesla
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
- `POST /api/v1/account/login` - přihlášení
- `GET /api/v1/account/login-link` - přihlášení přes link z emailu
- `PUT /api/v1/account/me` - úprava vlastního účtu
- `POST /api/v1/account/me/password` - změna vlastního hesla
- `POST /api/v1/account/logout` - odhlášení
- `POST /api/v1/account/forgot-password` - odeslání reset odkazu
- `POST /api/v1/account/reset-password` - potvrzení resetu hesla
- `PUT /api/v1/account/me/achievements/{id}` - nastavení hlavního achievementu
- `PUT /api/v1/account/me/badges/{id}` - nastavení hlavního badge
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
- `POST /api/v1/problem-reports` - vytvoření hlášení problému
- `PUT /api/v1/problem-reports/{id}/status` - změna stavu hlášení
- `DELETE /api/v1/problem-reports/{id}` - smazání hlášení

### SignalR hub

Hub běží na:

```text
/hubs/reservations
```

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
