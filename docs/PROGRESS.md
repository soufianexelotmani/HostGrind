# HostGrid implementation progress

## Phase 1 · Foundation · 2026-11-25

Read all eight files in `HostGrid-Codex-Pack` before editing. The initial Git working tree was clean. The existing solution, single API project, Angular standalone app, and npm lockfile were reused.

| Area | Result |
|---|---|
| Backend toolchain | .NET SDK 11.0.100 built the existing `net10.0` target. ASP.NET Core runtime 10.0.15 ran the API. For new workstations, the README calls out the matching .NET 10 SDK. |
| Frontend toolchain | Angular core 21.2.26, Angular CLI 21.2.31, TypeScript 5.9.3 and npm 11.20.0. Host Node 26.13.1 is outside Angular 21's supported range. Used a checksum-verified Node 24.15.0 Windows ZIP under the ignored `.local-tools/` directory. Used `npm.cmd` to work around the PowerShell execution policy. |
| SQL Server | The default SQL Server service was stopped and Docker was unavailable. LocalDB 17.0.4025.3 initially failed to start or connect under the restricted shell. The per-user instance started with local access and passed `SELECT 1`. The idempotent local setup command ensured the empty `HostGridDb` database exists. |
| API foundation | Standard ASP.NET health middleware exposes liveness and SQL readiness. The latter runs a read-only query and returns aggregate status. Added ProblemDetails, Development-only Scalar alongside OpenAPI, and a User Secrets ID. Removed the generated weather sample. |
| Angular foundation | Development `/api/**` requests forward to `http://localhost:5025`. The generated Angular view remains the startup shell for Phase 1. |
| Structure | Infrastructure health code lives in `backend/Health/`. Domain controllers, services, repositories, entities, DTOs, mappers and validators will be added with their respective features. |
| SQL migrations | Phase 2 will introduce the EF Core SQL Server model, migrations and seed. Phase 1 uses SqlClient solely for a database connectivity probe. |

