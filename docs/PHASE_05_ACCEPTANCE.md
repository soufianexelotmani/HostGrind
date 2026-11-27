# Phase 5 acceptance | Properties & Owners

The original 21-section Phase 5 brief supplied with the project and the specification pack define the acceptance scope. The [recovery report](PHASE_05_RECOVERY.md) retains checkpoint history. This matrix reflects the later verified implementation and distinguishes production readiness from local acceptance.

| Requirement | Status | Implementation, evidence and limitation |
|---|---|---|
| 1. Architecture | Verified | One ASP.NET Core 10 project, SQL Server, typed DTOs, tenant repositories, async services, validators and lazy Angular 21 features. |
| 2. Owner management | Verified | SQL lifecycle tests cover contact edits, account linking, duplicate rejection, invitation, resend, revocation, reactivation and archival. Manager Edge workflow covers directory and details. Associated properties and contracts on owner details each have a 100-record cap. |
| 3. Property management | Verified | SQL covers the six seeded types, tenant-unique reference, persisted fields, association, status and rowversion. Manager Edge covers create, edit, archive, reactivate and both list views. Owner reassignment is restricted until transfer rules are approved. |
| 4. Management contracts | Verified | SQL and Edge cover active and historical agreements, date validation, overlap, editable future terms, end scheduling, archival and PDF retention. |
| 5. Commission history | Verified | SQL covers the 20% fallback, effective dates, precision, a single winner on a competing date, historic terms, closed periods and reservation snapshots. The property rate changes explicitly and the agreement retains its agreed rate. |
| 6. Property photos | Verified with limitation | SQL covers JPEG, PNG and WebP decoding, invalid and oversized uploads, order, cover race, removal and private streaming. Edge covers gallery and cover. Lazy previews use original images. Thumbnail generation remains open. |
| 7. Private documents | Verified locally | SQL and Edge cover upload, scoped download, retention and failure handling. Storage reconciliation is read-only. Production requires a configured and verified scanner. |
| 8. Amenities and inventory | Verified | SQL covers the catalog, duplicate prevention, inventory CRUD, notes, validation and Owner read-only access. Angular and Edge cover form and table workflows. |
| 9. Seasonal pricing | Verified | SQL covers precedence, weekend masks, half-open dates, overlap races, preview provenance and stored reservation price preservation. Edge covers rate configuration and preview. Earlier quote previews use the current base fallback. |
| 10. Listing metadata | Verified | SQL covers Airbnb, Booking.com and Direct and rejects unsafe URLs and stale writes. Edge covers manual metadata. |
| 11. API conventions | Verified | Typed endpoints, validation, scoped queries, JSON error responses, version checks and audit events are exercised by the SQL integration suite. Owner and property directory filtering, sorting and paging run in SQL. |
| 12. Authorization and tenancy | Verified | Relational and HTTP tests cover foreign keys, fail-closed tenant scope, Manager and explicitly scoped Super Admin access, Owner isolation, writes, sessions, MFA and private files. |
| 13. Angular owner management | Verified | Browser and unit coverage for directory, contact forms and lifecycle actions. The associated list cap follows the API. |
| 14. Angular property management | Verified | Manager browser workflows cover create, detail, gallery, documents, contracts, pricing, inventory and listings with shared controls. |
| 15. Owner portal | Verified | Live Mailpit activation, optional MFA, property access, PDF, revocation and fresh sign-in. Upcoming features are labeled in navigation. |
| 16. UI and localization | Verified for exercised layouts | Edge covers English, French and Arabic, RTL, dark/light/system, keyboard tabs, responsive tables and the Owner grid empty state. Loading and error states are present in the shared list and detail templates. Screenshots support the visual review. |
| 17. Frontend performance | Bundle target verified | Initial bundle 420.71 kB. Lazy routes and image fetching remain. Thumbnail generation remains deferred. |
| 18. Migrations | Verified | Five recorded migrations through PropertyInventoryNotes. See database evidence below. |
| 19. Automated tests | Verified | 42 SQL Server tests, 26 Angular unit tests and nine live Edge scenarios passed. |
| 20. Deferred functionality | Deferred | Reservations, availability/calendar, finance, operations, external integrations and cloud provisioning remain outside the checkpoint. |
| 21. Verification and documentation | Verified locally | Both builds, health and proxy requests, migration checks and Git diff checks passed. This report, the [property guide](PROPERTY_MANAGEMENT.md), the module guides and screenshots record the evidence. |

