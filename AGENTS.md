# AGENTS.md

## Product Boundary

This is the TenonAdmin reference consumer application, not a kernel fork. It is one continuously growing multi-module system; CRM is the first flagship module. Reusable engines and capabilities belong in `tenon-admin` or a satellite package only after separate dogfood evidence and an explicitly approved project.

The current P0 scope is consumer bootstrap and validation only. Do not implement `Customer`, start P1, modify TenonAdmin kernel code, add a second business module, add a React frontend, or deploy a production environment.

## Release Discipline

- Keep `TenonAdmin` and `TenonAdmin.Templates` at the same exact stable release.
- Extract `web/` from the matching TenonAdmin tag or commit, never `dev`.
- Record release version, tag, commit, commands, logs, and validation evidence in `docs/`.
- Use English conventional commits, one independently reviewable task per commit.

## CRM Contract For Later Stages

- CRM module: `code=crm`, `ModuleId=1000`; its default route and menu belong to that module.
- The three trial users use `DefaultModuleId=1000`, are enabled non-super-admin accounts, have `MustChangePassword=false`, a non-null `LastPasswordChangeTime`, and no phone binding.
- Scope endpoints return structured DTOs, never fixed Chinese sentences. The Vue frontend creates zh/en range text from those DTOs.

## Verification

Run backend and frontend heavy processes sequentially because of local memory constraints. For backend changes, use restore, Release build, a first SQLite startup, real HTTP login, health endpoints, and OpenAPI. For frontend changes, use `npm install`, `npm run gen:api`, `npm run typecheck`, `npm run lint`, then a real browser login.

Treat the ledger as the implementation source of truth. Keep CRM-specific work here; only reproducible kernel/template defects are candidates for a `tenon-admin` dogfood issue.
