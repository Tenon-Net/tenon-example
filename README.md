# Tenon Example

`tenon-example` is the public reference consumer application for TenonAdmin. It is a single repository containing a growing, multi-module business system. CRM is its first flagship module; this repository does not develop reusable kernel or satellite-package capabilities.

## Release Provenance

This repository is pinned to the stable `0.3.2` release:

- NuGet: `TenonAdmin` `0.3.2` and `TenonAdmin.Templates` `0.3.2`.
- Source: TenonAdmin tag `v0.3.2`.
- Backend: generated with `dotnet new tenon-app` from `TenonAdmin.Templates@0.3.2`.
- Frontend: extracted from `Tenon-Net/TenonAdmin/web#v0.3.2`.

The `TenonAdmin` PackageReference in `tenon-example.csproj` must remain exactly aligned with the release provenance above. See [the app ledger](docs/app-ledger.md) for staged work and evidence.

## Prerequisites

- .NET SDK 10
- Node.js 22 and npm

## Backend

```powershell
dotnet restore
dotnet build -c Release
dotnet run
```

`Properties/launchSettings.json` pins `ASPNETCORE_ENVIRONMENT=Development`; without it the host resolves to `Production`, where automatic CodeFirst schema creation is disabled by design, and startup fails with missing seed tables. Do not delete this file. The default configuration uses SQLite. The first startup creates the schema and prints a random super-admin password. See [the v0.3.2 upgrade record](docs/v0.3.2-upgrade.md). With the backend running, its liveness, readiness, and development OpenAPI endpoints are available at `/health`, `/health/ready`, and `/openapi/v1.json`.

## Frontend

Run the backend first. Do not run the backend and frontend verification processes concurrently on this machine.

```powershell
Set-Location web
npm install
npm run gen:api
npm run typecheck
npm run lint
npm run dev
```

The frontend development server proxies the backend API and OpenAPI contract to `http://localhost:5100`.

## Docker

```bash
docker compose up -d --build
```

Builds and runs the full stack: MySQL, Redis, this backend, and a Caddy-fronted build of `web/`. Override secrets (`TENON_DB_PASSWORD`, `TENON_JWT_SECRET`, `TENON_ADMIN_PASSWORD`) and ports (`TENON_API_PORT`, `TENON_WEB_PORT`) via a `.env` file next to `docker-compose.yml` — never commit real values. The frontend container listens on `TENON_WEB_PORT` (default `8090`) and reverse-proxies `/api` and `/health*` to the backend itself.

## CRM Module: Multi-Org Data Scope In Action

The same `GET /api/v1/biz/customer/page` request returns a different row count depending on who is logged in, and `CustomerService` contains zero manual organization filtering — the kernel's global query filter does it. Try it live at **[tenonadmin.52moyu.net](https://tenonadmin.52moyu.net/login)**, or run it yourself: log in with any of the three seeded trial accounts (password `Trial@123456` for all three) and open **客户管理 / Customers**:

| Account | Data scope | Rows visible |
| --- | --- | --- |
| `总部管理员` (HQ admin) | All organizations | 214 |
| `华南区域经理` (South China regional manager) | South China region and below | 128 |
| `深圳专员` (Shenzhen specialist) | Shenzhen branch only | 42 |

![HQ admin sees all 214 rows](docs/assets/hq-admin-214.png)
![South China manager sees 128 rows, scoped to the region and its branches](docs/assets/south-manager-128.png)
![Shenzhen specialist sees only their own 42 rows](docs/assets/shenzhen-specialist-42.png)

All three accounts hold only read permissions on the customer endpoints (seeded in [P2](docs/app-ledger.md)), so the add/edit/delete controls are absent for them and present only for `superAdmin` — a real permission difference, not a client-side hint.

### Demo mode (read-only, for a shared/public deployment)

Set `TenonAdmin:DemoMode=true` (e.g. `TenonAdmin__DemoMode=true` as an environment variable, or in `appsettings.json`) to make every non-`GET` request — for every account, including `superAdmin` — return `403` with error code `41002`. This is a global server-side filter, not a UI convention. Leave it unset (the default) for local development and evaluation, where the trial accounts' own read-only permissions are still enough to keep the shared demo narrative intact.

This is exactly how [tenonadmin.52moyu.net](https://tenonadmin.52moyu.net/login) runs: `docker-compose.yml` in this repo builds the full stack (MySQL + Redis + backend + Caddy-fronted frontend), and the live deployment layers a server-local `docker-compose.override.yml` on top to turn `DemoMode` on — see [the app ledger](docs/app-ledger.md)'s P4 section for the deployment record, backup, and rollback steps.

## Reproducible Creation

From an empty parent directory, use the same artifacts recorded above:

```powershell
dotnet new install TenonAdmin.Templates@0.3.2
dotnet new tenon-app --output tenon-example
Set-Location tenon-example
npx degit Tenon-Net/TenonAdmin/web#v0.3.2 web
```

Then follow the backend and frontend commands in this README. The P0 validation record and consumer findings are maintained under `docs/`.
