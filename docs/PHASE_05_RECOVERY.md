# Phase 5 recovery audit

Audit date 2026-11-26. This report assesses the interrupted Properties & Owners implementation. The workspace was clean at `bfbf1b8` (`feat x`) on entry, with no staged, unstaged, or untracked changes. That commit contains the recovered implementation alongside prior phases. Its parents include `01a9309` and `5fb95a5`. `docs/PROGRESS.md` ends with Phase 4. The requested `docs/PHASE_05_SPEC.md` was absent from the checkout and the specification pack, so this audit uses the original 21-section Phase 5 attachment and the project specifications in `HostGrid-Codex-Pack`.

The existing application structure remains intact. The recovered files include tenant-scoped owner and property controllers, typed DTOs, portfolio repositories, an explicit property mapper, FluentValidation, private storage, audit recording, a new EF migration, and lazy Angular pages. Existing Identity, resource policies, Angular Signals, Reactive Forms, the shared UI system, and the development proxy are reused. There are no Phase 5 dependency additions evident in the recovered implementation. This audit changed only this report.

**Strict verified completion estimate of 10%** based on two fully verified modules out of the 20 requested assessment areas. Sixteen other modules have substantive code or artifacts with outstanding work or verification. This is an acceptance measure, independent of the amount of code already written.

## Requirement assessment

