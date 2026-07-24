# P0 Consumer Bootstrap Validation

Date: 2026-07-24

## Release Evidence

- Official NuGet v3 feeds reported the latest stable versions of both `TenonAdmin` and `TenonAdmin.Templates` as `0.3.0`.
- TenonAdmin tag `v0.3.0` dereferences to commit `ac84cad325bb808e67321b7b1b4c8b37d6fa94bd`.
- `tenon-example.csproj` contains the exact package reference `TenonAdmin` `0.3.0`.
- `web/package.json` reports `0.3.0`, and `web/` was extracted with `npx degit Tenon-Net/TenonAdmin/web#v0.3.0 web`.
- Public consumer repository: `https://github.com/Tenon-Net/tenon-example`.

## Primary Repository Results

| Gate | Evidence |
| --- | --- |
| Restore | `dotnet restore` exited 0. |
| Release build | Exited 0 with 0 warnings and 0 errors. |
| First SQLite startup | CodeFirst created 25 entities; first-run seed inserted 181 rows; the SQLite database became ready. |
| Random administrator credential | The first-run banner created `superAdmin` and printed a one-time random password. The password is intentionally not recorded. |
| Real HTTP login | `POST /api/v1/auth/login` returned envelope code `0` and issued access and refresh tokens. Tokens are intentionally not recorded. |
| Health | `/health` and `/health/ready` returned HTTP 200 with `Healthy`. |
| OpenAPI | `/openapi/v1.json` returned HTTP 200 and exposed the login endpoints plus the consumer sample endpoints. |
| Dependencies | `npm install` added 272 packages and completed successfully. |
| API generation | `npm run gen:api` generated `web/src/api/schema.d.ts` from the running consumer host. |
| Type check | `npm run typecheck` exited 0. |
| Lint | `npm run lint` exited 0. |
| Browser login | Playwright Chromium submitted the real Vue login form and navigated from `/login` to `/module`. |

The desktop-control CLI was not installed in the execution environment, so the real browser gate used the repository's Playwright dependency. Backend and frontend validation were run sequentially except for the bounded end-to-end browser session, where both servers are necessarily online.

## Clean-Room Reproduction

A disposable empty sibling directory repeated the README path:

1. Installed exact template package `TenonAdmin.Templates::0.3.0`.
2. Generated a fresh `tenon-app` consumer.
3. Extracted `web/` from `v0.3.0`.
4. Restored and built Release with 0 warnings and 0 errors.
5. Reproduced CodeFirst, random-admin creation, HTTP 200 health, and HTTP 200 OpenAPI.
6. Repeated `npm install`, `npm run gen:api`, `npm run typecheck`, and `npm run lint` successfully.

## Consumer Findings

1. `dotnet new tenon-app` reports that no project is configured for the template's restore post-action, even though the generated project restores and builds manually. This is a reusable template bootstrap issue.
2. .NET 10 warns that the `Package::version` install separator is deprecated in favor of `Package@version`. The exact version still installs correctly.
3. `npm install` consistently reports five advisories: three high and two moderate. They affect transitive `@redocly/openapi-core`, `brace-expansion`, and `js-yaml`, plus direct `echarts` and `vue-echarts`; fixes are reported as available. No release-pinned dependency was changed during P0.
4. npm reports deferred install-script approval for `esbuild@0.25.12` and `vue-demi@0.13.11`; API generation, type checking, linting, and Vite still completed in this environment.
5. The exact `v0.3.0` frontend source has three `git diff --check` findings: trailing whitespace in two design HTML files and a blank line at EOF in `src/views/personal/password.vue`.

Reusable template findings are tracked upstream; application-specific P1–P4 work remains in this repository.
