<!-- Keep in sync with README.zh-CN.md (canonical) -->

English | [简体中文](README.zh-CN.md) | [日本語](README.ja.md)

# Tenon Example

`tenon-example` is the public reference consumer application for TenonAdmin. It is a single repository containing a growing, multi-module business system. CRM is its first flagship module; this repository does not develop reusable kernel or satellite-package capabilities.

## Release Provenance

This repository is pinned to the stable `0.3.3` release:

- NuGet: `TenonAdmin` `0.3.3` and `TenonAdmin.Templates` `0.3.3`.
- Source: TenonAdmin tag `v0.3.3`.
- Backend: generated with `dotnet new tenon-app`; its `Dockerfile` matches the fix `TenonAdmin.Templates@0.3.3` ships (adopted directly, ahead of the template release — see [the v0.3.3 upgrade record](docs/v0.3.3-upgrade.md)).
- Frontend: extracted from `Tenon-Net/TenonAdmin/web#v0.3.2`, unchanged since (`v0.3.3` made no changes under `web/` upstream).

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

The same `GET /api/v1/biz/customer/page` request returns a different row count depending on who is logged in, and `CustomerService` contains zero manual organization filtering — the kernel's global query filter does it. Try it live at **[tenonadmin.52moyu.net](https://tenonadmin.52moyu.net/login)**, or run it yourself: log in with any of the trial accounts below and open **客户管理 / Customers**:

| Account | Password | Data scope | Rows visible | Also see |
| --- | --- | --- | --- | --- |
| `总部管理员` (HQ admin) | `Trial@123456` | All organizations | 214 | Full **系统 (system)** admin console — org/user/role/menu/dict/config/log/file management, a real granted role (not `superAdmin` bypass) |
| `华南区域经理` (South China regional manager) | `Trial@123456` | South China region and below | 128 | CRM only |
| `深圳专员` (Shenzhen specialist) | `Trial@123456` | Shenzhen branch only | 42 | CRM only |
| `superAdmin` | `TenonExample@675b52d8` | Unrestricted (bypasses scope) | 214 | Every module (系统 + crm + the kernel's sample 业务 module), full CRUD everywhere |

![HQ admin sees all 214 rows](docs/assets/hq-admin-214.png)
![South China manager sees 128 rows, scoped to the region and its branches](docs/assets/south-manager-128.png)
![Shenzhen specialist sees only their own 42 rows](docs/assets/shenzhen-specialist-42.png)

The three business-role accounts hold only read permissions on the customer endpoints (seeded in [P2](docs/app-ledger.md)), so the add/edit/delete controls there are absent for them — a real permission difference, not a client-side hint. HQ admin's access into the **系统** module is the opposite case on purpose: a full, genuinely-granted role (menu-driven `SysRoleMenu` rows, exactly like a real consumer would set up for an admin), with every button visible — demonstrating that this reference app is the kernel's full stock admin-system capability *plus* CRM layered on top, not a CRM-only tool. The login page's one-click account buttons cover all four.

### Demo mode (read-only, for a shared/public deployment)

Set `TenonAdmin:DemoMode=true` (e.g. `TenonAdmin__DemoMode=true` as an environment variable, or in `appsettings.json`) to make every non-`GET` request — for every account, including `superAdmin` — return `403` with error code `41002`. This is a global server-side filter, not a UI convention. Leave it unset (the default) for local development and evaluation, where the trial accounts' own read-only permissions are still enough to keep the shared demo narrative intact.

This is exactly how [tenonadmin.52moyu.net](https://tenonadmin.52moyu.net/login) runs: `docker-compose.yml` in this repo builds the full stack (MySQL + Redis + backend + Caddy-fronted frontend), and the live deployment layers a server-local `docker-compose.override.yml` on top to turn `DemoMode` on — see [the app ledger](docs/app-ledger.md)'s P4 section for the deployment record, backup, and rollback steps.

## Reproducible Creation

From an empty parent directory, use the same artifacts recorded above:

```powershell
dotnet new install TenonAdmin.Templates@0.3.3
dotnet new tenon-app --output tenon-example
Set-Location tenon-example
npx degit Tenon-Net/TenonAdmin/web#v0.3.3 web
```

Then follow the backend and frontend commands in this README. The P0 validation record and consumer findings are maintained under `docs/`.
