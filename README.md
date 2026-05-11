# EDUCHEM LAN Party App

<p align="center">
  <img src="client/public/images/logo/logo.png" alt="EDUCHEM LAN Party" width="160" />
</p>

<p align="center">
  Moderní webová aplikace pro organizaci školních EDUCHEM LAN party akcí.
  Informace pro účastníky, přihlašování, profily, administrace účtů a příprava rezervační části v jednom systému.
</p>

<p align="center">
  <img alt="Next.js" src="https://img.shields.io/badge/Next.js-16-black?style=for-the-badge&logo=nextdotjs" />
  <img alt="React" src="https://img.shields.io/badge/React-19-20232A?style=for-the-badge&logo=react&logoColor=61DAFB" />
  <img alt="TypeScript" src="https://img.shields.io/badge/TypeScript-6-3178C6?style=for-the-badge&logo=typescript&logoColor=white" />
  <img alt=".NET" src="https://img.shields.io/badge/.NET-10-512BD4?style=for-the-badge&logo=dotnet&logoColor=white" />
  <img alt="PostgreSQL" src="https://img.shields.io/badge/PostgreSQL-18-4169E1?style=for-the-badge&logo=postgresql&logoColor=white" />
  <img alt="Redis" src="https://img.shields.io/badge/Redis-DC382D?style=for-the-badge&logo=redis&logoColor=white" />
</p>

---

## Obsah

