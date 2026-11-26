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
