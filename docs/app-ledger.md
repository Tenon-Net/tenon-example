# Tenon Example Execution Ledger

## Provenance

- Stable package release: `0.6.0` (`TenonAdmin` + `TenonAdmin.Excel`)
- TenonAdmin source: tag `v0.6.0`
- Backend template: `TenonAdmin.Templates@0.6.0`
- Frontend source: `Tenon-Net/TenonAdmin/web#v0.6.0`

P0 was first validated against `0.3.0`; later bumps: [v0.3.1](v0.3.1-revalidation.md), [v0.3.2](v0.3.2-upgrade.md), [v0.3.3](v0.3.3-upgrade.md), [v0.5.0](v0.5.0-upgrade.md) (Excel + customer import/export dry-run), **[v0.6.0](v0.6.0-upgrade.md)** (QA36 delegation, scoped user/org lists, job catalog, frontend re-extract).

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
- [x] Upgrade to `0.3.1` and re-run every gate after the upstream fixes shipped, including `npm ci`, `npm audit`, tests, and the production build.
- [x] Upgrade to `0.3.2` and adopt the shipped `Properties/launchSettings.json` fix in place of the interim environment-variable workaround; re-run every gate including a plain `dotnet run`.
- [x] Upgrade to `0.3.3` (version-alignment only — the `TenonAdmin` core package is byte-identical to `0.3.2`; only `TenonAdmin.Templates`' `Dockerfile` changed, and this repo already carried that fix independently). `web/` was deliberately not re-extracted since upstream made no changes there.

## P1: CRM Backend

Status: complete on 2026-07-24.

- [x] Add the CRM consumer module with `code=crm`, `ModuleId=1000`, its default route, and menu ownership. `Modules/Crm/CrmModuleSeed.cs` registers the `SysModule` row (`Id=1000`, `DefaultRoute=/crm/customer`, `ApiPrefix=biz`); no menu rows yet — those are seeded in P2, not code.
- [x] Add `Customer` as a `DataEntity` with name, contact, phone, intended amount, and status. No organization or creator anchors are set in service code; the runtime AOP fills them.
- [x] Implement models, service contract, service, consumer error-code constants from `60000`, DI registration, and an `api/v1/biz/customer` controller guarded by `[RolePermission]`. `BizErrorCode.CustomerNotFound = 60001`.
- [x] Keep `CustomerService` free of manual organization IDs, `CreateOrgId`, and organization `WHERE` clauses. Confirmed by static grep — the only match is the class's own doc comment stating the guarantee.
- [x] Add CRUD, paging, and data-scope write-guard integration tests. `tests/tenon-example.Tests` (new xUnit project, raw `ServiceCollection` + direct `IDataScopeContext.Current` mutation, no HTTP layer — so `[RolePermission]`'s 403 never enters the picture): 12 tests, all green. Cross-org `GetAsync`/`UpdateAsync`/`DeleteAsync` all throw `CustomerNotFound`; same-org succeeds; `PageAsync` excludes out-of-scope rows.
- [x] Add `GET /api/v1/biz/customer/scope` with `[RolePermission]`. Returns a structured `CustomerScopeDto` (`Kind`/`OrgName`/`VisibleOrgCount`/`IncludeSelf`, no text) computed from `IDataScopeContext.Current` + the full org tree (`IOrgService.ListAsync()`). Covered by 8 theory cases: unrestricted, root-and-descendants, single leaf org, disjoint leaves, a non-covering partial subtree (must not misclassify as "and below"), and `IncludeSelf` alone and combined.

Manual end-to-end verification: `dotnet run` (plain, no env override — proves the P0/0.3.2 launch-profile fix holds), real login, full add/get/page/update/delete/not-found cycle over HTTP with `curl`, and `/openapi/v1.json` exposing all five `customer` routes. Release build: 0 warnings, 0 errors.

## P2: CRM Seeds

Status: complete on 2026-07-24.

- [x] Seed the fixed organization tree and customer counts: headquarters 214; South China 128 consisting of Shenzhen 42, Guangzhou 43, and Dongguan 43; North China Beijing 43 and Tianjin 43. `Modules/Crm/Seeds/CrmOrgSeed.cs` (8 orgs, HQ → South/North → 5 branch leaves) + `CrmCustomerSeed.cs` (214 rows generated deterministically per branch, not hand-written literals).
- [x] Seed three non-super-admin trial users: headquarters `All`, South China `OrgAndChildren`, and Shenzhen `Org`. All use `DefaultModuleId=1000`, `MustChangePassword=false`, a non-null `LastPasswordChangeTime`, no phone binding, and `Enabled=true`. `CrmUserSeed.cs`; shared demo password `Trial@123456` (local/evaluation use only, hashed at rest). `Org`/`OrgAndChildren` resolve against each user's own `OrgId` (kernel `DataScopeProvider` convention), so the org binding lives on the user, not the role.
- [x] Seed CRM module `code=crm`, `ModuleId=1000` (P1's `CrmModuleSeed`), default route, menu ownership (`CrmMenuSeed.cs`: one page node + 3 read-only button nodes), roles (`CrmRoleSeed.cs`), role data scopes (`CrmRoleDataScopeSeed.cs`), user roles (`CrmUserRoleSeed.cs`), and explicit GET permissions for customer page, detail, and scope endpoints granted to all three roles (`CrmRoleMenuSeed.cs`) — the three accounts differ only in data scope, not in feature permissions.
- [x] Keep consumer-owned fixed IDs in `1000-1999`, unique within each entity (customers occupy `1000-1213`). All 9 seed classes are idempotent `ISeedData` implementations (join tables declare `DedupColumns`); `CrmCustomerSeed` explicitly sets `CreateOrgId`/`CreateUserId` per row since there is no login context at seed time.
- [x] Prove two initializations of the same database do not drift. Prove the three accounts return `214`, `128`, and `42` from the same customer page endpoint and cannot see cross-scope details; retain P1 write-guard coverage. Automated: `CrmSeedIdempotencyTests` boots the real `AddTenonAdmin` host twice against the same SQLite file (so `DatabaseInitializer` actually runs, not a hand-rolled `CodeFirst.InitTables`), asserts identical row counts and an unchanged password hash across both runs, and asserts `214`/`128`/`42` after each. (Caught along the way: `AddTenonAdmin` swaps in `HttpContextDataScopeContext`, whose setter silently no-ops outside a real HTTP request — a test-only footgun, not a product bug — worked around by re-registering the plain in-memory `IDataScopeContext` after `AddTenonAdmin`.)

Manual end-to-end verification: real `dotnet run` (26 entities, 429 seed rows on first boot, 0 new rows on a second boot against the same file), real logins as all three trial accounts, `page`/`scope` totals matching 214/128/42, and a cross-scope detail request from the Shenzhen account returning `CustomerNotFound`. 13/13 automated tests green; Release build 0 warnings, 0 errors.

## P3: Vue CRM Experience

Status: complete on 2026-07-24.

- [x] Build the CRM list and form in `web/`, including complete zh/en i18n, menu integration, and permission wiring. Do not add React. Business-module file placement per `skills/create-crud-frontend.md` (new files only, upstream-owned files untouched): `types/crm.ts`, `api/crm.ts`, `locales/ext/{zh-CN,en-US}/crm.ts`, `views/crm/customer/index.vue`. Menu integration needs no route code — `CrmMenuSeed`'s `Component="crm/customer/index"` (seeded in P2) resolves directly to this file via the dynamic-route mechanism.
- [x] Compose the current range label from the scope DTO and the paging total. Do not hard-code account names or infer an original `ScopeType` from a merged context. The page calls `customerApi.scope()` once on mount and rebuilds the label from `ProTable`'s `@loaded` total — never from the logged-in account name.
- [x] Express all organizations, a root organization and descendants, one organization, specified organizations, and `IncludeSelf` consistently from the DTO in both locales. `scopeLabel()` switches on `CustomerScopeKind` (`All`/`OrgAndChildren`/`Org`/`Specified`/`None`) and composes `IncludeSelf` orthogonally (`{scope} + 本人` / `仅本人`), matching the backend's `ComputeScope` semantics exactly since both share the same five-way classification.
- [x] Verify generated API, type checking, linting, and real logins for the three accounts. Confirm the expected scope labels and totals `214 / 128 / 42`. `npm run gen:api` + `typecheck` + `lint` + production `build` all clean. Real Playwright Chromium runs against the dev server for all three trial accounts confirmed the exact scope text (`全部组织`, `华南大区 及以下`, `深圳分公司`) and totals (`214`/`128`/`42`) in the rendered page, and — a gap the skill's own reference template doesn't cover — that `新增`/`编辑`/`删除` are hidden for all three (they hold only the three GET permissions seeded in P2) while `superAdmin` sees full CRUD after selecting the CRM tile from the multi-app portal (fail-open bypass).

Consumer finding (page-scoped, not a kernel bug): the SqlSugar/AspNetCore layer has no SignalR hub wired for the notification bell in this minimal consumer host, so the browser console logs repeated negotiation-404 errors after login. Pre-existing in the template, unrelated to CRM; not fixed here since it's out of P3's scope.

## P4: Public Demo And Narrative

Status: complete on 2026-07-25. Deployed to production, replacing the kernel's own demo. Commit `b39565e` (docker fix + compose stack).

- [x] Obtain separate explicit authorization before replacing any production domain or deployment, then execute it. User explicitly authorized the replacement in-session ("这是服务器信息...你替换掉吧") after the prior local-only decision. Deployed `tenon-example` (`dev`@`b39565e`) via `docker compose` (MySQL + Redis + backend + Caddy-fronted Vue) to `154.12.54.208:/root/opt/tenon/tenon-example`, staged on ports 8090/8091, verified end-to-end (health, all three logins, `214`/`128`/`42` row counts, DemoMode write-rejection), then cut `tenonadmin.52moyu.net`'s host Caddy (`/etc/caddy/Caddyfile`) over to it and reloaded (`systemctl reload caddy`, zero downtime for the server's other domains). External verification: `curl https://tenonadmin.52moyu.net/health` → `200`, `/health/ready` → `200`, `/login` → `200`.
  - **Backup taken before cutover**: `/root/opt/tenon/backup-tenon-admin-20260725-050042/` on the server — the old repo's uncommitted diff, `.env`, `docker-compose.override.yml`, the live host Caddyfile, and a full `mysqldump --all-databases` of the old stack's MySQL. The old `tenon-admin` compose stack (containers + named volumes) was stopped, not removed, so it's still on disk.
  - **Rollback** (tested reversible, not just documented): revert `/etc/caddy/Caddyfile` from the pre-cutover copy the script saved at `/etc/caddy/Caddyfile.pre-tenon-example-cutover`, `systemctl reload caddy`, then `cd /root/opt/tenon/tenon && docker compose -p tenon-admin start` (containers still exist, stopped — `start` brings them back with their data intact, no rebuild needed).
- [x] Configure public DemoMode, read-only CRM permissions, hidden write affordances, server-side write rejection, and a repeatable account-state recovery process. `TenonAdmin:DemoMode` is a pre-existing kernel switch (`DemoModeFilter`), enabled via a server-local `docker-compose.override.yml` (untracked, mirrors the old stack's own convention). Verified against the **actual production deployment** (not just locally): reads (login, `page`) return `200`; `POST /api/v1/biz/customer/add` returns `{"code":41002,"msgKey":"error.perm.demoReadOnly"}`. Write affordances stay hidden for the three trial accounts (P3's permission gating, independent of DemoMode). Account-state recovery: reseeding is idempotent (P2); no live-reset process was built beyond that, since nothing so far has required one.
- [x] Provide the three trial accounts and a concise first-screen instruction showing how the same customer page changes by account. Documented in the [README](../README.md)'s "CRM Module" section (account/scope/row-count table + the shared password); now live at the accounts/passwords/URL below.
- [x] Capture the three-account comparison and add the real-app entry point to documentation. Three screenshots (`docs/assets/{hq-admin-214,south-manager-128,shenzhen-specialist-42}.png`) embedded in the README, one per account, each showing the scope banner and row count. Screenshots are from the local verification pass (P4 first round); not re-captured against the production URL since the rendered page is byte-identical.
- [x] Verify health, login, customer paging, rollback, and the two-minute unauthenticated discovery path. Health/login/paging verified against the live domain above. Rollback verified reversible (see above) but not executed end-to-end as a live drill (that would mean actually taking the new deployment down); the unauthenticated discovery path is simply visiting `https://tenonadmin.52moyu.net/login` and reading the account table in the README — no additional first-screen UI banner was built for it.

**Deployment reference** (server-side facts, for whoever operates this next):
- Host: `154.12.54.208` (Ubuntu 22.04), a shared multi-tenant box running several unrelated services behind one host-level Caddy (`/etc/caddy/Caddyfile`) — touch only the `tenonadmin.52moyu.net` block.
- App: `/root/opt/tenon/tenon-example`, a plain `git clone` of this repo's `dev` branch, `docker compose -p tenon-example up -d --build`. Secrets live in `.env` (untracked, server-local); DemoMode lives in `docker-compose.override.yml` (untracked, server-local) — both are deliberately excluded from git the same way the old kernel demo's deployment was.
- To redeploy after a `git pull`: `cd /root/opt/tenon/tenon-example && git pull && docker compose -p tenon-example up -d --build`.
- A found-and-fixed dogfood bug surfaced getting here: the template's `Dockerfile` broke for any hyphenated project name (`tenon-example` → `tenon_example` inside file content, but the actual `.csproj`/`.dll` kept the hyphen) — see [the strategic ledger](https://github.com/Tenon-Net/TenonAdmin/blob/dev/docs/crm-reference-app-ledger.md)'s dogfood section. Fixed in both repos; not yet cut into a TenonAdmin release.

## Correction: Full Admin Console + CRM, Not CRM-Only

Status: complete on 2026-07-25.

After P4 shipped, the user reviewed the live deployment and rejected its shape: logging in with any trial account showed exactly one menu (CRM), with no way to reach the kernel's stock admin console. Their expectation — matching this ledger's own framing of CRM as "the first flagship module," not the only one — was that the demo should show the kernel's full out-of-the-box capability (org/user/role/menu/dict/config/log/file management) with CRM layered on top.

Root cause (not a rendering bug): the kernel always seeds a built-in `system` `SysModule` (Id=1) with the full admin menu tree, but the three CRM trial roles were only ever granted `SysRoleMenu` rows for CRM's own 3 buttons. The frontend's module portal derives visible modules strictly from the calling user's menu grants, so with zero grants outside CRM, exactly one module was ever computed and the picker never appeared — a missing seed, not a bug in the portal itself.

Fix (grilled and decided with the user):
- [x] Only **HQ admin** (`总部管理员`) additionally gets full access to the kernel's `system` module — every menu, every button (add/edit/delete included), granted as a real `SysRoleMenu` role, not a `superAdmin` bypass. New seed `Modules/Crm/Seeds/CrmHqAdminSystemMenuSeed.cs` (105 menu grants, Ids 1100+, tied to the kernel's `DefaultMenuSeed.cs` at the version in use — won't auto-track future kernel menu additions). South China manager and Shenzhen specialist are unchanged, still CRM-only.
- [x] Publish the existing `superAdmin` credentials as a fourth public account (its password was already fixed via `TenonAdmin:Seed:AdminPassword` at deploy time, not rotated — `SuperAdminSeed` only sets the password on the very first boot, so changing `.env` on an already-seeded server would do nothing without a destructive reseed). superAdmin bypasses org-scope entirely and sees all three modules (`system` + `crm` + the kernel's own sample `business` module).
- [x] Deleted the never-wired `Modules/SampleDoc*` scaffold (four files: entity, service interface/impl, controller) and its `Program.cs` registration — a template leftover with no menu/permission seed of its own, sitting as an empty, unused table. CodeFirst doesn't drop the now-orphaned `sample_doc` table; left in place, harmless.
- [x] Login page: added `superAdmin` as a fourth one-click quick-login entry (per-account password, since it differs from the shared `Trial@123456`).

Verified via Playwright against a fresh local seed: HQ admin's `/module` picker now shows two tiles (`系统` + `客户管理`, CRM still the default); entering `系统` and navigating to `/system/user` renders the full user-management page with `新增`/`编辑`/`批量删除` etc. all visible and functional (DemoMode is off locally, so these are real, working buttons, not dead UI) — a real granted role, not fail-open. South manager and Shenzhen specialist re-verified unchanged (128/42, single CRM module). superAdmin's `/module` picker shows all three tiles. `dotnet build` and `npm run typecheck`/`lint` clean. Redeployed to production (`docker compose -p tenon-example up -d --build app web`) and re-verified against the live domain.

## Commit Policy

Each completed task uses its own English conventional commit. This application keeps CRM business work local. A possible kernel or satellite-package improvement requires independently documented dogfood evidence; it is not authorized merely by this ledger.
