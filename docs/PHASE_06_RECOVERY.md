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