- [Popis projektu](#popis-projektu)
- [Aktuální vývojáři](#aktuální-vývojáři)
- [Funkcionality](#funkcionality)
- [Technologie](#technologie)
- [Struktura projektu](#struktura-projektu)
- [Instalace a spuštění dev verze](#instalace-a-spuštění-dev-verze)
- [Konfigurace prostředí](#konfigurace-prostředí)
- [Databáze a migrace](#databáze-a-migrace)
- [Produkce a Docker](#produkce-a-docker)
- [Adresy aplikace](#adresy-aplikace)
- [Pravidla commitování](#pravidla-commitování)

## Popis projektu

**EDUCHEM LAN Party App** je full-stack aplikace pro přípravu a správu LAN party událostí na škole EDUCHEM. Projekt spojuje veřejnou prezentační část pro účastníky s přihlášenou aplikací pro uživatele a administrátory.

Veřejná část slouží jako přehled aktuální akce, pravidel, harmonogramu, historie a často kladených otázek. Přihlášená část řeší uživatelské účty, profily, změnu hesla, správu oprávnění a administraci účastníků. Backend poskytuje REST API, session přihlašování, ukládání dat do PostgreSQL, Redis session storage a HTML emaily přes Razor šablony.

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

- **Prezentační web události**: homepage, informace, historie, pravidla, harmonogram, FAQ a veřejná rezervační stránka.
- **Uživatelské přihlášení**: session autentizace, odhlášení, změna hesla a obnova hesla přes emailový odkaz.
- **Profily účastníků**: vlastní profil, veřejné profily podle UUID, avatar, banner, třída, škola a role.
- **Administrace účtů**: vytváření, úprava, mazání, reset hesla, odeslání přihlašovacích údajů a filtrování účtů.
- **Role a oprávnění**: `Student`, `Teacher`, `TeacherOrg`, `Admin` a `SuperAdmin`.
- **Dashboard data**: statistiky aktivních účtů, počtu účastníků, staffu a tříd.
- **Emailové šablony**: registrační email, reset hesla a email s přihlašovacími údaji renderované přes Razor views.
- **Příprava na rezervace**: příznaky pro povolení rezervací a samostatné routy pro rezervační část aplikace.
- **Containerizace**: produkční Docker image s .NET backendem, Next.js standalone frontendem a Nginx reverse proxy.

## Technologie

### Frontend

- [Next.js 16](https://nextjs.org/) s App Routerem a standalone buildem
- [React 19](https://react.dev/)
- [TypeScript 6](https://www.typescriptlang.org/)
- [Sass](https://sass-lang.com/) pro globální i modulové styly
- [SWR](https://swr.vercel.app/) pro klientský data fetching
- Vlastní komponenty, ikonky a tematizace přes `WebThemeProvider`

### Backend

- [.NET 10](https://dotnet.microsoft.com/)
- ASP.NET Core controllers
- Entity Framework Core 10
- PostgreSQL 18, mimo jiné kvůli `uuidv7()`
- Redis pro session cache a Data Protection keys
- MailKit pro SMTP emaily
- BCrypt pro hashování hesel
- Razor view rendering pro HTML emailové šablony

### Infrastruktura

- Docker multi-stage build
- Nginx reverse proxy
- Node.js 24 build stage
- PostgreSQL
- Redis

## Struktura projektu

```text
.
|-- client/                    # Next.js frontend
|   |-- public/                # obrázky, ikony, fonty, PDF
|   |-- src/app/               # App Router routy
|   |-- src/components/        # sdílené UI komponenty
|   |-- src/data/              # konfigurace webu a historické události
|   `-- package.json
|-- server/                    # ASP.NET Core backend
|   |-- Controllers/           # REST API v1
|   |-- Data/                  # EF Core DbContext a entity
|   |-- Dto/                   # datové modely API
|   |-- Migrations/            # EF Core migrace
|   |-- Services/              # auth, emaily, vokativ, Razor rendering
|   |-- Views/Emails/          # HTML emailové šablony
|   `-- server.csproj
|-- Dockerfile                 # produkční build celé aplikace
|-- nginx.conf                 # proxy pro /api a Next.js frontend
|-- start.sh                   # start backendu, frontendu a nginxu
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
dotnet tool install --global dotnet-ef
```

### 3. Instalace frontend závislostí

```bash
cd client
npm install
```

### 4. Spuštění databáze a Redis

V projektu zatím není pevný `docker-compose.yml`, ale pro lokální vývoj stačí spustit PostgreSQL a Redis například takto:

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

SMTP_HOST=smtp.example.com
SMTP_PORT=465
SMTP_EMAIL_USERNAME=lanparty@example.com
SMTP_EMAIL_PASSWORD=change-me
```

> Pokud nechceš lokálně posílat emaily, SMTP hodnoty nech jako placeholdery. Funkce, které email odesílají, při špatné konfiguraci vrátí chybu do logu, ale aplikace zůstane běžet.

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

Next.js v dev režimu proxyuje API volání z `/api/*` na backend `http://localhost:8080/api/*`.

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
| `SMTP_HOST` | SMTP server |
| `SMTP_PORT` | SMTP port, typicky `465` |
| `SMTP_EMAIL_USERNAME` | Odesílací email a SMTP login |
| `SMTP_EMAIL_PASSWORD` | SMTP heslo |

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

- `Accounts` pro uživatelské účty, role, profily a rezervační oprávnění.
- `Schools` pro školy zobrazované u profilu.
- PostgreSQL enumy `AccountGender` a `AccountType`.

## Produkce a Docker

Produkce se balí do jednoho image:

- frontend se sestaví jako Next.js standalone aplikace,
- backend se publikuje jako .NET aplikace,
- Nginx slouží jako reverse proxy na portu `80`,
- `/api/*` jde na ASP.NET Core backend,
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
- `http://localhost:3547/app/account` - nastavení účtu
- `http://localhost:3547/app/profile` - vlastní profil
- `http://localhost:3547/app/administration` - administrace účtů
- `http://localhost:3547/app/reset-password` - reset hesla

### API

- `GET /api/v1/account` - aktuálně přihlášený účet
- `GET /api/v1/account/dashboard` - dashboard statistiky
- `GET /api/v1/account/all` - seznam účtů pro organizátory
- `POST /api/v1/account/login` - přihlášení
- `POST /api/v1/account/logout` - odhlášení
- `POST /api/v1/account/forgot-password` - odeslání reset odkazu
- `POST /api/v1/account/reset-password` - potvrzení resetu hesla
- `GET /api/v1/profile` - profil aktuálního uživatele
- `GET /api/v1/profile/{uuid}` - veřejný profil podle UUID

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

> Původní projekt byl vytvořen v roce 2024: https://github.com/aldiix/edUCHEM-LAN-Party-Web