| Module | Classification | Evidence and outstanding acceptance work |
|---|---|---|
| 1. Owner management | PARTIALLY IMPLEMENTED | Create, edit, contact fields, search/filter/sort/page, portfolio and contract overview, archive, account status, link, invitation/resend and access disable are in place. SQL integration reached successful profile and property creation. The scenario failed before validating profile invitation, activation, and isolation. Verify the owner list/detail queries separately, the email-match rules for linking, and disable/archive effects. Owner portfolio child lists are capped at 100 without paging. |
| 2. Property management | PARTIALLY IMPLEMENTED | Create, edit, details, status, operational status display, six seeded types, references, coordinates, capacity, pricing, times, UTC timestamps, rowversion, immutable owner association, and both views exist. The history/configuration test passed property creation, generated-reference uniqueness enforcement, stale edit rejection and archival. Filtered list returned HTTP 500. Reactivation and simultaneous writes still need targeted tests. |
| 3. Management contracts | PARTIALLY IMPLEMENTED | Existing entity supports half-open dates, derived active/scheduled/ended status, optional end, owner snapshot, PDF association and history. SQL verified invalid/overlapping dates and retention of a contract rate after commission changes. PDF attachment and active rendering still need an end-to-end check. Inspect the Angular optional commission field, which can coerce an emptied number input to zero rather than request the configured rate. |
| 4. Commission rate history | COMPLETED AND VERIFIED | SQL verified rate bounds, historical rate retention, closing the previous interval and preserving the contract rate. The service appends effective periods and records an audit event. Reservation snapshot timing and financial recalculation remain later-phase decisions. |
| 5. Property photos | PARTIALLY IMPLEMENTED | Multiple uploads, signature/extension/cap validation, protected blobs, lazy gallery previews, first-photo cover, reordering and archive are present. The SQL file test covered photo size rejection. Successful image upload, ordering, cover changes, gallery error behavior and responsive previews need browser proof. Thumbnail generation is absent. |
| 6. Private document storage | PARTIALLY IMPLEMENTED | The local storage and scanner abstractions, opaque identifiers, PDF metadata, access checks, archive and contract retention exist. SQL integration passed allowed-owner and denied-other-owner downloads, anonymous denial, malformed type, size and storage key checks. Production scan integration is absent by design and unscanned content remains quarantined. Exercise contract attachment/archive and failure cleanup. Ambiguous commit outcomes retain the blob for reconciliation, and no reconciliation job exists. |
| 7. Amenities and inventory | PARTIALLY IMPLEMENTED | Nine amenity seeds and selection exist. Inventory add, edit, remove, quantity, condition/notes and search are present. SQL verified valid and negative-quantity requests. Amenity changes and inventory edit/removal are unverified. |
| 8. Seasonal pricing | COMPLETED AND VERIFIED | SQL verified custom precedence and equal-priority overlap rejection. Code orders custom, seasonal, weekend and base, with an exclusive end date and Saturday/Sunday weekend masks. Past/effective rules are retained. Browser configuration, each fallback tier, and day-specific boundary cases still merit additional acceptance tests. |
| 9. Listing metadata | IMPLEMENTED BUT NOT VERIFIED | Scoped Airbnb, Booking.com and Direct metadata is editable with bounded references and HTTP(S) URL validation. Integrations remain manual. No automated round-trip scenario was found. |
| 10. Backend APIs | PARTIALLY IMPLEMENTED | Tenant-prefixed REST endpoints, typed requests and responses, validation, ProblemDetails, audit, async SQL and rowversion exist. The suite surfaced a server error in filtered paging. Query translation for the owner and property projections needs a focused check. |
| 11. Authorization and tenant isolation | PARTIALLY IMPLEMENTED | Reuses the scoped context, composite FKs and property handler. Property association across organizations was rejected before the list failure. File authorization and the 26 prior persistence/authentication tests passed. Phase 5 Manager cross-organization list, Owner record denial and Owner write denial assertions occur after the failing list and therefore did not run. |
| 12. Angular owner management | PARTIALLY IMPLEMENTED | Lazy owner list, edit/create and details, controls for invitation/disable/archive/link, account status, associated records, shared table and states compile. No dedicated browser or unit test covers these pages. The owner page detail lists inherit the 100-record cap. |
| 13. Angular property management | PARTIALLY IMPLEMENTED | Lazy grid/table, filters, form and all seven detail tabs compile. Owner creation is a separate linked workflow. The list failure blocks the full experience. Feature-specific form, upload progress, status and edit journeys remain unverified. |
| 14. Owner portal | PARTIALLY IMPLEMENTED | The guarded My Properties routes and read-only detail rendering exist. The protected document test passed at API level. The filtered/paged list and browser journey still need verification with an activated Owner, including cross-owner denial. |
| 15. Internationalization and RTL | PARTIALLY IMPLEMENTED | Phase 5 adds English, French and Arabic dictionaries through the existing Transloco setup. Property history constructs `action_*` keys that are missing from the dictionary. Some detail dates/quantities use raw representations. Phase 5 Arabic layout and localized feedback have no dedicated browser coverage. |
| 16. Frontend performance | PARTIALLY IMPLEMENTED | Properties and Owners compile as lazy chunks, and images are loaded when near the viewport. Initial bundle is 500.31 kB versus 498.23 kB in Phase 4, about 2.08 kB larger and 315 bytes above the unchanged warning budget. Browser/network performance of actual portfolios and thumbnail behavior remain open. |
| 17. Database migrations | PARTIALLY IMPLEMENTED | Incremental migration exists, matches the model, and was applied by the integration fixture to disposable catalogs. The read-only inspection found it pending in `HostGridDb`. |
| 18. Automated tests | PARTIALLY IMPLEMENTED | Three Phase 5 SQL tests exist, two passed and one failed. No Phase 5-specific Vitest or Playwright scenario exists. Several assertions in the failed test were never reached. |
| 19. Documentation | NOT STARTED | The original Phase 5 deliverables still need README/progress, database/ERD updates and `PROPERTY_MANAGEMENT.md`. Existing documents stop at Phase 4. This recovery report is separate. |
| 20. Final verification | BLOCKED | The filtered-list HTTP 500 and pending LocalDB migration prevent acceptance. The nine requested Phase 5 screenshots, live portfolio workflow through Angular, and new end-to-end scenarios are absent. The four existing screenshots belong to Phase 4. |

Deferred scope is preserved. The audit found no Phase 5 implementation of bookings/calendar locking, financial calculations, closures, payouts, automation, external channel synchronization or Azure provisioning. The existing database and security guides remain the source for deferred finance and MFA recovery decisions. The installed SDK is .NET 11.0.100 with the app still targeting .NET 10. Angular remains 21 on Node 24.15.0.

## Verification and migration status

