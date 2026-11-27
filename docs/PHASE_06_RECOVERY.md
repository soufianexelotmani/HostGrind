# Phase 6 recovery

## Checkpoint 6.1 · Availability and concurrency · 2026-11-27

The repository had no standalone root specifications. The companion pack held the only copies, including the 6.0 amendments. Imported the eight source specifications and added 6.1 decisions to the repository copies. Use the root files for subsequent changes.

Implemented scoped GET availability, server-side overlap queries, transaction-owned property locks, booking admission and release primitives, Pending move support, and shared protection for property status/capacity writes. Added SQL Server acceptance cases against production services and a real bearer-token HTTP test. See [the engine](AVAILABILITY_ENGINE.md) and [progress](PROGRESS.md).

Initial backend build and locked restore succeeded with NU1900 from the restricted advisory endpoint. Generated `20261127133709_BookingAvailabilityFoundation` and reviewed the forward SQL. Local preflight confirmed five applied migrations and zero existing reservations. Forward SQL keeps the existing commission values and precision, adds nullable release data, and issues no data update or delete. Continue with isolated migration replay, SQL acceptance and full regression before accepting 6.1.

The isolated replay preserved a seeded nonzero rate and amount. The migration was then applied to HostGridDb. EF and SQL both showed six entries, trusted release constraint and no model drift. Angular build and **26 of 26** unit tests passed at **420.71 kB**. The first complete Edge run passed **9 of 9** with direct and proxied health checks. Added forced tenant/property range seeks after observing cross-property contention from a sparse-table scan, and normalized SQL driver's cancellation result. The full backend run also exposed a Phase 5 compatibility issue in authorization ordering. Scoped property-write denial continues to return the established 404.

## Files changed by 6.1

- Root specifications added are `AGENTS.md`, `ARCHITECTURE.md`, `AZURE_DEPLOYMENT.md`, `BUSINESS_RULES.md`, `DATABASE_DESIGN.md`, `DESIGN_SYSTEM.md`, `IMPLEMENTATION_PLAN.md` and `MASTER_PROMPT.md`.
- New backend files are `DTOs/Booking/AvailabilityModels.cs`, `Options/AvailabilityOptions.cs`, `Repositories/AvailabilityRepository.cs`, `Services/AvailabilityTransactions.cs`, `Services/PropertyAvailability.cs`, and the `BookingAvailabilityFoundation` migration and designer. Updated `Program.cs`, `Controllers/PropertiesController.cs`, `Data/Configurations/BookingConfigurations.cs`, the EF snapshot, `Entities/Booking/BookingEntities.cs`, both security exception files, and `Services/PortfolioAccess.cs`, `PortfolioAudit.cs` and `PropertyPortfolioService.cs`.
- Added `tests/HostGrid.Persistence.Tests/AvailabilityTests.cs`. Extended the existing test factory and SQL fixture in `AuthenticationTests.cs` and `SqlServerFixture.cs`. The migration-preservation case replays the upgrade in a separate test catalog starting at the previous migration.
- Added this file and [AVAILABILITY_ENGINE.md](AVAILABILITY_ENGINE.md). Updated [PROGRESS.md](PROGRESS.md), [DATABASE.md](DATABASE.md), [ERD.md](ERD.md), [RESERVATION_BUSINESS_RULES.md](RESERVATION_BUSINESS_RULES.md), [RESERVATION_CONCURRENCY_DESIGN.md](RESERVATION_CONCURRENCY_DESIGN.md), [PHASE_06_SPEC.md](PHASE_06_SPEC.md), [PHASE_06_CHECKPOINTS.md](PHASE_06_CHECKPOINTS.md) and [PHASE_06_TECHNICAL_AUDIT.md](PHASE_06_TECHNICAL_AUDIT.md).

No frontend, lockfile, or earlier migration changes were needed for this checkpoint. The existing production malware scanning and operator-assisted MFA recovery blockers remain. The installed SDK is 11.0.100 and the app targets .NET 10. A matching SDK is still a toolchain prerequisite.

## Final backend verification

