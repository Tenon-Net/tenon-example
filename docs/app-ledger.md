# Tenon Example Execution Ledger

## Provenance

- Stable package release: `0.3.0`
- TenonAdmin source: tag `v0.3.0`, commit `ac84cad325bb808e67321b7b1b4c8b37d6fa94bd`
- Backend template: `TenonAdmin.Templates::0.3.0`
- Frontend source: `Tenon-Net/TenonAdmin/web#v0.3.0`

This ledger owns the detailed implementation work for P1-P4. The strategic ledger and kernel/template dogfood intake remain in `TenonAdmin`.

## P0: Consumer Bootstrap And Validation

Status: complete on 2026-07-24. See [P0 validation](p0-validation.md).

- [x] Confirm the generated PackageReference is exactly `TenonAdmin` `0.3.0`.
- [x] Record `dotnet restore` and Release build evidence.
- [x] Record first SQLite startup, CodeFirst schema creation, and random super-admin password output.
- [x] Complete a real HTTP login and record `/health`, `/health/ready`, and `/openapi/v1.json` responses.
- [x] Install frontend dependencies, generate the API contract, type-check, lint, and complete a real browser login.
- [x] Reproduce the critical README path from an empty directory.
- [x] Record consumer findings. Only reusable kernel/template findings are eligible for a TenonAdmin dogfood issue.

## P1: CRM Backend

- [ ] Add the CRM consumer module with `code=crm`, `ModuleId=1000`, its default route, and menu ownership.
- [ ] Add `Customer` as a `DataEntity` with name, contact, phone, intended amount, and status. Do not set organization or creator anchors in service code; the runtime AOP does that for interactive writes.
- [ ] Implement models, service contract, service, consumer error-code constants from `60000`, DI registration, and an `api/v1/biz/customer` controller guarded by `[RolePermission]`.
- [ ] Keep `CustomerService` free of manual organization IDs, `CreateOrgId`, and organization `WHERE` clauses.
- [ ] Add CRUD, paging, and data-scope write-guard integration tests. Set restricted `IDataScopeContext` directly and prove cross-scope read, update, and delete are blocked without conflating missing permission with data-scope protection.
- [ ] Add `GET /api/v1/biz/customer/scope` with `[RolePermission]`. Return a structured DTO, not localized text; include the semantic range needed for frontend zh/en composition.

## P2: CRM Seeds

- [ ] Seed the fixed organization tree and customer counts: headquarters 214; South China 128 consisting of Shenzhen 42, Guangzhou 43, and Dongguan 43; North China Beijing 43 and Tianjin 43.
- [ ] Seed three non-super-admin trial users: headquarters `All`, South China `OrgAndChildren`, and Shenzhen `Org`. All use `DefaultModuleId=1000`, `MustChangePassword=false`, a non-null `LastPasswordChangeTime`, no phone binding, and `Enabled=true`.
- [ ] Seed CRM module `code=crm`, `ModuleId=1000`, default route, menu ownership, roles, role data scopes, user roles, and explicit GET permissions for customer page, detail, and scope endpoints.
- [ ] Keep consumer-owned fixed IDs in `1000-1999`, unique within each entity. Register idempotent `ISeedData` implementations and explicitly set `CreateOrgId` and `CreateUserId` for customer seeds.
- [ ] Prove two initializations of the same database do not drift. Prove the three accounts return `214`, `128`, and `42` from the same customer page endpoint and cannot see cross-scope details; retain P1 write-guard coverage.

## P3: Vue CRM Experience

- [ ] Build the CRM list and form in `web/`, including complete zh/en i18n, menu integration, and permission wiring. Do not add React.
- [ ] Compose the current range label from the scope DTO and the paging total. Do not hard-code account names or infer an original `ScopeType` from a merged context.
- [ ] Express all organizations, a root organization and descendants, one organization, specified organizations, and `IncludeSelf` consistently from the DTO in both locales.
- [ ] Verify generated API, type checking, linting, and real logins for the three accounts. Confirm the expected scope labels and totals `214 / 128 / 42`.

## P4: Public Demo And Narrative

- [ ] Obtain separate explicit authorization before replacing any production domain or deployment. P0-P3 do not authorize deployment.
- [ ] Configure public DemoMode, read-only CRM permissions, hidden write affordances, server-side write rejection, and a repeatable account-state recovery process.
- [ ] Provide the three trial accounts and a concise first-screen instruction showing how the same customer page changes by account.
- [ ] Capture the three-account comparison and add the real-app entry point to documentation.
- [ ] Verify health, login, customer paging, rollback, and the two-minute unauthenticated discovery path.

## Commit Policy

Each completed task uses its own English conventional commit. This application keeps CRM business work local. A possible kernel or satellite-package improvement requires independently documented dogfood evidence; it is not authorized merely by this ledger.