- `dotnet build --no-restore` passed with zero warnings or errors. `dotnet test --no-build --no-restore` ran **29** SQL Server tests, **28 passed**, **1 failed**, no skips. All 26 pre-Phase-5 tests passed. The two new passing tests covered property history/pricing/validation/concurrency-token behavior and private file checks.
- `npm.cmd run build` passed. It reported NG8113 for the unused `EmptyState` import in `OwnerDetails` and the initial bundle warning described above.
- `npm.cmd test -- --watch=false` passed **12 of 12** tests across four files. The existing Playwright suite initially passed two of three tests, with a sign-in navigation timeout while builds were running. The isolated rerun passed **3 of 3** in Edge. These checks cover the existing workspace, so they do not establish Phase 5 browser acceptance.
- `dotnet ef migrations has-pending-model-changes --project backend --no-build` found no model drift. The read-only `sqlcmd` query showed `20261125223356_InitialHostGrid` and `20261125225143_AuthenticationSecurity` applied to `HostGridDb`. `20261125234521_PropertyPortfolio` is pending, and `property.Property.CreatedAt` is absent there. The migration makes the existing Owner membership link optional, replaces its unique index with a filtered unique index, adds archival/timestamp fields and seeds amenities. The audit performed no local development catalog update.
- NuGet's transitive audit and `npm.cmd audit --omit=dev` found zero advisories. The full npm check still reported 16 moderate development advisories, with an Angular 22 major upgrade proposed by npm. Dependency versions and lockfiles were left intact.
- `git diff --check` passed. A fresh clean install was unnecessary for this recovery audit, which used the existing installed lockfile dependencies.

## Known errors and security findings

- The failing test is `ManagerCanCreateAndInviteAnOwnerThenOwnerCanReadOnlyTheirPortfolio`. It received HTTP 500 at the paged property list assertion in `AuthenticationTests.cs:349`. It had already created the owner and two properties and rejected the foreign organization owner association. The list query composes filtering and sorting after a positional DTO projection in `PortfolioRepository`. EF translation is the leading hypothesis, pending confirmation from a server exception or query inspection. A temporary SQL-generation probe could not restore in the restricted shell. Check the analogous owner list before closing the issue.
- Owners cannot write through the property resource policy and portfolio collections are scoped by the verified owner ID. Nested file reads first authorize the property and then resolve the file link within that property. Those safeguards were exercised for downloads. The failed journey leaves independent coverage for several Phase 5 route-level denials pending.
- File checks are structural format/size validation. There is no configured malware scanner. The default Development setting allows authorized reads of quarantined files. Production download requires a clean verdict. A production scanner and blob/database reconciliation procedures are still needed.
- Review consistency of property archival versus concurrent uploads, ordering and configuration changes. These operations use different record/lock paths, and archival races were not tested. Confirm archive behavior for files and contracts with acceptance tests.

## Recommended continuation order

1. Diagnose and fix the property list HTTP 500 and validate owner list SQL translation. Add isolated paging/filtering and authorization assertions so one failed list cannot mask invitation or portal coverage.
2. Verify owner invitation/resend/link/disable/archive and property reactivation with real SQL. Finish file attachment and upload workflows, archive consistency and historical-data tests. Review storage operations and document the production scanner boundary.
3. Close form behavior, missing action translations, localized dates/numbers, and initial bundle budget. Exercise the Manager and Owner workflows through the Angular proxy with fictitious accounts, including the requested Playwright cases and nine screenshots.
4. Review and apply the incremental migration only in the approved continuation. Then rerun builds, the dependency audits, all suites and live smoke tests, and update Phase 5 release documentation.

## Checkpoint 5.1 · Backend stabilization · 2026-11-26

The baseline failed again with HTTP 500. A temporary xUnit console capture recorded `System.InvalidOperationException` from EF Core. The failing LINQ predicate included `new PropertySummary(...).City.Contains(@term)`. The stack reached `EntityFrameworkQueryableExtensions.CountAsync` from `PortfolioRepository.PageAsync` before page retrieval. The position-only constructor projection hid the DTO member-to-column mapping from EF when the service composed the search predicate. The capture was removed after diagnosis. The local diagnostic is retained in the ignored `.local-tools/checkpoint51-before.log`.

`backend/DTOs/Properties/PortfolioModels.cs` and `backend/Repositories/PortfolioRepository.cs` now give the property and owner query projections explicit member assignments. Their JSON fields, endpoints, paging limits, tenant filters, and owner restrictions are preserved. SQL Server executes the filtering, ordering, count and page query. The implementation performs no whole-table materialization.