`dotnet restore --locked-mode` and `dotnet build --no-restore` passed. The advisory endpoint remained inaccessible (NU1900). `dotnet test --no-build --no-restore` passed **64 of 64** in 2 minutes 7 seconds with zero skipped. This includes the two previously failing Owner HTTP denial cases. The 22 added cases cover range boundaries, realistic status and release history, all three overlap pairings, concurrent revisions and moves, property changes, separate properties, cancellation, timeout, retry and tenant-scoped reads. See [concurrency evidence](RESERVATION_CONCURRENCY_DESIGN.md).

## Final browser verification and handoff

All nine Edge scenarios passed against the final backend build using fictitious fixtures and a separate LocalDB catalog. Direct API and Angular-proxied liveness and readiness returned HTTP 200. The Angular production bundle is 420.71 kB under the unmodified 500 kB warning budget, and all 26 unit tests passed.

Checkpoint 6.1 is complete. Checkpoint 6.2 can wire authorized guest and normal booking management to this service after approval. The complete historical entry workflow, edits to existing nightly history, manual Pending expiry, confirmation and lifecycle endpoints, and exceptional early release rules remain listed in [reservation business rules](RESERVATION_BUSINESS_RULES.md).

## Checkpoint 6.2 · Guests and reservations · 2026-11-27

The project owner approved 6.1, the ten 6.2 decisions and the allocation, commission rounding and historical evidence rules. The existing 6.1 coordinator is used by normal and historical reservation writes. The new migration is `20261127142833_GuestReservationManagement`. Read-only local preflight found zero booking and guest rows, six migrations and no new-uniqueness collisions. Do not advance to 6.3 before explicit approval.

Milestones 6.2-A through D passed five focused SQL tests, including isolated migration replay and concurrent route writes. The local migration was applied and verified with seven recorded migrations and no model drift. 6.2-E built at 421.72 kB. 6.2-F is rerunning full SQL, Angular and Edge acceptance. Earlier failures were limited to migration-fixture schema selection, Angular test translations and a browser locator that initially selected a column-visibility checkbox. See [progress](PROGRESS.md).

The full SQL Server run passed 69 of 69 and the Angular unit suite passed 30 of 30. The first Edge run preserved all nine previous scenarios. The new scenario is being rerun after improving its navigation selector. A later successful locked restore reached the NuGet feed. The explicit direct and transitive API audit and the npm production audit reported zero findings.

### Files changed in 6.2

- Backend controllers `GuestsController.cs` and `ReservationsController.cs`; booking DTOs `GuestModels.cs` and `ReservationModels.cs`; `GuestMapper.cs`, `ReservationMapper.cs`, `GuestRepository.cs`, `ReservationRepository.cs`, `GuestValidation.cs` and `ReservationValidation.cs`.
- New services `GuestManagement.cs`, `ReservationManagement.cs`, `ReservationConfirmation.cs`, `ReservationPricing.cs` and `ReservationEvidence.cs`. Reused and updated `PortfolioAccess.cs`, `PortfolioAudit.cs`, `PropertyAvailability.cs`, `PropertyPricing.cs` and API registration in `Program.cs`.
- Updated `BookingEntities.cs` and `BookingConfigurations.cs`; added the incremental `GuestReservationManagement` migration, designer and EF snapshot changes. Existing six migrations were preserved.
- Angular lazy booking routes, guest and reservation pages, translations, typed models and tests in `frontend/src/app/features/bookings/`. Updated `app.routes.ts`, `core/config/navigation.ts` and `core/config/ui-translations.ts`. Added `frontend/e2e/bookings.spec.ts` and synchronized upload assertions in `frontend/e2e/private-files.spec.ts`.
- Updated `frontend/src/styles.css` so long booking references wrap at narrow widths and in RTL.
- Added `ReservationManagementTests.cs` and adjusted only schema-specific setup or expected table count in the prior `AvailabilityTests.cs` and `PersistenceTests.cs`. Property and guest authorization still uses the existing test factory.
- Added [guest management](GUEST_MANAGEMENT.md), [reservation management](RESERVATION_MANAGEMENT.md), [pricing](RESERVATION_PRICING.md), [historical entry](HISTORICAL_RESERVATIONS.md) and fictitious screenshots. Updated the README, the three root business/database/plan specifications, and the Phase 6, availability, database, ERD and progress guides.

