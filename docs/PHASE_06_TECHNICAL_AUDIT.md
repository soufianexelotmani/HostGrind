# Phase 6 technical audit

This is the 6.0 baseline. For the schema changes and endpoint implemented in 6.1, see the [availability engine](AVAILABILITY_ENGINE.md).

## Existing persistence

The eight booking entities already have SQL Server mappings in `BookingConfigurations.cs`. The schema uses DateOnly for day boundaries, UTC datetime2 for action times, decimal(18,2) for money, decimal(5,2) for percentages and restrictive relationships. Tenant filters fail closed and composite foreign keys enforce same-organization relationships. No booking controller, booking business workflow or mounted reservation/calendar feature exists. The current Manager and Owner menu labels are Upcoming.

| Entity | Current support | Gap or future decision |
| --- | --- | --- |
| Guest | Tenant ID, name, optional phone/email, RowVersion | Scoped validation and search. No automatic deduplication from contact info, which can be shared. |
| Reservation | Tenant-qualified property and guest, unique internal ref per tenant, filtered unique external ref per tenant/property/source, three sources, date/guest counts, three revenue modes, commission and payment fields, actual in/out timestamps, notes and RowVersion. Tenant/property/date range index. | Service must enforce capacity and lifecycle. Current rate and commission are non-null, so a Pending row cannot accurately represent an uncaptured snapshot. No explicit confirmation time, lookup date, or release boundary. |
| ReservationNight | One row per tenant/reservation/date. Tenant/property/night index. Parent FK ties it to the reservation's property. Stores net allocation, commission amount and rate. | Index permits multiple reservations per property/date. Stores finance allocations; no separately accepted per-night price, source or override provenance. Parent property is an EF alternate-key component, so reassignment with dependent rows is not an ordinary update. |
| ReservationPayment | Positive amount, paid date, method, receiver and optional reference. One reservation can have multiple payments. | Deposits can be ordinary partial receipts. No dedicated deposit flag or payment idempotency key. Balances and status need an approved amount-due rule and transactional validation. |
| ReservationCancellation | One per reservation, reason, UTC cancellation, nonnegative retained fee | No automatic fee or recognized revenue. Current unique record makes reopening and recancelling a separate design decision. Retained fee is currently required, so distinguish approved zero from unknown if business requires both. |
| ReservationRefund | Positive amount, refunded date, payer, optional reference and same-reservation payment link | No cumulative cap or replay handling. Decide allowed methods, payer rules and unlinked refunds. |
| ReservationStatusHistory | Old/new enum values, UTC change, actor and tenant-qualified reservation | No reason field or initial nullable state. Use structured creation and reason-bearing action audit. Add fields only if business retrieval or integrity requires them. |
| CalendarBlock | Same-tenant property, exclusive dates, Active/Cancelled, reason, RowVersion and range index | No persisted source enum or typed linkage. Use current manual block fields first. Add maintenance/request linkage only when later workflows need it. |
| Property | Owner, rowversion, MaxGuests, property and operational status, local in/out times | Enforce capacity in scoped booking writes. Availability eligibility and how property edits affect bookings need explicit rules. |
| CommissionRateHistory | Tenant/property lookup, dated windows, percentage, version and indexed history | Reuse `PropertyPortfolioService.EffectiveCommissionAsync` semantics and default rate after authorization. Persist the approved lookup day and immutable result at confirmation. |

## Reuse and candidate additive changes

- Reuse `AuthenticatedTenantContext`, resource authorization and `PortfolioAccess`. Owner queries join to authorized properties before projection. Use scoped services, validators, DTOs and existing audit plumbing. SQL checks enforce field shape, FKs and reference uniqueness. Service transactions enforce capacity, occupancy, aggregates, closed-period protection and append-only history.
- Reuse `PropertyPricing.PreviewAsync` for proposals in MAD, capped at 366 nights per preview. `NightlyQuote` already provides date, amount, source and rule ID. External bookings preserve manually recorded actual amounts. Store accepted nightly prices separately from net/commission allocations when needed, with nullable quote provenance and explicit override marker. Do not invent an external nightly breakdown.
- Plan an additive nullable pre-confirmation representation for capture UTC time and effective lookup date. At the write checkpoint choose whether to make existing snapshot/amount nullable with constrained status rules or introduce a distinct optional confirmation snapshot. Preserve original rate data and never fabricate confirmation timestamps for legacy rows. If nightly price and finance allocation share a row, resolve required allocation columns before inserting pre-confirmation price rows.
- A nullable first-released business date and UTC/actor evidence, or a typed release record, can preserve original reservation dates. Choose after boundary and reversal rules are agreed. A generic status change is insufficient. No new occupancy table or index is required for the recommended lock by itself. Check query plans on representative range data before adding indexes.
- For property moves, default proposal permits only Pending before dependent historical rows, with locks on both properties. If approved scope includes confirmed moves, define transfer, commission and parent-key/child history handling explicitly. Never rewrite a property FK on financially referenced history as a shortcut.
- Future ledger idempotency and explicit correction records are conditional on approved workflows. Payment/refund aggregation should lock or version the reservation consistently and prevent over-refunds. Phase 7 owns settlement and corrections spanning financial periods.

## Baseline evidence

The approved Phase 5 baseline and audit history are in [Phase 5 acceptance](PHASE_05_ACCEPTANCE.md). In Checkpoint 6.0, read-only SQL queries against HostGridDb confirmed eight booking tables, 18 enabled and trusted booking foreign keys, and zero rows in Reservation, ReservationNight and CalendarBlock. Empty tables provide no evidence of handling legacy booking values. Recheck row counts and snapshot compatibility in any deployment database before a future migration.

HostGridDb records `20261125223356_InitialHostGrid`, `20261125225143_AuthenticationSecurity`, `20261125234521_PropertyPortfolio`, `20261126165727_ManagementContractArchive` and `20261126200005_PropertyInventoryNotes`. EF reported no pending migration or model drift. No migration was created or applied to HostGridDb. The existing integration test fixture exercised migration replay only inside its disposable test catalogs.

See [progress](PROGRESS.md) for commands, build/test results and environment constraints. Production malware scanning and operator-assisted MFA recovery are open production blockers. A matching .NET 10 SDK remains a toolchain prerequisite.