The added NuGet packages are pinned and locked. Their package metadata lists the MIT license. [SqlClient 6.1.8](https://www.nuget.org/packages/Microsoft.Data.SqlClient/6.1.8) · [Scalar.AspNetCore 3.2.0](https://www.nuget.org/packages/Scalar.AspNetCore/3.2.0)

### Commands and verification

| Commands or checks | Observed result |
|---|---|
| `git status --short`, `git ls-files`, `rg --files`, `Get-Content`, `dotnet --list-sdks`, `dotnet --list-runtimes`, `dotnet tool list -g`, `node -v`, `npm.cmd -v`, `npm.cmd ls --depth=0 --prefix frontend`, `Get-Command`, `Get-Service '*SQL*'` | Inspected specifications, app configuration, dependencies and tooling. |
| `SqlLocalDB info`, `SqlLocalDB start MSSQLLocalDB`, `sqlcmd` with `SELECT 1`, conditional `CREATE DATABASE` and `SELECT DB_NAME()` | LocalDB running and `HostGridDb` reachable. |
| `curl.exe`, `Invoke-WebRequest`, `Get-FileHash`, `Expand-Archive` | Verified official registry access and bootstrapped Node 24 from the official archive. The first Node download URL failed and the explicit version succeeded. |
| `dotnet build` (baseline), `dotnet restore`, `dotnet build --no-restore`, `dotnet restore --locked-mode` | Passed. Phase 1 API build had zero warnings and errors. |
| `dotnet test --no-build` | Exit 0. The solution has zero .NET test projects. The backend was verified through live HTTP checks. |
| `npm.cmd ci`, `npm.cmd run build` | Passed on Node 24.15.0. npm reported install-script approval warnings for four packages. |
| `npm.cmd test -- --watch=false` | Passed. Vitest ran two tests in one file. |
| `dotnet HostGrid.Api.dll --urls …`, `node node_modules/@angular/cli/bin/ng.js serve`, PowerShell `Invoke-WebRequest` | API liveness, SQL readiness, OpenAPI, Scalar, Angular startup and both proxied health routes returned 200. A second API process without a SQL connection string returned 200 for liveness and 503 for readiness. Smoke processes were stopped afterward. |
| `dotnet list backend/HostGrid.Api.csproj package --vulnerable --include-transitive`, `npm.cmd audit --omit=dev`, `npm.cmd audit --json` | NuGet and npm production audits reported zero vulnerabilities. Full npm audit reported 16 moderate findings in the existing Angular CLI development tree. Its proposed fix requires Angular CLI 22. Angular 21 remains the specified version. |
| `git diff --check` | Passed. |

### Changed files

- Added `README.md`, `docs/PROGRESS.md`, `backend/Health/SqlServerHealthCheck.cs`, `backend/packages.lock.json`, `frontend/.nvmrc`, and `frontend/proxy.conf.json`.
- Modified `.gitignore`, `backend/HostGrid.Api.csproj`, `backend/HostGrid.Api.http`, `backend/Program.cs`, and `frontend/angular.json`.
- Removed the generated sample files `backend/Controllers/WeatherForecastController.cs` and `backend/WeatherForecast.cs`.

The next checkpoint is user approval for Phase 2. Work was performed locally.

## Phase 2 · Database and entities · 2026-11-25

Phase 1 was approved. The persistence model and initial migration were verified against SQL Server LocalDB 17.0.4025.3. The API remains one application project. The xUnit project is separate. Angular 21 remains on its existing lockfile and was verified using Node 24.15.0. This workstation has .NET SDK 11.0.100 and ASP.NET Core runtime 10.0.15. The .NET 10 SDK is documented in the README, and SDK selection remains unpinned.

### Migration and seed results

- `20261125223356_InitialHostGrid` applied successfully to the existing `HostGridDb` LocalDB catalog. The catalog had zero user tables before the migration.
- SQL Server reports 60 HostGrid entity types, five framework Identity support tables, and one EF migration history table for 66 physical tables. The eleven module schemas are `identity`, `organization`, `property`, `booking`, `finance`, `expense`, `operation`, `document`, `communication`, `reporting`, and `system`. Migration history uses `dbo`.
- Catalog inspection found 249 indexes including key-backed indexes, 142 foreign keys, 98 check constraints, and two active closure-protection triggers. The generated forward SQL was reviewed for schema creation, keys, filtered indexes, seed inserts and triggers. It contains no table drops or truncates.
- Verified three role names, six property types and zero accounts in `HostGridDb`. Optional demonstration seeding was exercised in the test catalog. Running it twice resulted in one fictitious organization, one settings row, and twelve expense categories.

### Verification

| Commands or checks | Observed result |
|---|---|
| `dotnet --list-sdks`, `dotnet --version`, `dotnet restore`, `dotnet tool restore`, `dotnet restore --locked-mode`, `dotnet build --no-restore` | Restores and build passed. The build reported zero warnings and errors. EF CLI, EF Core and Identity EF are pinned to 10.0.15. |
| `dotnet ef migrations add InitialHostGrid --project backend --output-dir Data/Migrations`, `dotnet ef migrations script --no-build --project backend --idempotent` | Migration, snapshot, and review SQL generated. Forward SQL was reviewed before application. |
| `dotnet test --no-restore` | xUnit discovered and passed 13 tests on a separately created LocalDB catalog. Tests covered the migration, required fields, tenant filtering and write guards, cross-tenant relationships, financial precision, direct-net shape, uniqueness, closure locks, and idempotent seeding. |
| `dotnet ef migrations list --project backend --no-build`, `dotnet ef database update --project backend --no-build`, `dotnet ef migrations has-pending-model-changes --project backend --no-build`, `sqlcmd` metadata checks | Migration listed as pending before update and applied afterward. The model matched the snapshot. SQL metadata and key unique indexes were verified in the target catalog. |
| `npm.cmd ci`, `npm.cmd run build`, `npm.cmd test -- --watch=false` | Passed on Node 24.15.0. Vitest reported 2 of 2 tests passed. |
| HTTP probes via `Invoke-WebRequest` | Angular root, OpenAPI, Scalar, and direct and proxied health checks returned 200. With SQL configuration absent, API liveness returned 200 and readiness returned 503. |
| `dotnet list package --vulnerable --include-transitive`, `npm.cmd audit --omit=dev`, npm audit during `npm.cmd ci` | NuGet and npm production checks reported zero vulnerabilities. The existing Angular CLI development tree still reports 16 moderate findings. The previous audit's proposed fix requires upgrading Angular CLI to 22. No forced audit fix was run. |
| `git diff --check` | Passed. |

Direct dependency licenses were verified in NuGet package metadata. Microsoft EF Core, Identity EF, the EF tool, and the test SDK use MIT. xUnit v3 and its VSTest runner use Apache-2.0.

### Files

- Added the pinned EF tool manifest at `.config/dotnet-tools.json`.
- Added eleven entity files under `backend/Entities/`, eleven module mapping files under `backend/Data/Configurations/`, and `backend/Enums/PersistenceEnums.cs`.
- Added `backend/Data/HostGridDbContext.cs`, `backend/Data/HostGridDbContextFactory.cs`, `backend/Data/Seed/ReferenceData.cs`, `backend/Data/Seed/DemoData.cs`, and `backend/Security/ITenantContext.cs`.
- Added the migration, designer, and snapshot in `backend/Data/Migrations/`.
- Added the xUnit project, SQL fixture, tests, and lockfile in `tests/HostGrid.Persistence.Tests/`.
- Added `docs/DATABASE.md` and `docs/ERD.md`. Updated `README.md`, this log, `HostGrid.slnx`, `backend/Program.cs`, `backend/HostGrid.Api.csproj`, and `backend/packages.lock.json`.

### Next checkpoint

Phase 3 requires explicit approval. Membership resolution, owner authorization and audited administrator access belong to that phase. SQL enforces row-level and relational invariants described in the [database guide](DATABASE.md). The booking overlap lock and settlement workflows will be implemented and tested in their respective service phases. The guide lists the remaining financial decisions that require approval before those calculations are implemented.

## Phase 3 · Authentication and authorization · 2026-11-25

Phase 2 was approved. The existing single ASP.NET Core application, SQL Server catalog, and Angular 21 project were reused. Angular ran on Node 24.15.0. The workstation still has the .NET 11 SDK and .NET 10 runtime. The .NET 10 SDK remains the recommended matching prerequisite.

Identity now supports invitation and activation, password login and lockout, reset and change, TOTP enrollment and verification, one-time recovery codes, and authenticated MFA reset. JWT access tokens expire after 15 minutes. Seven-day cookie refresh tokens rotate under SQL Server account locks. Reuse revokes the family. JWT validation resolves account status, active session, security stamp, and memberships on the server. Route scope and property ownership are enforced server-side. Super Admin paths explicitly authorize global administration and audit reads. The Angular client uses memory-only access tokens, session restoration, coordinated refresh, guards, role routes, and Terracotta authentication screens with English, French and Arabic copy.

Migration `20261125225143_AuthenticationSecurity` adds `identity.LoginChallenge`, `system.SecurityEvent`, and two `identity.RefreshToken` columns. The reviewed forward SQL added tables, indexes and columns. EF applied it to the existing LocalDB `HostGridDb` with the Phase 2 migration intact. EF reports no pending model changes.

| Verification | Result |
|---|---|
| `dotnet restore --locked-mode` and `dotnet build --no-restore` | Passed. |
| `dotnet test --no-build --no-restore` | 26 SQL Server tests passed, including 13 Phase 2 tests and 13 authentication tests. Coverage includes activation and password changes, reset expiry and reuse, lockout, expired and invalid JWTs, refresh concurrency and family revocation, mandatory MFA and code reuse, Origin and CSRF checks, owner isolation, and administrator and membership policies. |
| `dotnet ef migrations add AuthenticationSecurity`, `dotnet ef migrations script`, `dotnet ef database update`, `dotnet ef migrations list`, and `dotnet ef migrations has-pending-model-changes` | Incremental SQL generated, reviewed and applied. Both migration IDs are present. |
| `npm.cmd ci`, `npm.cmd run build`, and `npm.cmd test -- --watch=false` | Passed. Vitest ran six tests covering the route shell, Signals state, guards, bearer headers, CSRF headers, and coordinated refresh. |
| Live HTTP smoke with LocalDB, Mailpit 1.34.0 and Angular dev proxy | Passed with a fictitious account in a disposable catalog. Verified SMTP invitation and activation, required MFA, current user, admin access, refresh, logout, denied CSRF request, and both health routes. The live test found a content-root difference between bootstrap and API processes. A stable Data Protection application name resolved it. |
| NuGet audit, npm production audit, and npm install audit | Zero NuGet and npm production findings. The Angular CLI development dependency tree still reports 16 moderate findings. Preserved the Angular lockfile and version. |
| `git diff --check` | Passed. |

### Phase 3 files

- Added `backend/Controllers/{AuthController,AccountsController}.cs`, `backend/DTOs/Auth/AuthModels.cs`, `backend/Extensions/SecurityRegistration.cs`, `backend/Integrations/Email/SecurityEmail.cs`, `backend/Mappers/CurrentUserMapper.cs`, `backend/Middlewares/TenantSecurityMiddleware.cs`, `backend/Options/SecurityOptions.cs`, `backend/Repositories/AuthenticationRepository.cs`, and `backend/Validators/SecurityValidation.cs`.
- Added `backend/Security/{AuthenticatedTenantContext,ResourceAuthorization,SecurityExceptionHandler,SecurityRequestException}.cs` and `backend/Services/{AccountManagementService,AccountSecurityService,AuthenticationService,CurrentUserService,SecurityAuditService,TokenService}.cs`.
- Added `backend/Data/Configurations/AuthenticationConfigurations.cs` and the incremental migration and designer. Updated the snapshot, Identity configuration and entities, context, API registration, API project file, and NuGet lockfile.
- Added `frontend/src/app/core/auth/` and `frontend/src/app/features/auth/`. Updated the Angular config, routes, shell, app test, root styles and index title.
- Added `tests/HostGrid.Persistence.Tests/AuthenticationTests.cs`. Updated the SQL fixture, migration assertions, test project file and NuGet lockfile.
- Added `docs/AUTHENTICATION.md` and `docs/SECURITY.md`. Updated the README and this progress log.

The [authentication guide](AUTHENTICATION.md) lists the endpoints and setup commands. The [security guide](SECURITY.md) describes deployment assumptions, the recovery-code requirement, and later operational work. Reservation locking and financial decisions remain documented in [DATABASE.md](DATABASE.md). Phase 4 awaits explicit approval.

## Phase 4 · Design system and application layouts · 2026-11-25

Phase 3 was approved. The existing Angular 21 application and authenticated API were reused. Phase 4 changes are frontend and documentation only. The single .NET API, migrations, and authentication behavior are preserved.

- Added role-specific dashboard shells, a persistent desktop sidebar, bottom navigation and More drawer, breadcrumbs, profile and logout controls, notification entry, and a Ctrl+K or Cmd+K navigation palette. Upcoming destinations are labeled and excluded from search.
- Redesigned the existing authentication forms around the original HostGrid logo and a generated riad image. PrimeNG, Tailwind CSS 4, Lucide, and Angular CDK support the shared control set. Transloco serves the existing authentication copy and new shell dictionaries through one loader. The browser stores only language, theme mode, and sidebar state.
- Added all requested shared component foundations with typed inputs and outputs. Table requests are emitted for the caller to fetch. Dashboard panels show empty states until their functional phases supply data.

### Dependencies

| Package | Pinned version | License |
|---|---|---|
| PrimeNG Community | 21.1.10 | MIT |
| @primeuix/themes | 2.0.3 | MIT |
| Angular CDK | 21.2.14 | MIT |
| Tailwind CSS and @tailwindcss/postcss | 4.3.5 | MIT |
| @lucide/angular | 1.80.0 | ISC |
| @jsverse/transloco | 8.4.0 | MIT |
| Fontsource Manrope Variable and Tajawal | 5.3.0 | OFL-1.1 |
| @playwright/test | 1.65.1 | Apache-2.0 |

### Verification

| Commands or checks | Observed result |
|---|---|
| `dotnet build --no-restore`, `dotnet test --no-build --no-restore` | Passed. Build had zero warnings and errors. All 26 SQL Server tests passed. |
| `npm.cmd ci`, `npm.cmd run build`, `npm.cmd test -- --watch=false` | Angular 21 and Node 24 verified. All 12 Vitest tests passed. The initial production bundle is 498.23 kB, just under the 500 kB warning budget. |
| `npm.cmd run test:e2e` with Edge | All 3 Playwright layout tests passed. Covered five viewport widths, three role fixtures, English/French/Arabic, light/dark, keyboard palette, Escape, password visibility, forbidden routes, and page overflow. |
| Live Edge, API, Mailpit, SQL Server and Angular proxy | Verified SMTP invitation and activation, TOTP over HTTP, real browser login and MFA with a recovery code, refresh and reload restoration, logout, CSRF rejection and both health endpoints. Used a disposable local database and fictitious account. |
| Token contrast and visual review | Sampled text and button contrast ranged from 6.44:1 to 14.66:1. Interactive outlines measured 3.71:1 or higher. Browser review found and corrected Escape behavior and the primary-link text color. Four captures are linked in the UI guide. |
| NuGet audit, npm production audit and full npm install audit | Zero NuGet and npm production findings. The existing Angular CLI dependency tree reports 16 moderate development findings. Angular 21 and locked dependencies were preserved. |
| `git diff --check` | Passed. |

### Phase 4 files

- Added `frontend/.postcssrc.json`, `frontend/playwright.config.ts`, and `frontend/e2e/workspace.spec.ts`. Updated `frontend/.gitignore`, `frontend/angular.json`, `frontend/package.json`, and `frontend/package-lock.json`.
- Added `frontend/public/brand/{logo.svg,mark.svg,riad.jpg,manrope-OFL.txt,tajawal-OFL.txt}`. Replaced `frontend/src/styles.scss` with `frontend/src/styles.css` and updated the favicon in `frontend/src/index.html`.
- Updated `frontend/src/app/{app.config.ts,app.routes.ts}` and the auth page TypeScript, template, and SCSS. Added `frontend/src/app/core/config/{branding.ts,navigation.ts,preferences.ts,preferences.spec.ts,theme.ts,ui-translations.ts}`, the app and auth layouts, and `frontend/src/app/features/dashboard/dashboard.ts`.
- Added `frontend/src/app/shared/components/{data-table.ts,data-table.spec.ts,feedback.ts,form-elements.ts,modal.ts,page-elements.ts,preference-controls.ts}` and `frontend/src/app/shared/utils/formatters.ts`.
- Added `docs/UI_GUIDE.md` and four files in `docs/screenshots/`. Updated the README and this log.

Dashboard KPIs, notifications, business search, chart and calendar packages, and server-backed branding remain scheduled for later phases. Account recovery without an authenticator or recovery code, the matching .NET 10 SDK, and finance and reservation decisions remain tracked in the [security](SECURITY.md) and [database](DATABASE.md) guides. Phase 5 awaits explicit approval.

## Phase 5 · Checkpoint 5.1 Backend stabilization · 2026-11-26

The recovery audit was approved. A focused LocalDB reproduction returned HTTP 500. Test-only console capture confirmed an EF Core `InvalidOperationException` translating the search predicate over a constructor-projected `PropertySummary`. The stack reached `CountAsync` in `PortfolioRepository.PageAsync`. Property and owner projections now bind DTO members explicitly, allowing SQL Server to filter, sort, count and page while preserving tenant and Owner scoping and response fields.

Changed `backend/DTOs/Properties/PortfolioModels.cs`, `backend/Repositories/PortfolioRepository.cs`, and `tests/HostGrid.Persistence.Tests/AuthenticationTests.cs`, plus this log and the recovery report. The original journey now verifies filtered Manager and Owner portfolio pages, invitation, activation, property access restrictions and cross-tenant rejection. The new Owner-directory regression checks search, descending sort, two pages, tenant exclusion and detail lookup.

Reviewed the generated forward SQL of `20261125234521_PropertyPortfolio` and checked the target for seed conflicts. Applied it to the existing `HostGridDb`. Verified nine seeded amenities, new nullable and defaulted columns, the filtered unique Owner index, and intact, trusted Owner foreign keys. Both prior migration entries remain, and EF reports no model drift.

| Check | Result |
|---|---|
| `dotnet restore --locked-mode` and `dotnet build --no-restore` | Passed. Build reported zero warnings and errors. |
| Focused and full `dotnet test --no-build --no-restore` | **2 of 2** focused and **30 of 30** full passed, including all 26 pre-Phase-5 tests. |
| `npm.cmd run build` and `npm.cmd test -- --watch=false` on Node 24 | Build passed and **12 of 12** unit tests passed. Initial bundle **500.31 kB** with a **315-byte** warning overage. Existing NG8113 also reported. |
| Existing `npm.cmd run test:e2e` in Edge | **3 of 3** passed. The earlier cold-start timeout did not recur against the running dev server. |
| Direct and proxied HTTP health, EF migration list and snapshot check, `git diff --check` | Passed. |

The [updated recovery assessment](PHASE_05_RECOVERY.md) maintains the Phase 5 classifications. Recommended Checkpoint 5.2 covers owner account lifecycle and property management acceptance. It awaits explicit approval.

## Phase 5 · Checkpoint 5.2 Owner lifecycle and property management acceptance · 2026-11-26

The Manager and explicitly scoped Super Admin can use `POST /api/v1/organizations/{organizationId}/owners/{id}/enable-access` to restore a linked Owner membership. The related `disable-access` operation uses the same profile and membership checks. Both use the existing Identity security stamp and refresh-session revocation. Reactivation requires a fresh session and rejects archived profiles and activated accounts disabled at platform level. The existing invitation endpoint rotates pending activation proofs under the per-account lock on resend. The link endpoint requires the profile email to match the eligible account's email. The owner details page exposes the localized reactivation confirmation to authorized management roles.

Real SQL Server coverage now verifies resend and old-link rejection, activation, Manager and Super Admin scope, old JWT and refresh-cookie rejection after disable and after reactivation, contact editing, account linkage and duplicate rejection, owner archival constraints and retained property and closed financial history. The property tests assert all six catalog types, generated reference uniqueness, rejected ownership reassignment, concurrent edits with a single winner, archival, reactivation, operational status and audit history. Existing property mutations and server-side query behavior were reused.

- Backend files changed were `backend/Controllers/OwnersController.cs`, `backend/Repositories/AuthenticationRepository.cs`, `backend/Services/AccountManagementService.cs`, `backend/Services/OwnerPortfolioService.cs` and `tests/HostGrid.Persistence.Tests/AuthenticationTests.cs`.
- Frontend files changed were `frontend/src/app/features/owners/owner-pages.ts`, `frontend/src/app/features/properties/portfolio-copy.ts` and `frontend/src/app/features/properties/portfolio-page.scss`. Added `frontend/src/app/features/owners/owner-pages.spec.ts` and `frontend/e2e/portfolio.spec.ts`. Updated this log and the [recovery assessment](PHASE_05_RECOVERY.md).

| Check | Result |
|---|---|
| `dotnet restore --locked-mode`, `dotnet build --no-restore` | Passed. Zero build warnings and errors. |
| `dotnet test --no-restore` | **33 of 33** passed against a disposable LocalDB catalog, including the previous 30. |
| `npm.cmd run build`, `npm.cmd test -- --watch=false` on Node 24.15.0 | Build passed. **17 of 17** unit tests passed, including all 12 existing tests. Initial bundle **500.31 kB**, **315 bytes** over the existing warning budget. |
| Playwright in Edge | **4 of 4** passed, including the live Manager suite and all three existing tests. The final run verified Arabic at 390 px. An earlier test selector timed out despite a populated Owner picker. The next run found an RTL table overflow. The selector now uses the accessible combobox role, and the portfolio grid keeps horizontal scrolling inside the shared table. |
| EF migration list, pending model changes check and read-only SQL history | Passed. All three migrations remain applied to `HostGridDb`. No migration generated or applied to that catalog. |
| Direct and proxied API health, `git diff --check` | Both API health endpoints returned HTTP 200 directly and through the Angular proxy. The diff check passed. |

The live Playwright spec expects a Development API behind the Angular proxy, a local SMTP catcher and a fictitious Manager with MFA configured. Provide `HOSTGRID_E2E_MANAGER_EMAIL`, `HOSTGRID_E2E_MANAGER_PASSWORD` and one unused `HOSTGRID_E2E_MANAGER_RECOVERY_CODE` through the environment before running `npm.cmd run test:e2e`. Use a disposable development catalog, since the test creates records and consumes the recovery code.

## Phase 5 · Checkpoint 5.3 Property photos and private documents · 2026-11-26

Verified the existing tenant-scoped photo and document collections, downloads and archive operations. Added bounded JPEG, PNG and WebP decoding, configured multipart request limits, consistent cover visibility and ordering, controlled storage errors, and a read-only inventory for orphan candidates. Existing contract attachment now shares the private document lookup. No EF model changes were required.

The Angular Photos and Documents tabs show upload progress, preview loading and errors, and scan availability. The picker stays disabled while another change is finishing. Authenticated downloads use temporary object URLs, which are revoked after use or on navigation. Existing Manager and Owner routes remain lazy-loaded.

- Backend files changed are `backend/Controllers/PropertiesController.cs`, `backend/DTOs/Properties/PortfolioModels.cs`, `backend/HostGrid.Api.csproj`, `backend/packages.lock.json`, `backend/Integrations/Storage/LocalFileStorage.cs`, `backend/Mappers/PortfolioMapper.cs`, `backend/Program.cs`, `backend/Repositories/PortfolioRepository.cs`, and `backend/Services/{PropertyConfigurationService,PropertyFiles,PropertyPortfolioService}.cs`. Added `backend/Integrations/Storage/{PhotoContent,PrivateFileInventory}.cs` and `backend/Validators/PropertyUploadLimit.cs`.
- Frontend files changed are `frontend/src/app/features/properties/{portfolio-api.ts,portfolio-copy.ts,private-photo.ts,property-details.html,property-details.ts}` and `frontend/src/app/shared/components/form-elements.ts`. Added `frontend/src/app/features/properties/private-files.spec.ts` and `frontend/e2e/private-files.spec.ts`. Updated `tests/HostGrid.Persistence.Tests/{AuthenticationTests.cs,packages.lock.json}`.
- Storage configuration and operating instructions are in [FILE_STORAGE.md](FILE_STORAGE.md). The [gallery](screenshots/phase5-property-photos.png) and [documents](screenshots/phase5-private-documents.png) captures use a fictitious property. The [recovery assessment](PHASE_05_RECOVERY.md) tracks remaining Phase 5 work.

The live file test uses the same fictitious Manager credentials as Checkpoint 5.2 with a second unused code in `HOSTGRID_E2E_FILES_MANAGER_RECOVERY_CODE`. It also uses an activated fictitious Owner with optional MFA left off and with `HOSTGRID_E2E_FILES_OWNER_EMAIL`, `HOSTGRID_E2E_FILES_OWNER_PASSWORD`, and `HOSTGRID_E2E_FILES_OWNER_NAME` set. Set `HOSTGRID_FILES_SCREENSHOT_DIR` to capture the two screens.

| Check | Result |
|---|---|
| `dotnet restore --locked-mode` and `dotnet build --no-restore` | Passed with zero warnings and errors. |
| `dotnet test --no-build --no-restore` | **37 of 37** passed against LocalDB, including the previous 33. |
| `npm.cmd run build` on Node 24.15.0 | Passed. The initial bundle is **500.33 kB**, 333 bytes over the unchanged warning budget. |
| `npm.cmd test -- --watch=false` | **20 of 20** passed, including the previous 17. |
| Playwright in Edge | **5 of 5** passed, including all four existing tests. The new live scenario verified Manager upload, cover selection and download and the assigned Owner's read-only access. The first run caught a picker race, which was fixed and verified on rerun. |
| Direct and proxied API health | Liveness and SQL readiness returned HTTP 200 directly and through the Angular proxy. |
| EF migration list, model snapshot check and read-only SQL history | All three migrations remain applied to `HostGridDb`. No model drift or new migration. |
| NuGet advisories | The configured NuGet sources reported no vulnerable API packages, including transitive dependencies. |
| `git diff --check` | Passed. |

Production downloads and covers require a clean scan result. The fallback scanner quarantines files, and a real scanner with a process to release clean content remains a production requirement. Checkpoint 5.4 remains pending approval.

## Phase 5 · Checkpoint 5.4 Contracts and commission history · 2026-11-26

The accepted policy keeps property commission changes explicit and historical contract terms fixed. The property view returns the active contract, current rate and business date. Scheduled contracts support versioned editing. Open contracts can end on a future date, and ended contracts can be archived while their agreements and PDFs remain in history. Contract and commission writes check closed property months under the property transaction lock. The commission history remains the source for configured rates as of a business date.

- Backend changes cover `backend/Controllers/PropertiesController.cs`, `backend/DTOs/Properties/PortfolioModels.cs`, `backend/Entities/Property/PropertyEntities.cs`, `backend/Repositories/PortfolioRepository.cs`, `backend/Services/{PropertyConfigurationService,PropertyPortfolioService}.cs`, `backend/Validators/PortfolioValidation.cs`, the new `ManagementContractArchive` migration and snapshot, and `tests/HostGrid.Persistence.Tests/AuthenticationTests.cs`.
- Angular changes cover `frontend/src/app/features/properties/{portfolio-api.ts,portfolio-copy.ts,property-details.ts,property-details.html,private-files.spec.ts}`. Added `frontend/e2e/contracts.spec.ts` and extracted shared live-test helpers into `frontend/e2e/portfolio-fixtures.ts` from the existing private-files scenario.
- The [contract and commission guide](CONTRACTS_AND_COMMISSION.md) lists the new edit, archive and effective-rate endpoints, the existing create and termination routes, policy decisions and safeguards. The [recovery assessment](PHASE_05_RECOVERY.md) tracks remaining work.

- Resume verification confirmed all four migration entries on the existing local `HostGridDb`, including `20261126165727_ManagementContractArchive` and the three earlier migrations. SQL Server confirmed the nullable `ArchivedAt` column. EF reported no model drift. This check was read-only.
- The targeted LocalDB rerun passed **4 of 4** contract, commission, attachment and portfolio regression tests. The closure query now compares the year and month as separate SQL `smallint` values and avoids the failed conversion of a combined date number.
- The Angular production build passed. The initial bundle remains **500.33 kB**, 333 bytes over the unchanged warning budget.
- The full Angular unit suite passed **23 of 23**, including all 20 baseline tests. It verified contract validation, effective-date input, confirmation, Owner read-only controls and the rate mismatch warning.
- API liveness and SQL readiness returned HTTP 200 directly and through the Angular development proxy. The live Edge contract scenario passed with fictitious Manager and Owner accounts, including PDF attachment and download, the mismatch warning and two dated commission rates. Captured the [contract interface](screenshots/phase5-management-contract.png) and [commission history](screenshots/phase5-commission-history.png).
- The full Playwright suite passed **6 of 6** in Edge, including all five baseline tests.
- The final .NET build passed with zero warnings or errors. The full SQL Server suite passed **39 of 39**, including all 37 baseline tests. The commission test verified that a successful change leaves an existing reservation’s snapshot rate, amount and rowversion intact. It also verified that blocked changes leave closed financial records and commission history intact. Both staged and working-tree diff checks passed.

## Phase 5 · Checkpoint 5.5 Amenities, inventory, pricing and listings · 2026-11-26

- Audited the existing nine amenity seeds, versioned inventory and listing actions, per-property pricing lock and Saturday/Sunday rules. Reused the existing catalog and saved associations.
- Implemented inventory notes and a searchable table, a versioned base-rate action, nightly rule provenance, and a bounded multi-night pricing preview. Reused existing seasonal/listing forms and added localized labels and validation. Generated one additive inventory-notes migration.
- Reviewed the generated migration SQL and applied `20261126200005_PropertyInventoryNotes` to the existing local `HostGridDb`. The four earlier migrations and inventory row count were preserved. EF reported no model drift.
- Locked restore and the .NET build passed with no build warnings or errors. The SQL Server suite passed **41 of 41**, including amenity and inventory changes, listing validation, every pricing tier, concurrent overlap rejection and stored reservation price preservation. After the localized label updates, the Angular production build passed at **501.81 kB**, 1.81 kB over the unchanged warning budget.
- Angular unit tests passed **25 of 25**. API liveness and SQL readiness returned HTTP 200 directly and through the Angular proxy.
- Backend changes cover `backend/Controllers/PropertiesController.cs`, `backend/DTOs/Properties/PortfolioModels.cs`, `backend/Entities/Property/PropertyEntities.cs`, `backend/Data/Configurations/PropertyConfigurations.cs`, the inventory notes migration files and EF snapshot, `backend/Services/{PropertyConfigurationService,PropertyPortfolioService,PropertyPricing}.cs`, `backend/Validators/PortfolioValidation.cs`, and `tests/HostGrid.Persistence.Tests/AuthenticationTests.cs`. Angular changes cover `frontend/src/app/features/properties/{portfolio-api.ts,portfolio-copy.ts,property-details.ts,property-details.html,private-files.spec.ts}` and the new `frontend/e2e/inventory-pricing.spec.ts`.
- The [inventory and pricing guide](INVENTORY_AND_PRICING.md) records the API, date semantics and limits. Historical seasonal rules and stored reservations are preserved. The base rate remains the current fallback for previews.
- Captured the [inventory](screenshots/phase5-inventory.png) and [pricing preview](screenshots/phase5-pricing.png) screens with fictitious data through the live Angular proxy. The first browser attempt caught an outdated condition label. The inventory condition and notes now have distinct labels.
- Final Playwright run passed **7 of 7** in Edge, including all six baseline tests. Both Git diff checks passed. Checkpoint 5.6 Owner Portal Acceptance awaits explicit approval.

## Phase 5 · Checkpoint 5.6 Owner portal acceptance · 2026-11-26

- Reused the existing Owner shell, portfolio and account lifecycle. Added operational status in both portfolio views, localized file-preview feedback and a past-quote notice. The Owner UI omits the audit tab, and the API omits uploader identity from Owner file metadata.
- Added a SQL Server acceptance test for nested write denial, scope checks, and Owner-visible file metadata, plus a live browser journey that starts with a Manager-created profile and consumes the real Mailpit invitation.
- Locked restore and .NET build passed with zero warnings and errors. Angular production build passed at **501.81 kB**, 1.81 kB above the unchanged warning budget. Read-only EF verification found all five existing migrations and no model drift. API liveness and SQL readiness returned 200 directly and through the Angular proxy.
- SQL Server suite passed **42 of 42**. The first live Owner test exposed an activation link decoding issue. Angular decodes and can reserialize the fragment during initial navigation. The activation and reset forms now split the already-decoded route fragment at the proof delimiter, preserving literal plus signs in both fields.
- Final Angular production build passed at **501.81 kB**, 1.81 kB over the unchanged warning budget. The Angular suite passed **26 of 26**, including the encoded plus-sign regression with Angular's URL serializer.
- Backend files changed were `backend/DTOs/Properties/PortfolioModels.cs`, `backend/Services/PropertyFiles.cs` and `tests/HostGrid.Persistence.Tests/AuthenticationTests.cs`. Frontend files changed are `frontend/src/app/core/auth/auth.spec.ts`, `frontend/src/app/features/auth/auth-page.ts`, and `frontend/src/app/features/properties/{portfolio-api.ts,portfolio-copy.ts,property-details.ts,property-details.html,property-list.ts,property-list.html}`. Added `frontend/e2e/owner-portal.spec.ts` and the [Owner portal guide](OWNER_PORTAL.md).
- Recovered the final Edge run from its completed session and Playwright result. **8 of 8 passed** in 52.4 seconds. The new journey verified Mailpit activation, optional MFA, session restoration, the read-only portfolio, cross-owner and write denial, PDF download, account disable and revocation, reactivation with fresh sign-in, and logout. It also verified English, French, Arabic RTL, theme changes, keyboard tabs and table layout at widths from 360 to 1440 pixels. The [Owner portal guide](OWNER_PORTAL.md) links all seven screenshots.
- Rechecked the five migrations and EF model consistency against local `HostGridDb` without writing to it. Working tree and staged diff checks passed. Checkpoint 5.7 awaits explicit approval.

## Phase 5 · Checkpoint 5.7 Final verification and optimization · 2026-11-26

- Locked .NET restore and build passed, with zero warnings or errors. The full SQL Server regression suite started against disposable catalogs.
- The read-only catalog query found all five migrations, 68 trusted composite foreign keys, six property types and nine amenities. The local Owner, Property, ManagementContract and PropertyInventoryItem tables each contained zero rows. No writes were made to local HostGridDb.
- The initial frontend baseline was 501.81 kB. Static inspection found that the eager Aura preset imported the full component theme catalog. The configuration now imports the shared Aura tokens and the component themes used by HostGrid. The budget stays at 500 kB.
- EF confirmed no drift. The transitive NuGet advisory check reported zero known vulnerable API packages. The first frontend clean install found a Windows file lock from the local Angular watcher. The identified watcher was stopped and the install retried.
- The full SQL Server suite passed **42 of 42**. The locked Angular install succeeded on retry. The production npm audit returned zero findings. The full npm graph reported 16 moderate findings in development tooling, with no high or critical findings.
- The optimized production build passed at **420.71 kB**, down **81.10 kB** from 501.81 kB, below the unchanged 500 kB budget. The main chunk fell from 112.78 kB to 31.68 kB. The static Aura catalog was the source of the reduction.
- Angular unit tests passed **26 of 26**. API liveness and SQL readiness both returned HTTP 200 directly and through Angular's development proxy.
- Edge passed **9 of 9**, including the eight baseline scenarios and a live Super Admin organization scope check. The visual review caught clipped property references on narrow grid cards. The card now wraps long references, with a browser assertion added. The final rerun is in progress.
- The production rebuild after the wrapping fix measured **420.71 kB** again. Angular unit tests again passed **26 of 26**. Staged and unstaged diff checks and release-document links passed.
- The second Edge run passed eight scenarios and timed out waiting for the existing private PDF upload request. The test now waits for the cover change to finish and selects the PDF input inside the active Documents panel. The HTTP 201 and download assertions are retained.
- A later run completed the upload but timed out waiting for Owner sign-in. The next Edge run passed the private file scenario with explicit login response and landing-page assertions, then exposed a separate intermittent Owner directory search timeout. The live search test now targets the grid control and verifies the actual filtered response before checking the empty state.
- The explicit login assertion identified the intermittent sign-in failure as HTTP 429. The fixture bootstrap and the Edge tests shared the Development API's authentication rate-limit window. The verification runner now waits one full window after fixture setup. Application rate limiting and browser assertions stay enabled.
- The final Edge suite passed **9 of 9 in 50.2 seconds**, including the full Owner journey and private file scenario. Liveness and SQL readiness again returned HTTP 200 directly and through the Angular proxy. The [Phase 5 acceptance report](PHASE_05_ACCEPTANCE.md) records the 21-section matrix, screenshots, remaining production blockers and Phase 6 prerequisites. Checkpoint 5.7 is complete and awaits explicit approval before Phase 6.

## Phase 6 · Checkpoint 6.0 Business Rules & Technical Audit · 2026-11-27

- Phase 5 was approved. Read the specification pack and existing project guides. Updated the original `BUSINESS_RULES.md` and `IMPLEMENTATION_PLAN.md` in the companion specification pack. Added the [Phase 6 spec](PHASE_06_SPEC.md), [model audit](PHASE_06_TECHNICAL_AUDIT.md), [business rules](RESERVATION_BUSINESS_RULES.md), [concurrency design](RESERVATION_CONCURRENCY_DESIGN.md) and [checkpoint plan](PHASE_06_CHECKPOINTS.md).
- Recorded eight approved business decisions, a proposed lifecycle and a shared transaction-owned per-property SQL lock design. Booking implementation and new migrations are outside this checkpoint. The precise release boundaries, property eligibility and commission lookup date still need approval.
- `dotnet build --no-restore` passed with zero warnings and errors. `dotnet test --no-build --no-restore` passed **42 of 42** on isolated SQL Server test catalogs. The installed SDK is 11.0.100 and the .NET 10 runtime is 10.0.15. The matching SDK remains a prerequisite.
- `dotnet ef migrations list --project backend --no-build`, `dotnet ef migrations has-pending-model-changes --project backend --no-build` and read-only `sqlcmd` checks confirmed five applied migrations, no model drift, eight booking tables and 18 trusted booking foreign keys. The local booking, nightly and block tables inspected were empty. The first catalog attempt lacked an EF connection string and met a sandboxed SQL client TLS error. The read-only retry with the local connection succeeded.
- Angular built at **420.71 kB** initial raw size under the unchanged **500 kB** warning budget. `npm test -- --watch=false` passed **26 of 26** in six files. Used the existing Node 24.15.0 and installed dependencies. Initial frontend command retries corrected the working directory and the sandbox's esbuild filesystem traversal failure.
- Phase 5 advisory evidence remains zero known production npm/NuGet findings and 16 moderate npm development-tooling findings. The advisory databases were queried during Phase 5. No dependencies were installed for Checkpoint 6.0.
- Edge workspace smoke passed **3 of 3** in 23.2 seconds using the existing mocked workspace fixtures. The six live fixture scenarios were last verified as part of the full **9 of 9** Phase 5 run. They were not rerun here because the existing live runner provisions a freshly migrated catalog. The audit changed documentation only. Checkpoint 6.1 awaits explicit approval.

## Phase 6 · Checkpoint 6.1 Availability & Concurrency Protection · 2026-11-27

- Checkpoint 6.0 was approved. Imported the eight standalone specification files from the companion pack because the repository had no equivalents. The root files are now authoritative and record the new approvals.
- Added the scoped availability endpoint, protected booking write primitives and shared property status/capacity locks. Initial locked restore and backend build passed with NU1900 for the sandbox-restricted advisory endpoint.
- Inspected the generated incremental SQL, including nullable commission and release metadata. The existing LocalDB catalog had five migrations and zero reservations before application. See [the recovery log](PHASE_06_RECOVERY.md) for the current verification state.
- Angular production build stayed at **420.71 kB** and the existing **26 of 26** Angular unit tests passed. The first focused .NET run found a test startup error from a null timeout configuration override. Corrected the fixture to set the option only for tests that specify a timeout.
- The migration replay preserved an existing reservation's dates, 23.25% rate and 232.50 commission. Applied the migration to local HostGridDb and verified all six entries, trusted release constraint and no model drift. The local reservation count stayed at zero.
- The first Edge regression passed **9 of 9**. Direct liveness/readiness and both proxied checks returned 200. The SQL tests exposed a sparse-table overlap scan that waited on another property's insert, plus SqlClient surfacing cancellation as a SQL exception. Restricted overlap queries to their two tenant/property range indexes and normalized cancellation at the transaction boundary.
- The next full backend run passed all 21 new cases and found two Phase 5 regressions in denied property writes. The coordinator now checks the property resource before its manager guard, preserving existing scoped 404 responses. Rebuilt successfully and started the final full SQL Server and Edge runs. Git diff checks and the new documentation links passed.
- The final full SQL Server run passed **64 of 64**, preserving all 42 Phase 5 tests and adding 22 cases. In overlapping races, exactly one writer committed. A different property proceeded while the first writer held its lock. Tests also proved a waiting hold can take newly released nights, cancellation and rollback free the lock, and an EF precommit retry reacquires it without duplicating the hold or audit.
- The final full Edge run passed **9 of 9**. Liveness and readiness again returned 200 directly and through the Angular proxy. Checkpoint 6.1 is complete. Checkpoint 6.2 awaits explicit approval.

## Phase 6 · Checkpoint 6.2 Guests & Reservation Management · 2026-11-27

- Checkpoint 6.1 was approved. Confirmed the three remaining policies with the project owner. External residual cents go to the earliest unresolved nights, the reservation commission rounds half away from zero, and historical entry uses Active properties, current capacity, completed stays, an actual confirmation date and private PDF evidence.
- Guest, pricing, reservation and historical APIs and Angular Manager routes were added. The initial backend build passed, with NU1900 for the inaccessible advisory feed. npm production advisory query returned zero findings. Initial Angular compilation exposed a dialog selector and observable type mismatch, corrected before retrying.
- Generated and reviewed `20261127142833_GuestReservationManagement`. EF reported no drift. Local preflight found six applied migrations, zero Guest and Reservation rows, and no duplicate external references. Isolated SQL verification is running before local application.
- **6.2-A through D focused verification** passed five SQL Server acceptance tests. The API tests exercised the guest directory and suggestions, confirmation with no fallback, revision history, concurrent overlaps and external reference uniqueness, and historical PDF access. The migration replay blocked a normalized legacy duplicate and preserved existing commission values and unknown metadata after the conflict was resolved in the disposable fixture.
- Applied the migration to local HostGridDb. All seven migrations are listed, the new filtered unique index is active, the booking schema has ten tables and EF found no drift. The primary catalog still had zero bookings and guests. The complete SQL run exposed a legacy test setup using the new Guest EF columns against an older schema. Updated the fixture to insert and read only columns available at the migration under test, preserving the original assertions.
- **6.2-E frontend build** passed at **421.72 kB**, below the unchanged 500 kB warning budget. The 26 baseline Angular tests passed. New booking form tests needed test-locale setup. Edge is running with a scoped fictitious property and historical commission fixture. Direct and proxied health checks returned 200.
- The complete SQL Server suite passed **69 of 69** after the fixture correction. Angular passed **30 of 30** after loading the same locale registry as the application. Both production advisory checks returned zero findings after a successful locked NuGet restore retry.
- The first full Edge run passed all nine baseline scenarios. The new booking test selected a same-named reservation list column control during navigation. It now waits for the accessible reservation form combobox. The full Edge rerun uses a new disposable catalog and fixture recovery code.
- The final backend build passed with zero warnings and errors. The complete SQL Server suite passed **70 of 70**, including 64 baseline cases, with no skips. Added route coverage for tenant-qualified guest/property selection, capacity, status, accepted external prices and Pending reassignment. EF still reports no drift with all seven migrations.
- The new browser journey reached historical booking, PDF access, French and Arabic. Its 390 px viewport assertion exposed horizontal overflow from the long booking reference in the shared heading. Enabled heading wrapping. The Angular build now measures **421.78 kB** and the final browser run is in progress.
- Final Angular unit regression passed **30 of 30**, including all 26 baseline tests. The booking tests exercised invalid dates, capacity, contact suggestions and guest selection, explicit nightly overrides, conflict and timeout responses, and historical evidence requirements.
- The new live Edge booking journey passed on the final build, including Arabic RTL at 390 px, French, historical evidence download, and Owner denial. The combined run returned **9 passed and 1 failed** because the existing private-files scenario received HTTP 429 during Owner sign-in. The nine existing scenarios are being rerun in a fresh rate-limit window. The configured limit and security assertions remain active.
- **6.2-F verified.** The separate baseline Edge run passed **9 of 9** in 48.2 seconds. The new booking journey and all nine baseline scenarios passed across the final two runs. Both health checks returned 200 directly and through the Angular proxy. All ten [screenshots](RESERVATION_MANAGEMENT.md), relative documentation links and staged and unstaged Git diff checks passed. Checkpoint 6.2 is complete. Checkpoint 6.3 awaits explicit approval.

## Phase 6 · Checkpoint 6.2 baseline preservation · 2026-11-27

- Checkpoint 6.2 is completed and approved. Checkpoint 6.3 is authorized and has not started. Owner-facing calendar UI is deferred to Checkpoint 6.6.
- The recovered Phase 5 and 6.2 source, tests, documents, synthetic screenshots, and seven migration pairs plus snapshot are already in commit `63628c0`. The follow-up adds Git ignore rules for local sensitive data and artifacts and removes an extra trailing blank line in the Playwright fixture helper. Keep real guest data and private uploads out of versioned paths, and keep signing keys and credentials in user secrets or environment variables.
- The installed .NET SDK is 11.0.100 with the .NET and ASP.NET Core 10.0.15 runtimes. The locked restore and backend build passed with zero build warnings or errors. The matching .NET 10 SDK remains the documented toolchain prerequisite. The Angular checks used the existing local Node 24.15.0 and npm 11.12.1 via `npm.cmd`. The production bundle measured **421.78 kB** against the **500 kB** warning budget. Angular unit tests passed **30 of 30** across seven files.
- LocalDB registry inspection succeeded outside the sandbox and showed that `MSSQLLocalDB` was stopped. Started the existing instance and read its `HostGridDb` migration history. SQL and EF reported seven applied migrations through `20261127142833_GuestReservationManagement`. EF found no pending model changes. Direct and Angular-proxied liveness and readiness returned HTTP 200 after starting the local services. No migrations or fixture writes were applied to `HostGridDb`.
- `dotnet test --no-build --no-restore` passed **70 of 70** SQL Server integration cases with zero skips in 2 minutes 48 seconds. The test fixtures used their own disposable LocalDB catalogs.
- The full Edge regression passed **10 of 10** in one run in about 1 minute 24 seconds. A separate repeat passed **9 of 9** in 52 seconds. Both used freshly migrated, isolated catalogs and synthetic accounts. Authentication rate limiting stayed enabled. The runner waited one full window after fixture setup.
- Reviewed all 162 paths from the recovered commit and spot-checked the synthetic guest, owner and private-document screenshots. Credential-pattern and tracked-artifact checks found no matches. Verified the new ignore rules against representative sensitive and generated paths. Both the follow-up and cumulative baseline diffs passed `git diff --check`.

## Phase 6 · Checkpoint 6.3-A calendar reads and indicators · 2026-11-27

- Added the organization-scoped calendar feed and daily summary, using the existing tenant authorization and half-open availability semantics. Display filters leave authoritative holds intact. See the [contracts](CALENDAR_API.md) and [recovery file list](PHASE_06_RECOVERY.md).
- Locked restore and the final backend build passed. The complete SQL Server suite passed **73 of 73** with zero skips, including scope and privacy, filtered holds, year boundaries, pagination and same-day turnover. Angular built at **421.78 kB** and **30 of 30** unit tests passed. No migration was needed for 6.3-A. Continue with 6.3-B block writes and recovery verification.

## Phase 6 · Checkpoint 6.3-B block writes and concurrency · 2026-11-27

- Added versioned manual block management, transactional audit, Casablanca release boundaries, and the nullable lifecycle fields. Reviewed the additive `CalendarBlockLifecycle` migration SQL. The corrected isolated migration replay preserved the legacy reservation and block dates and reason. Focused SQL coverage passed **6 of 6**, including competing HTTP block and reservation writes. See the [recovery file list](PHASE_06_RECOVERY.md) and [block lifecycle](CALENDAR_BLOCKS.md). The complete final regression is running.

## Phase 6 · Checkpoints 6.3-C and 6.3-D calendar UI recovery · 2026-11-27

- Recovered the existing Manager portfolio and property calendars, reservation prefill, previews and block controls. The production build passed at **429.63 kB**, the property FullCalendar chunk remains lazy, and Angular passed **34 of 34** unit tests. npm and NuGet production checks reported zero findings. Live `HostGridDb` has eight migrations through `CalendarBlockLifecycle`, with zero blocks, zero reservations and no EF model drift.
- The interrupted browser run reached the three property views and the block workflow. The resumed run addresses the duplicate language-control selector and validates keyboard selection, localization and Owner denial. The first resumed benchmark setup hit an unsupported SQL fixture function. The synthetic generator now works without that function. Full Edge and performance results remain pending until the run completes.
- The corrected benchmark run passed the live calendar scenario and captured all 13 screenshots. It measured 68–196 ms median across the six feed cases and 33 ms for the desktop scroll sample. The existing booking scenario exposed a test synchronization issue under the expanded fixture. The final run uses the standard synthetic fixture and a form-readiness assertion. See [performance](CALENDAR_MANAGEMENT.md#local-performance-measurements) and [screenshots](CALENDAR_UI_GUIDE.md#synthetic-screenshots).

## Phase 6 · Checkpoint 6.3-E final acceptance · 2026-11-27

- Locked restore and the final backend build passed with zero warnings and errors. The final SQL Server regression passed **78 of 78** with zero skips, including the migration replay and the waiting-reservation cancellation race. The calendar revision action now follows existing eligibility for legacy Confirmed reservations, with an added integration assertion.
- Angular production passed at **429.63 kB** under the **500 kB** budget, and all **34 of 34** unit tests passed. The complete standard Playwright Edge run passed **11 of 11** in one execution with authentication rate limiting enabled. Both production dependency audits reported zero findings.
- The existing development catalog has eight migrations through `CalendarBlockLifecycle` and EF reports no pending model changes. Direct and proxied liveness and readiness returned HTTP 200. Git whitespace and documentation link checks passed.
- Checkpoints 6.3-A through 6.3-E are implemented and verified. See the [recovery record and modified file groups](PHASE_06_RECOVERY.md) and the [calendar user guide](CALENDAR_MANAGEMENT.md). Checkpoint 6.4 awaits approval. Owner-facing calendar UI remains deferred to 6.6.