For the live booking scenario, set `HOSTGRID_E2E_BOOKING_MANAGER_RECOVERY_CODE` to an unused recovery code and `HOSTGRID_E2E_BOOKING_PROPERTY` to a fixture property in the Manager's organization. The fixture should be Active with capacity for at least one guest, have a base price and an 18.5% effective commission on the current business date and on 2025-08-30. Use the existing Manager and private-files Owner environment settings. Set `HOSTGRID_BOOKING_SCREENSHOT_DIR` to capture the screens. Run all tests with the application authentication rate limit active and allow one full window between real account provisioning and the browser suite.

Final .NET build passed with zero warnings and errors. The full SQL Server suite passed **70 of 70** with zero skipped in 4 minutes 13 seconds. The six added SQL cases cover the guest workflows and privacy, Pending and Confirmed transactions, legacy replay, simultaneous overlaps and external reference uniqueness, revised and retained night prices, and historical PDF protection. The Angular production rebuild after the heading fix measured **421.78 kB**.

The final Angular run passed **30 of 30**. The new Edge journey passed on the final build in 12.6 seconds and produced all ten [screenshots](RESERVATION_MANAGEMENT.md). The combined Edge run had one existing-scenario failure from HTTP 429. The existing scenarios are being verified separately with a fresh rate-limit window.

The separate baseline run passed **9 of 9** live Edge scenarios in 48.2 seconds. Across the final two runs, all ten scenarios passed on the final build. API liveness and SQL readiness returned 200 both directly and through the Angular proxy. Git diff and documentation link checks passed.

**Checkpoint 6.2 is complete.** Checkpoint 6.3 can build the calendar and manual blocking interface on the existing availability service after approval. Pending-to-Confirmed, stay lifecycle, manual expiry, payments, refunds and the Owner reservation portal remain in their approved later checkpoints. The production malware scanner, operator-assisted MFA recovery and matching .NET 10 SDK remain open.

## Checkpoint 6.3-A · Calendar feed and daily indicators · 2026-11-27

The approved starting commit is `3a6fe1c`. Implemented the no-store, tenant-scoped feed and selected-date summary. Booking and block display filters are independent of the authoritative hold projections. Calendar reads share the existing interval predicates and use scoped, no-tracking SQL projections. The summary counts Confirmed and CheckedIn effective nights and realized historical nights as approved. The three clarified decisions and the twelve approved business decisions are recorded in [the specification](PHASE_06_SPEC.md) and [the root rules](../BUSINESS_RULES.md).

Changed `backend/Controllers/CalendarController.cs`, `backend/DTOs/Booking/CalendarModels.cs`, `backend/Options/CalendarOptions.cs`, `backend/Repositories/{AvailabilityRepository,CalendarRepository}.cs`, `backend/Services/CalendarManagement.cs`, `backend/Program.cs`, and `tests/HostGrid.Persistence.Tests/CalendarTests.cs`. Added the [read contract](CALENDAR_API.md). This milestone required no migration or frontend changes. The locked restore and final build passed. The full SQL suite passed **73 of 73**, with zero skips. Angular production remained **421.78 kB** and **30 of 30** unit tests passed. Continue with 6.3-B and verify block writes, concurrency and migration replay before calendar acceptance.

## Checkpoint 6.3-B · Manual block lifecycle · 2026-11-27

Implemented Manager block create, read, revision and audited cancellation over the existing property transaction coordinator. All future-night changes recheck the shared overlap rules. A revision to an ongoing block preserves the original row and elapsed interval and writes the replacement inside the same transaction. A cancelled ongoing block retains elapsed nights. The API returns the replacement ID when the edit creates one.

Added `CalendarBlocksController`, `CalendarBlockManagement`, `CalendarBlockValidation` and the additive `20261127161858_CalendarBlockLifecycle` migration with designers and snapshot. Updated the booking entity and configuration, calendar DTOs and repository, shared overlap query, availability service, registration and SQL tests. See the [manual lifecycle](CALENDAR_BLOCKS.md) and [HTTP contract](CALENDAR_API.md).