`tests/HostGrid.Persistence.Tests/AuthenticationTests.cs` extends the recovered journey with a second filtered and sorted Owner portfolio page and a query explicitly selecting another Owner. A new Manager regression covers searching and sorting owner profiles across two pages, excludes a matching Owner in another organization, and verifies detail lookup. The focused two-test run passed, including the original invitation, activation, property access, mutation denial and cross-tenant assertions.

### Local migration

Reviewed the idempotent forward SQL for `20261125234521_PropertyPortfolio` from the Phase 3 migration. It adds four columns, makes `Owner.UserId` nullable, rebuilds the unique index with `WHERE UserId IS NOT NULL`, and inserts nine amenity records. The transaction contains no row deletes, table drops or truncation. The catalog had no conflicting seed keys or codes and had zero owners, properties, stored files and amenities before application. EF applied the migration to `HostGridDb`. Both prior migrations are still recorded, the owner foreign keys are enabled and trusted, the replacement index is unique and filtered, and the affected catalog contains nine amenities with the other three verified counts unchanged.

### Checkpoint verification

- Locked .NET restore and build passed with zero build warnings and errors. The focused SQL Server tests passed **2 of 2**, then the full run passed **30 of 30**, including the original 26 regression tests. No skips.
- Angular 21 on Node 24 built successfully. Vitest passed **12 of 12**. The existing initial bundle is **500.31 kB**, 315 bytes over the warning budget. The existing NG8113 warning for the unused `EmptyState` import also remains.
- The existing Playwright suite passed **3 of 3** in Edge against a running Angular dev server. The audit's cold-start login timeout did not recur on this run. The suite still covers the earlier workspace flows.
- API liveness and SQL readiness returned HTTP 200 with `Healthy` directly and through the Angular development proxy. EF lists all three migrations as applied and found no pending model changes. `git diff --check` passed.

### Phase 5 classifications after 5.1

This table updates the recovery baseline above. The other classifications carry forward. Strict verified completion is now **15% by module count**, with commission history, seasonal pricing and migrations fully verified.

| Module | Current classification | Checkpoint evidence or remaining work |
|---|---|---|
| 1. Owner management | PARTIALLY IMPLEMENTED | Scoped paging, detail, invitation and activation verified. Link, resend, disable and archive acceptance remain. |
| 2. Property management | PARTIALLY IMPLEMENTED | Filtered paging works in SQL Server. Reactivation, simultaneous writes and feature browser acceptance remain. |
| 10. Backend APIs | PARTIALLY IMPLEMENTED | Owner and property list/query failures resolved. Remaining nested endpoints still need their acceptance tests. |
| 11. Authorization and tenant isolation | PARTIALLY IMPLEMENTED | Verified cross-tenant request rejection, cross-organization owner association rejection, Owner list scope, Owner detail scope and write denial. Remaining access lifecycle cases and explicit portfolio administration acceptance remain. |
| 14. Owner portal | PARTIALLY IMPLEMENTED | The API verifies activated Owner scope and excludes someone else's property. Browser proof remains. |
| 17. Database migrations | COMPLETED AND VERIFIED | Forward SQL reviewed, migration applied and SQL catalog verified. |
| 18. Automated tests | PARTIALLY IMPLEMENTED | Focused SQL regressions verified. Dedicated Phase 5 browser scenarios remain. |
| 20. Final verification | PARTIALLY IMPLEMENTED | The list and migration blockers are resolved. Feature-level end-to-end acceptance and requested captures remain. |

Recommended Checkpoint 5.2 is owner account lifecycle and property management acceptance. Cover resend, link, disable and archive, plus property reactivation and concurrent edits. Keep private-file acceptance, browser portfolio flows, translations, bundle follow-up and captures in their own approved scope.

## Checkpoint 5.2 · Owner lifecycle and property management acceptance · 2026-11-26

The owner-specific `enable-access` endpoint restores the linked Owner membership through existing Identity services. The scoped lifecycle checks run under the user lock and reject an archived profile, a different membership role, and an activated account disabled at platform level. Disabling revokes refresh sessions and invalidates the existing access token. Resend rotates the pending invitation proof under the same lock used for activation. Linking requires matching email addresses and an eligible Owner membership in the organization. Archive retains properties and the associated closed financial record.

