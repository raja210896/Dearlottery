# LotteryAnalytics

Statistical analytics PWA for tracking lottery draw results and historical patterns. **Statistical analysis only — past results do not guarantee future results.**

## Stack

- **Frontend:** React + TypeScript + Vite + React Router, PWA
- **Backend:** ASP.NET Core Web API (.NET 8) + EF Core + SQL Server, JWT admin auth
- **Sync:** Backend-only Sambad API client (BackgroundService scheduler)

## Structure

```
frontend/   React PWA
backend/    ASP.NET Core Web API
database/   migration notes / seed data
docs/       project docs
```

## Setup

### Backend

```bash
cd backend
dotnet restore
dotnet ef migrations add InitialCreate
dotnet ef database update
dotnet run
```

Copy `backend/.env.example` to configure `ConnectionStrings__DefaultConnection`, `Sambad__BaseUrl`, `Sambad__Token`, `Jwt__Secret` as environment variables (ASP.NET Core uses `__` as the nested-config separator), or via `appsettings.Development.json` for local dev (gitignored in production as `appsettings.Production.json`).

### Frontend

```bash
cd frontend
npm install
npm run dev
npm run build
npm run test
```

Copy `.env.example` to `.env` and set `VITE_API_BASE_URL`.

### Tests

```bash
# Backend (xUnit)
cd backend.Tests
dotnet test

# Frontend (Vitest)
cd frontend
npm run test
```

## Admin

First admin login is bootstrapped from `AdminBootstrap__Username` / `AdminBootstrap__Password` env vars on first run (only if no admin user exists yet). Log in at `/admin/login`.

## Notifications

Web Push requires VAPID keys (`WebPush__PublicKey` / `WebPush__PrivateKey`). Generate a pair with:

```bash
npx web-push generate-vapid-keys
```

WhatsApp notifications are an unimplemented abstraction (`IWhatsAppNotificationService`) reserved for a future paid integration — not wired up in v1.

## Notes

- Sambad API credentials never reach the frontend — all sync happens server-side.
- No secrets are committed; see `.env.example` in each project.
- Deployment targets (Cloudflare Pages/Vercel for frontend, any ASP.NET-compatible host for backend, any SQL Server-compatible host for the database) are not hard-coded.
