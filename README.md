# Safe-QR

A multi-layered QR code / URL safety scanner. Users scan or upload a QR code, the backend runs the decoded URL through several threat-detection engines in parallel (Google Safe Browsing, VirusTotal, an in-house ONNX phishing model, and a rule-based engine), and the result is shown with an option to save it to a per-user history and preview the URL safely via an isolated backend sandbox.

## Architecture

| Layer | Tech | Location |
|---|---|---|
| Frontend | Next.js 14 (React) | `Src/Frontend/safe-qr` |
| Backend API | ASP.NET Core (.NET 10) | `Src/Backend/Safe Qr/Safe Qr Backend` |
| Database | PostgreSQL (via EF Core) | external — you provide the instance |

The frontend and backend are two separate processes on two separate ports. The frontend calls the backend over HTTPS with `credentials: 'include'`; the backend allows this via a CORS policy plus a session cookie (`SameSite=None; Secure`).

## Prerequisites

Install these before doing anything else:

- **[.NET 10 SDK](https://dotnet.microsoft.com/download)** — required to build/run the backend.
- **[Node.js](https://nodejs.org/)** 18.17+ (or any current LTS) — required for the frontend.
- **[PostgreSQL](https://www.postgresql.org/download/)** — a running server you can create a database on (local install, Docker container, or a hosted instance).
- **EF Core CLI tool**, used to apply database migrations:
  ```bash
  dotnet tool install --global dotnet-ef
  ```
- **git**, to clone the repo.

You'll also want API keys for the two external threat-intel services the scan pipeline calls (see [Backend secrets](#2-backend-secrets) below):
- **VirusTotal** — free account + API key at https://www.virustotal.com/gui/my-apikey
- **Google Safe Browsing** — API key from the [Google Cloud Console](https://console.cloud.google.com/) (enable the "Safe Browsing API" on a project, then create an API key)

Both are **required**, not optional — the backend throws on the first scan request if the VirusTotal key is missing, and Safe Browsing calls will fail without a key (the pipeline does have a local ONNX + rule-engine fallback if a *call* fails, but it does not substitute for a missing key entirely).

## Setup

### 1. Clone and create the database

```bash
git clone <this-repo-url>
```

In `psql` (or any Postgres client), create a database and a user for the app:

```sql
CREATE DATABASE safeqr_dev;
CREATE USER safeqr_user WITH PASSWORD 'choose-a-password';
GRANT ALL PRIVILEGES ON DATABASE safeqr_dev TO safeqr_user;
```

### 2. Backend secrets

From `Src/Backend/Safe Qr/Safe Qr Backend`, use [.NET user-secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets) so real credentials never end up in `appsettings.json` (which is committed and intentionally left blank):

```bash
cd "Src/Backend/Safe Qr/Safe Qr Backend"
dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Port=5432;Database=safeqr_dev;Username=safeqr_user;Password=choose-a-password"
dotnet user-secrets set "VirusTotal:ApiKey" "<your virustotal api key>"
dotnet user-secrets set "SafeBrowsing:ApiKey" "<your google safe browsing api key>"
```

### 3. Build the backend and apply migrations

Still in `Src/Backend/Safe Qr/Safe Qr Backend`:

```bash
dotnet build
dotnet ef database update
```

This creates the `User`, `UrlReport`, and `ScanHistory` tables in `safeqr_dev`.

### 4. Trust the HTTPS dev certificate

The backend serves HTTPS with ASP.NET Core's self-signed local dev certificate. Trust it once per machine:

```bash
dotnet dev-certs https --trust
```

> **Using Firefox?** This command only trusts the cert for the Windows/macOS OS certificate store, which Chrome/Edge read from — Firefox keeps its own separate store and won't see it. If you get `NetworkError when attempting to fetch resource` when logging in, open `https://localhost:56166/api/UrlReport/All` directly in Firefox once, click **Advanced → Accept the Risk and Continue**, then retry. (Check `Properties/launchSettings.json` for the actual HTTPS port if it's not 56166 on your machine.)

### 5. Run the backend

```bash
dotnet run
```

By default it listens on `https://localhost:56166` and `http://localhost:5027` (see `Properties/launchSettings.json`). Leave this running in its own terminal.

The **first** request that triggers the sandbox-preview feature (`POST /api/Sandbox/preview`) will automatically download a headless Chromium build via PuppeteerSharp — this needs internet access and can take a minute or two the first time; every request after that is fast.

### 6. Frontend setup

In a separate terminal:

```bash
cd Src/Frontend/safe-qr
npm install
```

Copy the example env file and adjust if your backend is running on a different port:

```bash
cp .env.example .env.local
```

`.env.local` should contain:
```
NEXT_PUBLIC_API_BASE_URL=https://localhost:56166
```

Then run it:

```bash
npm run dev
```

Open http://localhost:3000 — it redirects straight to the scanner page.

> If your frontend runs on a port other than 3000, the backend's CORS policy needs to know about it. Either set an environment variable / `appsettings` entry for `FrontendOrigins` as a string array (see `Program.cs`), or it defaults to allowing `http://localhost:3000` only.

## Verifying it works

1. Go to http://localhost:3000/register and create an account.
2. You're redirected to the scanner — click one of the **Demo Payloads** (or scan/upload a real QR code containing a URL).
3. You should see a real result pulled from Google Safe Browsing, VirusTotal, the ONNX model, and the in-house engine — not a canned/mocked response.
4. Click **Save**, then go to **History** — the scan should appear, backed by the `ScanHistory` table in Postgres (scoped to your logged-in user).
5. For a suspicious/malicious result, try **Open Sandbox** — this calls the backend's isolated headless-browser screenshot service and shows a real rendered image of the page.

## Project structure

```
Src/
  Backend/Safe Qr/Safe Qr Backend/   ASP.NET Core API (controllers, services, EF Core entities/migrations)
  Frontend/safe-qr/                  Next.js app (app/, components/, lib/services/)
```

Key backend endpoints: `POST /api/Scan`, `GET/POST/DELETE /api/ScanHistory`, `POST /api/User/{Create,Login,Logout}`, `GET /api/User/Me`, `POST /api/Sandbox/preview`, `GET /api/UrlReport/*`.

## Troubleshooting

- **Login/register returns 500, or `NetworkError when attempting to fetch resource`** — see the Firefox cert-trust note above; also confirm the backend is actually running (`dotnet run` in the backend folder) and the ports in `.env.local` match `launchSettings.json`.
- **"Could not copy ... .exe" build error** — the backend is already running (in another terminal, or Visual Studio's debugger) and has the build output locked. Stop it before rebuilding.
- **Sandbox preview fails for a specific URL** — the backend refuses non-http(s) schemes and any hostname resolving to a private/loopback/link-local address (basic SSRF guard), and times out after 20s for pages that are too slow to load.
