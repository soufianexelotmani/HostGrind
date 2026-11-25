# HostGrid

HostGrid is a property management platform for Moroccan concierge businesses. The solution contains the ASP.NET Core API under `backend/` and the Angular standalone app under `frontend/`. See [Phase 1 progress](docs/PROGRESS.md) for verification and scope.

## Local prerequisites

- A .NET SDK that can build `net10.0` and the ASP.NET Core 10 runtime. Install the .NET 10 SDK for a matching toolchain.
- Node.js 24 and npm. Use `npm.cmd` if PowerShell blocks the npm script shim.
- SQL Server LocalDB on Windows, or a local SQL Server Developer or Express instance. The examples use LocalDB and Windows authentication.

## Run locally

From the repository root, start LocalDB and create the empty development database if needed.

```powershell
SqlLocalDB start MSSQLLocalDB
sqlcmd -S '(localdb)\MSSQLLocalDB' -E -C -b -Q "IF DB_ID(N'HostGridDb') IS NULL CREATE DATABASE [HostGridDb];"

$env:ConnectionStrings__HostGrid = 'Server=(localdb)\MSSQLLocalDB;Database=HostGridDb;Integrated Security=True;Connect Timeout=3'
dotnet restore --locked-mode
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

Phase 1 uses an empty local database. EF Core schema and migrations belong to Phase 2. Account creation and sign-in belong to Phase 3. SMTP and private storage setup will be documented with those integrations.

## Verify

```powershell
dotnet build
dotnet test
cd frontend
npm.cmd run build
npm.cmd test -- --watch=false
```

The backend has HTTP smoke checks in this phase. Run the applications and request the health URLs above to verify startup, the proxy, and SQL connectivity.
