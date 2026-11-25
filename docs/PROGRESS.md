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
