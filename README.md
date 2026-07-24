# Tenon Example

`tenon-example` is the public reference consumer application for TenonAdmin. It is a single repository containing a growing, multi-module business system. CRM is its first flagship module; this repository does not develop reusable kernel or satellite-package capabilities.

## Release Provenance

This repository is pinned to the stable `0.3.0` release:

- NuGet: `TenonAdmin` `0.3.0` and `TenonAdmin.Templates` `0.3.0`.
- Source: TenonAdmin tag `v0.3.0`, commit `ac84cad325bb808e67321b7b1b4c8b37d6fa94bd`.
- Backend: generated with `dotnet new tenon-app` from `TenonAdmin.Templates::0.3.0`.
- Frontend: extracted from `Tenon-Net/TenonAdmin/web#v0.3.0`.

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

The default configuration uses SQLite. The first startup creates the schema and prints a random super-admin password. With the backend running, its liveness, readiness, and development OpenAPI endpoints are available at `/health`, `/health/ready`, and `/openapi/v1.json`.

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

## Reproducible Creation

From an empty parent directory, use the same artifacts recorded above:

```powershell
dotnet new install TenonAdmin.Templates::0.3.0
dotnet new tenon-app --output tenon-example
Set-Location tenon-example
npx degit Tenon-Net/TenonAdmin/web#v0.3.0 web
```

Then follow the backend and frontend commands in this README. The P0 validation record and consumer findings are maintained under `docs/`.