## API and reusable services

Routes use `/api/v1/organizations/{organizationId}`. The [property guide](PROPERTY_MANAGEMENT.md) lists the owner and property routes. Signed-in Owners read their own portfolio. Managers write within their organizations. Super Admin targets an explicit organization.

## Build and database evidence

The initial Angular bundle fell from **501.81 kB to 420.71 kB**, a reduction of 81.10 kB, approximately 16%. The main chunk fell from 112.78 kB to 31.68 kB. The new preset imports Aura's shared base and only the button, input text, progress spinner, ripple, skeleton and toast component themes. PrimeNG components already use selective imports. Property and Owner routes remain deferred, portfolio translations remain with their features, and the shared UI translations, icons, fonts and global styles were kept. The warning budget remains 500 kB.

Local `HostGridDb` records `InitialHostGrid`, `AuthenticationSecurity`, `PropertyPortfolio`, `ManagementContractArchive` and `PropertyInventoryNotes`. EF found no model drift. Read-only queries found 68 trusted composite keys, six property types and nine amenities. The local catalog's portfolio tables inspected for preservation were empty. Migration replay and seed checks ran in isolated SQL Server test catalogs, and the test suite exercised rejected cross-tenant relationships. This checkpoint made no changes to the local app database or migration files.

Production package audits reported zero npm runtime findings and zero NuGet API findings including transitive dependencies. The full npm audit reported 16 moderate development-tooling findings affecting Angular CLI and its dependency tree, including npm tooling, TUF and Sigstore. No high or critical findings were reported. Dependency versions and both lockfiles stay on the approved versions.

## Regression verification

| Check | Result |
|---|---|
| Locked .NET restore and build | Passed with zero warnings and errors. |
| SQL Server LocalDB integration | 42 passed, zero failed or skipped. |
| Angular lockfile installation | Passed after stopping the Local Angular watcher holding a Windows dependency file open. |
| Angular production build and unit tests | Build passed without a budget warning. 26 unit tests passed. |
| Playwright Edge | Nine passed in 50.2 seconds, including all eight baseline scenarios. Zero failed or skipped in the final run. |
| Direct API and Angular proxy health | Liveness and SQL readiness returned HTTP 200. |
| Git diff and documentation links | Staged and working-tree whitespace checks passed. Relative links in the release guides resolve. |

The SQL suite exercised closed-period immutability, reservation and commission snapshot preservation, concurrent writes, role and organization boundaries, revocation and MFA, file validation, quarantine and authorized downloads. Local file preview exceptions are Development-only. Production releases still require a scanner.

Earlier Edge reruns encountered intermittent PDF upload and Owner search timeouts and an Owner sign-in HTTP 429. The browser tests now wait for the relevant UI state and assert live responses. The final run started after the fixture requests aged out of the authentication limit and passed both affected scenarios with real upload, download and filtered search assertions.

## Screenshot evidence

- Manager [grid](screenshots/phase5-manager-properties-grid.png), [table](screenshots/phase5-manager-properties-table.png), [creation form](screenshots/phase5-manager-property-create.png) and [property details](screenshots/phase5-manager-property-details.png)
- Manager [Owner directory](screenshots/phase5-manager-owners.png), [Owner details](screenshots/phase5-manager-owner-details.png), [Arabic property details](screenshots/phase5-manager-property-arabic.png) and [dark mode](screenshots/phase5-manager-property-dark.png)
- [Super Admin organization portfolio](screenshots/phase5-admin-properties.png) and [Owner mobile portfolio](screenshots/phase5-owner-mobile.png). The [Owner portal guide](OWNER_PORTAL.md) links the full set of Owner captures.
- [Photos](screenshots/phase5-property-photos.png), [private PDFs](screenshots/phase5-private-documents.png), [contracts](screenshots/phase5-management-contract.png), [commission history](screenshots/phase5-commission-history.png), [inventory](screenshots/phase5-inventory.png) and [pricing](screenshots/phase5-pricing.png)

