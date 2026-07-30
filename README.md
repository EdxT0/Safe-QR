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

## Updating an existing checkout

If you already had Safe-QR cloned and set up, and are pulling this update, **skip the fresh-clone [Setup](#setup) above** and do this instead.

### What's new

- Per-user scan history (`ScanHistory` table), a real QR camera/upload scanner (html5-qrcode), and an isolated backend sandbox preview (PuppeteerSharp headless Chromium).
- Misclassification reporting (US-06) — a "🚩 Report" button on scan results, open to anyone, no login required.
- An Admin-only dashboard at `/admin` — system analytics and exportable threat/feedback reports (US-07, US-08).
- **Security fix:** public registration (`/register`) can no longer create an Admin account. Every signup is now hardcoded to `role: User` — there is no longer any way to self-elevate through the API.

### Steps

1. **Pull the latest code**
   ```bash
   git pull
   ```

2. **Rebuild the backend** — picks up the new PuppeteerSharp package
   ```bash
   cd "Src/Backend/Safe Qr/Safe Qr Backend"
   dotnet build
   ```

3. **Apply the new migrations**
   ```bash
   dotnet ef database update
   ```
   This adds the `ScanHistory` and `ThreatFeedback` tables (if you didn't already have them from a partial earlier pull) and makes `ThreatFeedback.UserId` nullable.

4. **Set up an Admin account (new, required for `/admin`)**

   Since registration can no longer create Admins, the backend instead seeds exactly one Admin account on startup, from config — but only if `Admin:Email` / `Admin:Password` are set and no user with that email exists yet:
   ```bash
   dotnet user-secrets set "Admin:Email" "admin@yourdomain.local"
   dotnet user-secrets set "Admin:Password" "choose-a-strong-password"
   dotnet user-secrets set "Admin:Name" "Site Administrator"
   ```
   If you skip this, no Admin account exists and `/admin` is unreachable for everyone — that's the safe default, not a bug.

   > ⚠️ **Check for pre-existing Admin accounts.** Before this fix, anyone could self-register with `role: "admin"`. If that ever happened on your database, that account is still an Admin — migrations don't touch existing rows. Check and demote if needed:
   > ```sql
   > SELECT "Id", "Name", "Email", "Role" FROM "User" WHERE "Role" = 'Admin';
   > UPDATE "User" SET "Role" = 'User' WHERE "Id" = <id>;
   > ```

5. **Restart the backend**
   ```bash
   dotnet run
   ```
   The first time anyone opens **Sandbox Preview**, the backend auto-downloads a headless Chromium build (needs internet, takes a minute or two once; instant after that).

6. **Frontend** — no new required steps or environment variables. `npm install` (in case anything changed) and `npm run dev` as usual.

7. **Verify the update took**
   - Register a brand-new test account — it should always come back `role: User`, even if you try to force `role: "admin"` in the request body.
   - Log in as your seeded Admin and open `/admin` — analytics + reports should load; log in as a non-admin and try `/admin` — you should get redirected away instead.
   - On any scan result, click "🚩 Report" while signed out — it should work with no login prompt.

## Verifying it works

1. Go to http://localhost:3000/register and create an account.
2. You're redirected to the scanner — scan a real QR code (camera or **Switch to Image Upload**) that encodes a URL. (The old "Demo Payloads" shortcut buttons are commented out in `app/scanner/page.jsx` for production — uncomment them there if you want canned examples for a demo.)
3. You should see a real result pulled from Google Safe Browsing, VirusTotal, the ONNX model, and the in-house engine — not a canned/mocked response.
4. Click **Save**, then go to **History** — the scan should appear, backed by the `ScanHistory` table in Postgres (scoped to your logged-in user).
5. For a suspicious/malicious result, try **Open Sandbox** — this calls the backend's isolated headless-browser screenshot service and shows a real rendered image of the page.
6. Click **🚩 Report** on any result — this works even signed out, and stores the report in `ThreatFeedback`.
7. Log in as your seeded Admin account (see [Updating an existing checkout](#updating-an-existing-checkout) if you haven't set one up) and open `/admin` — you should see live analytics and both report tables, each with a working CSV export.

## Project structure

```
Src/
  Backend/Safe Qr/Safe Qr Backend/   ASP.NET Core API (controllers, services, EF Core entities/migrations)
  Frontend/safe-qr/                  Next.js app (app/, components/, lib/services/)
```

Key backend endpoints:
- `POST /api/Scan` — public, runs a URL through the threat pipeline
- `GET/POST/DELETE /api/ScanHistory` — requires login, scoped to the caller
- `POST /api/User/{Create,Login,Logout}`, `GET /api/User/Me`
- `POST /api/Sandbox/preview` — public, isolated headless-browser screenshot
- `POST /api/ThreatFeedback` — public (login optional; attributes the report when a session exists)
- `GET /api/UrlReport/*`, `GET /api/ThreatFeedback` — **Admin-only** (`[Authorize(Roles = "Admin")]`)

## Troubleshooting

- **Login/register returns 500, or `NetworkError when attempting to fetch resource`** — see the Firefox cert-trust note above; also confirm the backend is actually running (`dotnet run` in the backend folder) and the ports in `.env.local` match `launchSettings.json`.
- **"Could not copy ... .exe" build error** — the backend is already running (in another terminal, or Visual Studio's debugger) and has the build output locked. Stop it before rebuilding.
- **Sandbox preview fails for a specific URL** — the backend refuses non-http(s) schemes and any hostname resolving to a private/loopback/link-local address (basic SSRF guard), and times out after 20s for pages that are too slow to load.
- **`/admin` redirects you away, or the "Admin" nav link never shows up** — either you're not logged in as an Admin, or no Admin account was ever seeded. Set `Admin:Email` / `Admin:Password` via `dotnet user-secrets` (see [Updating an existing checkout](#updating-an-existing-checkout)) and restart the backend — it only seeds the account if it doesn't already exist, so this is safe to run again.
- **`GET /api/UrlReport/...` or `/api/ThreatFeedback` returns 403** — expected if you're logged in but not an Admin. A 401 instead means you're not logged in at all.
