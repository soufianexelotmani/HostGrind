# HostGrid database

HostGrid uses SQL Server and a single `HostGridDbContext` derived from ASP.NET Core Identity's EF context. Configuration classes are grouped by business module in `backend/Data/Configurations/`. The [ERD](ERD.md) shows their foreign keys.

## Local setup and migrations

Use SQL Server Developer, Express, or LocalDB. The following PowerShell commands use Windows authentication against the default per-user LocalDB instance.

```powershell
SqlLocalDB start MSSQLLocalDB
$env:ConnectionStrings__HostGrid = 'Server=(localdb)\MSSQLLocalDB;Database=HostGridDb;Integrated Security=True;Connect Timeout=10'
dotnet tool restore
dotnet restore --locked-mode
dotnet ef migrations list --project backend
dotnet ef migrations script --project backend --idempotent --output .local-tools/hostgrid.sql
dotnet ef database update --project backend
```

Create `.local-tools/` before generating the script. Review the script and the target database before applying it. EF does not run migrations on API startup. For a new migration use `dotnet ef migrations add <Name> --project backend --output-dir Data/Migrations`. Use environment variables or .NET User Secrets for credentials. The migration history is in `dbo.__EFMigrationsHistory`.

The migration inserts the three role names and six property types with stable IDs. Manager and Owner permissions will come from memberships in Phase 3. The role seeds contain no assignments or user accounts.

### Optional fictitious data

Enable the demo explicitly for the migration command. The design-time factory checks Development, LocalDB, Windows authentication, and the `HostGridDb` catalog before enabling the demo. It uses EF's migration lock and creates a fictitious organization, settings, and the twelve default expense categories. Repeated runs preserve existing records. Default expense category names are tenant-local and can be reused during organization provisioning.

```powershell
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:HostGrid__SeedDemo = 'true'
try {
    dotnet ef database update --project backend
} finally {
    Remove-Item Env:HostGrid__SeedDemo
}
```

### SQL Server integration tests

Run `dotnet test` with LocalDB available, or set `HOSTGRID_TEST_SQLSERVER` to a dedicated development SQL Server instance connection with create database permission. The test fixture overrides the database name with a unique `HostGrid_Phase2_Test_...` catalog, applies the migration, and removes only the catalog it created. The application database is outside the fixture.

## Entity groups

| SQL schema | Persisted data |
|---|---|
| `identity` | Users, roles, claims, logins, role memberships, tokens, preferences, and hashed refresh-token rotation links. |
| `organization` | Organizations, memberships, settings, and owner contacts. |
| `property` | Properties, types, amenities, photos, inventory, contracts, commission-rate history, seasonal pricing, and channel listings. |
| `booking` | Guests, reservations, payments, cancellations, refunds, status history, calendar blocks, and nightly allocations. |
| `expense` | Categories, expenses, expense payments, attachments, recurrence templates, and reimbursements. |
| `finance` | Monthly close snapshots and lines, signed adjustments, payouts and allocations, and cash movement journal. |
| `operation` | Cleaning, checklist templates and snapshots, maintenance, external contacts, work tasks, and linked documents or expenses. |
| `document` | Private file metadata and property document links. |
| `communication` | Owner requests, messages, status history, notifications, preferences, and email outbox. |
| `reporting` | Requested reports and dashboard layouts. |
| `system` | Tenant audit history and durable job execution state. |

## Isolation and integrity

- Tenant relationships use composite foreign keys with `OrganizationId`. Tenant rows also reference their organization and have tenant-qualified alternate keys. EF filters queries by an authorized tenant captured for the context lifetime, and tracked writes require that same tenant. Requests in Phase 2 have an unresolved tenant and receive no tenant rows. The Phase 3 policy must verify membership, resource ownership and administrative authorization before creating a tenant-specific context. Administrative operations should use audited, explicit tenant selection, including when iterating multiple tenants. Identity and shared resource catalog IDs are global. SQL access, filter bypasses, and bulk APIs are privileged maintenance paths outside the query/write guards.
- SQL Server generates sequential GUIDs for surrogate keys. Business dates and local wall-clock times use `date` and `time`. Technical timestamps use UTC `datetime2`, with EF rejecting non-UTC values and restoring UTC kind on read. Framework lockout timestamps use `datetimeoffset`. Mutable records use SQL Server `rowversion`. The schema fixes monetary precision at `decimal(18,2)` in MAD and percentages at `decimal(5,2)` within 0–100.
- Restrictive FKs protect historical records. System-managed Identity claims and credentials use the framework's Identity child delete behavior.
- SQL enforces tenant reference uniqueness, filtered external booking ID uniqueness, recurring occurrence uniqueness, automatic cleaning uniqueness, closure period uniqueness, allocation uniqueness, and movement idempotency. Refund links identify the same reservation as the optional original payment. Payout lines require the payout and closure to share an owner. Audit actors and other historical actors may outlive membership in the applicable tenant and reference global user IDs.
- Monthly closure rows and lines become immutable in SQL when the closure is closed. Save draft lines and totals before the close transition within the same transaction. Line modifications take a lock on the parent closure. Later changes use signed adjustments in an open month and preserve the historical row. Typed source IDs on closure lines, adjustments, transactions and audits reference different source tables and require service checks. File contents, booking overlaps, category lifecycle, proofs for confirmed charges, and aggregate payment limits require service-level enforcement.

## Reservation concurrency strategy

Every reservation or block mutation affecting occupancy will acquire a transaction-owned exclusive SQL Server application lock via `sp_getapplock`, keyed by organization and property. Acquire the lock before checks or writes, use a bounded timeout, and handle negative lock return codes as failures. For a move between properties, acquire both property locks in a stable order. Inside the transaction check both active reservations and active calendar blocks using `start < proposedEnd AND end > proposedStart`, excluding the row being edited. Then write the reservation or block and its nightly allocations before commit. This preserves same-day turnover. Phase 6 will exercise concurrent reservations and block combinations on SQL Server. Phase 2 has supporting range indexes and per-reservation nightly uniqueness.

## Financial scope and decisions to confirm

Direct-net bookings store absent gross and fees as NULL. Gross input stores the actual fee or the applied rate and calculated fee. Net revenue equals gross less fees when supplied. Commission uses the historical percentage of net, before expenses. Owner profit equals net less commission and owner expenses. The validated 14,000 MAD net example yields 2,800 MAD commission at 20%, and 11,100 MAD owner profit after 100 MAD of expenses.

The model stores nightly net and commission allocations, partial receipts and refunds, expense funding source, reimbursements, owner-direct receipts, payout allocations, signed settlement balances and later adjustments. Financial services must separately calculate economic profit and actual settlement. They must serialize writes affecting cumulative payment, reversal, and payout limits.

Approval is still needed before implementation of the rules for commission snapshot timing, rounding and remainder allocation, stays in progress at close, recognition dates for retained cancellation fees, and mixed-cash settlement and deficit allocation. Confirm pending reservation occupancy and equal-priority seasonal pricing behavior before reservation services. These decisions can be made in their respective phases.