The live runs used fictitious accounts, Mailpit and isolated LocalDB catalogs. When provisioning fixtures through the API, leave a full 60-second authentication rate-limit window before starting Edge. The setup requests count toward the same limit as the browser requests. For the Super Admin scenario, provide `HOSTGRID_E2E_ADMIN_EMAIL`, `HOSTGRID_E2E_ADMIN_RECOVERY_CODE`, `HOSTGRID_E2E_MANAGER_PASSWORD` as the shared fixture password, and `HOSTGRID_E2E_ORGANIZATION_ID`. Set `HOSTGRID_PHASE5_SCREENSHOT_DIR` for the final Manager and Super Admin captures.

## Checkpoint 5.7 changes

- `frontend/src/app/core/config/theme.ts` selects the Aura component presets used by HostGrid. `frontend/src/app/features/properties/portfolio-copy.ts` fills missing property and file history translations. `frontend/src/app/features/properties/portfolio-page.scss` wraps long property references.
- `frontend/e2e/portfolio.spec.ts` adds the explicit Super Admin scope scenario, history and card assertions, and the missing screenshot captures. The nine new screenshots are linked above. `frontend/e2e/private-files.spec.ts` waits for confirmed cover ordering before selecting the PDF input and checks the Owner sign-in response. `frontend/e2e/owner-portal.spec.ts` checks the Owner grid search response and empty state.
- Updated `README.md`, `docs/DATABASE.md`, `docs/ERD.md`, `docs/OWNER_PORTAL.md`, `docs/PROGRESS.md` and `docs/PHASE_05_RECOVERY.md`. Added `docs/PROPERTY_MANAGEMENT.md` and this report.

## Production blockers and Phase 6 handoff

- **Production** requires real malware scanning and a verified quarantine-release process, plus an operator-assisted MFA recovery procedure. Local Development uses the documented private storage preview exception. A matching .NET 10 SDK remains a local and CI toolchain prerequisite. The API was verified targeting .NET 10 with SDK 11.0.100.
- **Phase 6** can reuse tenant context, resource authorization and the property repository, `PropertyPricing` for single and multi-night MAD quotes with rule provenance, and the dated commission lookup over `CommissionRateHistory`. The booking schema already has `Reservation`, `ReservationNight` and `CalendarBlock`. No booking writes or availability workflow are included here.
- Before Phase 6 booking writes, confirm availability eligibility for inactive, archived or operationally unavailable properties, which reservation statuses occupy nights, and pending hold expiry. Use a SQL Server transaction-owned property lock for overlap validation and writes covering reservations and calendar blocks, including update, cancellation and reactivation. Validate half-open stays and test concurrent writers against SQL Server. Confirm commission snapshot timing before booking snapshots are written.
- The current reservation-night uniqueness rule is per reservation and date. The property/date lookup index supports overlap queries and permits multiple rows for the same property and date. Phase 6 needs an atomic availability check and write protocol across both reservations and calendar blocks.
- **Phase 7 financial decisions** include rounding and remainder allocation, cancellation recognition, mixed-cash settlement and deficit allocation. Keep signed adjustments and closed-period protections. Historical reservation prices and commission snapshots remain untouched by Phase 5 configuration.
- **Phase 5 limitations** include no ownership-transfer workflow, no dated base-rate history, no generated thumbnails and no automated orphan deletion. See the [storage guide](FILE_STORAGE.md), [contract guide](CONTRACTS_AND_COMMISSION.md), [inventory and pricing guide](INVENTORY_AND_PRICING.md), and [Owner portal guide](OWNER_PORTAL.md).