A Manager who re-enables a pending account can resend its invitation from the same details page. The earlier proof is invalidated when access changes. An activated Owner signs in again after reactivation. Archival eligibility continues to require all associated properties to be archived.

Real SQL Server tests cover the lifecycle, duplicate links, Manager and explicit Super Admin access, cross-tenant and Owner denials, concurrent property edits, unique references, archival, reactivation and structured audit history. The Manager browser journey covers creation and editing of owners and properties, invitation and scoped access changes, grid and table switching, server sorting, and French and Arabic layouts. The production size remains 500.31 kB, 315 bytes above the unchanged budget. All three migrations remain applied to `HostGridDb`; EF found no pending model changes. The [progress log](PROGRESS.md) lists the changed files and verification commands.

| Module | Classification | Checkpoint evidence or remaining work |
|---|---|---|
| 1. Owner management | COMPLETED AND VERIFIED | Scoped listing, details, forms, secure resend, activation, linkage, revocation, reactivation and historical archive verified. The existing owner details response still returns up to 100 properties and 100 contracts. |
| 2. Property management | COMPLETED AND VERIFIED | Core creation, editing, details, validation, lookup, filtering, statuses, generated reference, ownership and concurrent updates verified. |
| 10. Backend APIs | PARTIALLY IMPLEMENTED | Owner and core property endpoints verified. Nested portfolio endpoints retain their prior acceptance status. |
| 11. Authorization and tenant isolation | COMPLETED AND VERIFIED | Manager scope, explicit Super Admin scope, Owner read and write boundaries, unauthorized lifecycle actions, and revocation verified using real SQL. File-specific authorization remains tracked under private document storage. |
| 12. Angular owner management | COMPLETED AND VERIFIED | Manager create, edit, and account actions verified through the proxy. Vitest checks form errors, query parameters and role-specific visibility. |
| 13. Angular property management | PARTIALLY IMPLEMENTED | Core Manager property workflow verified through the proxy. Nested property tabs retain their prior acceptance status. |
| 14. Owner portal | PARTIALLY IMPLEMENTED | Owner API read/write boundaries verified. Live Owner browser journey remains. |
| 15. Internationalization and RTL | PARTIALLY IMPLEMENTED | Account actions translated into English, French and Arabic. Arabic mobile table containment verified. History labels and broader localization remain. |
| 16. Frontend performance | PARTIALLY IMPLEMENTED | Lazy routes retained. Initial bundle warning remains. |
| 17. Database migrations | COMPLETED AND VERIFIED | The three applied migrations are unchanged and the model matches the snapshot. |
| 18. Automated tests | PARTIALLY IMPLEMENTED | Passed 33 backend tests, 17 Angular unit tests and 4 Playwright tests, including the live Manager browser journey. Broader Phase 5 feature scenarios remain. |
| 19. Documentation | PARTIALLY IMPLEMENTED | Checkpoint evidence and local browser instructions recorded. Phase-wide release documentation remains. |
| 20. Final verification | PARTIALLY IMPLEMENTED | Checkpoint verification performed. Phase-wide acceptance and requested screenshots remain. |

Recommended next checkpoint is property photos and private document acceptance, including upload and ordering, contract attachment, scoped downloads and archive consistency. The remaining Owner browser journey and phase-wide localization, captures and release documentation are tracked separately.

## Checkpoint 5.3 · Property photos and private documents · 2026-11-26

Property files now use complete image decoding for JPEG, PNG and WebP, configured photo and document request limits and a shared visibility rule for previews and covers. File ordering and property archival use the same per-property lock. An optimistic concurrency test recorded one winning cover change and one stale rejection. Private downloads have attachment disposition for PDFs, a validated image media type for photos and no-store caching. The existing contract attachment path reuses the scoped, readable-document lookup. Referenced historical PDFs remain available after a rejected archive request.

The local provider stages and renames uploads, cleans up after a failed metadata save and retains ambiguous commits for read-only reconciliation. SQL Server tests cover unavailable storage, cleanup, cancellation and orphan detection. The [storage guide](FILE_STORAGE.md) documents the configuration, backup and reconciliation commands. The direct and proxied health checks passed, and all three migrations remain applied with a matching EF model.