The generated forward SQL adds nullable columns, three restrictive actor relationships and checked category and release boundaries. It does not rewrite legacy blocks. The first isolated replay used the wrong catalog in a test setup and was corrected. The focused SQL run passed **6 of 6** with zero skips after correction. It includes legacy replay, the ongoing revision and cancellation path, stale version rejection and conflicting HTTP write races. The complete regression and the local migration history check are tracked in the final verification entry.

## Checkpoint 6.3-C and 6.3-D · Calendar interfaces · 2026-11-27

Recovered the implemented Manager routes, deferred FullCalendar day/week/month component, Angular 30-day portfolio, filters, indicators, previews, block forms, and reservation prefill. The previously interrupted smoke run reached all three property views and the block lifecycle before stopping on a duplicate language-control locator. The current Edge scenario scopes language and theme controls to the banner and exercises keyboard date selection, French, Arabic RTL, mobile, dark mode, and Owner denial.

The UI changes are under `frontend/src/app/features/calendar/`, with integration in `app.routes.ts`, `core/config/navigation.ts`, the existing reservation form, and property details. The lockfile and installed FullCalendar manifests agree on four MIT Standard packages at `6.1.20`. The Angular adapter declares Angular peers 12 through 21. The resumed production build passed at **429.63 kB** under the existing **500 kB** warning budget. The individual property calendar remains lazy. Angular passed **34 of 34** tests, including cancellation of obsolete feed requests and preservation of hidden holds. See the [UI guide](CALENDAR_UI_GUIDE.md).

The resumed live calendar scenario passed, covering all property views, keyboard selection, reservation prefill and refresh, overlap rejection, block revision and cancellation, filtered and cancelled events, French and Arabic RTL, dark theme, mobile layout and Owner denial. It produced [13 synthetic screenshots](CALENDAR_UI_GUIDE.md#synthetic-screenshots) and the [local performance measurements](CALENDAR_MANAGEMENT.md#local-performance-measurements). The same benchmark run exposed a property selection race in the existing booking scenario. The booking test now waits for form readiness and verifies the selected value. The final regression uses the standard synthetic portfolio.

## Checkpoint 6.3-E · Final acceptance · 2026-11-27

Completed milestones 6.3-A through 6.3-E. After recovery, matched the calendar's revision eligibility to the existing reservation form and write service for legacy Confirmed reservations without stored pricing. Added the corresponding Manager read assertion in `CalendarTests.cs`. Refined `frontend/e2e/{bookings,calendar}.spec.ts` for readiness, keyboard selection and localization. Kept the recovered implementation and migration in place.

- The final locked .NET restore and build passed with zero warnings and errors. The full SQL Server suite passed **78 of 78** with zero skips in 4 minutes 22 seconds. Coverage includes replay with legacy blocks, scope and privacy, allowed categories and property states, historical cutoff and audited cancellation, competing writes, and the cancellation race where a waiting reservation uses the newly released night after commit.
- `HostGridDb` contains eight migrations through `20261127161858_CalendarBlockLifecycle`. Both inspected row counts remain zero and EF reported no pending model changes. The final health run returned HTTP 200 for liveness and readiness directly and through the Angular proxy on the existing and isolated catalogs.
- The final Angular production build was **429.63 kB** against the **500 kB** initial warning budget. Angular passed **34 of 34**. The final standard Edge run passed **11 of 11** in about 1 minute 42 seconds with rate limiting enabled. The separate benchmark run passed the calendar scenario and produced measurements and screenshots. It passed 10 of 11 overall before the booking fixture synchronization fix. npm and NuGet production audits reported zero vulnerable packages from their configured sources.
- See the [API](CALENDAR_API.md), [block rules](CALENDAR_BLOCKS.md), [performance evidence](CALENDAR_MANAGEMENT.md#local-performance-measurements), and [UI guide and screenshots](CALENDAR_UI_GUIDE.md#synthetic-screenshots). Git whitespace and local documentation link checks passed. The working baseline is `3a6fe1c` plus the local 6.3 changes. Checkpoint 6.4 awaits explicit approval, and Owner-facing calendar UI remains in 6.6.
