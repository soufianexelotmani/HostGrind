# HostGrid

HostGrid is a property management platform for Moroccan concierge businesses. The solution contains the ASP.NET Core API under `backend/` and the Angular standalone app under `frontend/`. See the [progress log](docs/PROGRESS.md) for verification and scope.

## Local prerequisites

- A .NET SDK that can build `net10.0` and the ASP.NET Core 10 runtime. Install the .NET 10 SDK for a matching toolchain.
- Node.js 24 and npm. Use `npm.cmd` if PowerShell blocks the npm script shim.
- SQL Server LocalDB on Windows, or a local SQL Server Developer or Express instance. The examples use LocalDB and Windows authentication.

## Run locally

From the repository root, start LocalDB and apply the EF Core migration.

```powershell
SqlLocalDB start MSSQLLocalDB
$env:ConnectionStrings__HostGrid = 'Server=(localdb)\MSSQLLocalDB;Database=HostGridDb;Integrated Security=True;Connect Timeout=3'
dotnet tool restore
dotnet restore --locked-mode
dotnet ef database update --project backend
dotnet run --project backend --launch-profile http
```

For a different SQL Server, set `ConnectionStrings__HostGrid` to the corresponding connection string or use `.NET User Secrets` with the key `ConnectionStrings:HostGrid`. Keep any credentials outside version control.

In a separate terminal, from the repository root:

```powershell
cd frontend
npm.cmd ci
npm.cmd start
```

The Angular dev server at `http://localhost:4200` forwards `/api/**` to the API at `http://localhost:5025`. The API's separate `https` launch profile also binds to `https://localhost:7169` when a development certificate is available.

| URL | Expected behavior |
|---|---|
| `http://localhost:4200/api/v1/health/live` | HTTP 200 when the API is running |
| `http://localhost:4200/api/v1/health/ready` | HTTP 200 when the configured SQL database can execute a query, HTTP 503 otherwise |
| `http://localhost:5025/openapi/v1.json` | OpenAPI document in Development |
| `http://localhost:5025/scalar/v1` | Interactive API reference in Development |

See the [database guide](docs/DATABASE.md) for migrations, development seed data, and SQL integration tests. The [ERD](docs/ERD.md) shows the schema. Follow the [authentication guide](docs/AUTHENTICATION.md) to run Mailpit, configure secrets, and bootstrap the first Super Admin. The [security guide](docs/SECURITY.md) describes the authorization and session controls.

## Verify

```powershell
dotnet build
dotnet test
cd frontend
npm.cmd run build
npm.cmd test -- --watch=false
```

To run the browser smoke tests, start the usual Angular dev server or let Playwright start it. Install Chromium using `npx.cmd playwright install chromium`, or set `HOSTGRID_BROWSER_CHANNEL` to `msedge` to use an installed copy of Edge. Run `npm.cmd run test:e2e` from `frontend/`. See the [UI guide](docs/UI_GUIDE.md) for design, localization, and screenshots.

The backend test suite executes the migration and relational checks on a separate SQL Server test database. Run the applications and request the health URLs above to verify startup, the proxy, and SQL connectivity.