| Module | Classification | Evidence or remaining work |
|---|---|---|
| 3. Management contracts | PARTIALLY IMPLEMENTED | Scoped PDF attachment and historical retention verified. Contract acceptance stays in Checkpoint 5.4. |
| 5. Property photos | COMPLETED AND VERIFIED | Valid formats, invalid signatures and incomplete images, previews, ordering, concurrent cover changes, archival and read access verified. Thumbnail generation remains a phase-wide performance task. |
| 6. Private document storage | PARTIALLY IMPLEMENTED | Local upload, authorized download, retention and reconciliation verified. A production scanner and its quarantine processing are still open. |
| 11. Authorization and tenant isolation | COMPLETED AND VERIFIED | Manager, explicitly scoped Super Admin, assigned Owner, other Owner and foreign Manager covered by SQL tests. The live Owner can preview and download without write controls. |
| 13. Angular property management | PARTIALLY IMPLEMENTED | Photos and Documents verified through the Angular proxy. Other nested property tabs retain their previous classifications. |
| 14. Owner portal | PARTIALLY IMPLEMENTED | Live Owner gallery previews and private document downloads verified. Other portal acceptance scenarios remain. |
| 16. Frontend performance | PARTIALLY IMPLEMENTED | Lazy protected image requests retained. The initial bundle is 500.33 kB, 333 bytes over the unchanged warning budget. |
| 17. Database migrations | COMPLETED AND VERIFIED | Existing migrations and snapshot verified. |
| 18. Automated tests | PARTIALLY IMPLEMENTED | Passed 37 SQL Server tests, 20 Angular tests and 5 Edge tests. Broader Phase 5 tests remain. |
| 19. Documentation | PARTIALLY IMPLEMENTED | Added the storage guide and fictitious gallery and document captures. Phase-wide documentation remains. |
| 20. Final verification | PARTIALLY IMPLEMENTED | Checkpoint builds and verification completed. Remaining phase-wide acceptance is tracked above. |

Recommended Checkpoint 5.4 is Contracts & Commission acceptance. Production scanning and broader Phase 5 release verification remain open.

## Checkpoint 5.4 · Management contracts and commission history · 2026-11-26

The interrupted implementation was resumed in place. The SQL Server closure-range query now compares year and month separately, and all four targeted regressions passed. The active contract displays a warning when its agreed rate differs from the current property rate. Managers can edit scheduled agreements, schedule the end of open agreements, archive ended records and explicitly schedule rate changes. Owners can see their property’s agreement, authorized PDF and dated rates through the same property authorization.

| Module | Classification | Evidence or remaining work |
|---|---|---|
| 3. Management contracts | COMPLETED AND VERIFIED | Contract lifecycle covered by SQL Server and the live Manager and Owner browser scenario. Archive retains the agreement and its private PDF. |
| 4. Commission rate history | COMPLETED AND VERIFIED | Effective dates, default rate, history, same-date concurrency and closed-period protection covered. The agreed rate and dated property rate are shown separately. |
| 6. Private document storage | PARTIALLY IMPLEMENTED | Contract download and retention verified. Production scanner integration remains open. |
| 13. Angular property management | PARTIALLY IMPLEMENTED | Contract forms and dated commission history verified. Inventory, seasonal pricing and listing acceptance remains for Checkpoint 5.5. |
| 17. Database migrations | COMPLETED AND VERIFIED | Local `HostGridDb` records `ManagementContractArchive` and the three earlier migrations. EF found no model drift. |

The [contract guide](CONTRACTS_AND_COMMISSION.md) documents authorization, endpoints and the approved rate policy. The [progress log](PROGRESS.md) records files and test results. The [agreement](screenshots/phase5-management-contract.png) and [commission history](screenshots/phase5-commission-history.png) screenshots use fictitious data. Historical financial corrections remain deferred to adjustment records.

Final verification passed 39 SQL Server tests, 23 Angular unit tests and 6 Playwright tests. The reservation fixture’s commission snapshot and rowversion were preserved after a successful rate change. The initial Angular bundle remains 500.33 kB. Checkpoint 5.5 awaits approval.

## Checkpoint 5.5 · Amenities, inventory, pricing and listings · 2026-11-26

The existing amenity catalog, inventory CRUD, listing metadata, and seasonal rule locking remain in use. Inventory notes, base-rate configuration and multi-night pricing with rule provenance have been implemented. Saturday/Sunday weekends and custom → seasonal → weekend → base precedence carry forward from the recovered implementation. SQL Server passed all 41 tests. Local `HostGridDb` records all five migrations with no model drift.

Angular passed 25 unit tests, and the latest production initial bundle is 501.81 kB. API liveness and SQL readiness passed directly and through the Angular development proxy. Refer to the [inventory and pricing guide](INVENTORY_AND_PRICING.md) for endpoints, authorization, and the current-base fallback for historical quote previews.

| Module | Classification | Evidence |
|---|---|---|
| 7. Amenities and inventory | COMPLETED AND VERIFIED | Catalog, duplicate rejection, amenity addition and removal, inventory CRUD, notes and role guards passed SQL and Angular coverage. |
| 8. Seasonal pricing | COMPLETED AND VERIFIED | All four rate sources, half-open dates, concurrent overlap rejection, night-by-night preview and preservation of stored reservation prices passed SQL verification. |
| 9. Listing metadata | COMPLETED AND VERIFIED | Airbnb, Booking.com and Direct metadata, unsafe URL rejection and duplicate protection passed SQL verification. |
| 17. Database migrations | COMPLETED AND VERIFIED | The additive inventory notes migration and four previous migrations are applied. EF found no model drift. |

All seven Playwright tests passed in Edge. The Manager scenario saved amenities, inventory, a base and weekend rate, and a manual listing, then previewed the rates. Captures are available for [inventory](screenshots/phase5-inventory.png) and [pricing](screenshots/phase5-pricing.png). Owner Portal Acceptance is the next checkpoint, pending approval.

## Checkpoint 5.6 · Owner portal acceptance · 2026-11-26

The Owner portfolio reuses shared property views. This checkpoint adds operational statuses, preview feedback and an explicit distinction between quotes and past reservation prices. Owner file metadata omits uploader identities, and the read-only tab strip omits the audit tab.

Both builds passed, and the initial Angular bundle remains 501.81 kB. All five migrations are present in local HostGridDb with no model drift. Both API health checks passed through the Angular proxy.

SQL Server passed 42 tests. Angular passed 26 tests, including a regression for invitations with encoded plus signs. The final production bundle after the activation-link fix was 501.81 kB. The [Owner portal guide](OWNER_PORTAL.md) records available pages, permissions and production malware scanning requirements.

The recovered final Edge run passed all eight scenarios. The live Owner journey covered invitation through Mailpit, activation, optional authenticator enrollment, session restoration, read-only property access, scoped PDF download, revocation and a fresh sign-in after reactivation. All seven screenshots are available in the [Owner portal guide](OWNER_PORTAL.md). **Module 14, Owner Portal, is completed and verified for Phase 5 scope.** Production malware scanning and phase-wide release verification remain open. Checkpoint 5.7 awaits explicit approval.

## Checkpoint 5.7 · Final Phase 5 verification · 2026-11-26

See the [Phase 5 acceptance matrix](PHASE_05_ACCEPTANCE.md) for current classifications and the [progress log](PROGRESS.md) for the completed regression steps. All 42 SQL Server tests passed. The existing local database was read only, and SQL and EF confirmed all five migrations with no model drift. Production package advisory checks reported zero findings, and npm reported 16 moderate findings in development tooling.

Angular unit tests passed 26 of 26. The production initial bundle fell from 501.81 kB to 420.71 kB with the 500 kB warning budget in place. The final Edge regression passed nine of nine in 50.2 seconds, including the explicit Super Admin scope check and all eight baseline scenarios. Earlier runs exposed browser synchronization issues and a fixture-induced authentication HTTP 429. The final suite kept its assertions and waited for the authentication window to expire after fixture provisioning.

Phase 5 local acceptance is complete. The [acceptance report](PHASE_05_ACCEPTANCE.md) identifies production malware scanning and operator-assisted MFA recovery as open, documents the matching .NET 10 SDK prerequisite and records the Phase 6 availability, concurrency and commission snapshot decisions. Phase 6 awaits explicit approval.
