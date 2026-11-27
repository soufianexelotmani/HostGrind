# Phase 6 | Reservations and Calendar

Checkpoint 6.0 audited the Phase 5 baseline. Checkpoint 6.1 adds the [availability engine](AVAILABILITY_ENGINE.md) and shared SQL transaction boundary. The applicable architecture is Angular 21, one ASP.NET Core 10 Web API, EF Core Code First and SQL Server. Keep controllers, domain-specific services and repositories, DTOs, mappers and validators in the current application.

## Product scope

Managers manually record Airbnb, Booking.com and Direct bookings, with an optional external reference. V1 has no platform APIs, iCal, automated imports, synchronization or scraping. A source enum and tenant-scoped external reference allow a future integration without granting it a new access path today. Phase 6 covers guests, booking and block conflicts, a portfolio and property calendar, lifecycle actions, recorded receipts and refunds, and scoped Owner views. Phase 7 handles rounding, accounting recognition, mixed-cash settlement, monthly allocations and closures, and payouts. Phase 8 owns maintenance and Owner request workflows. Those workflows can call the same block action once authorized.

The [reservation rules](RESERVATION_BUSINESS_RULES.md) distinguish approved behavior from proposed transitions and unresolved cases. The [technical audit](PHASE_06_TECHNICAL_AUDIT.md) maps the existing model and migrations. The [concurrency design](RESERVATION_CONCURRENCY_DESIGN.md) specifies the shared transaction boundary. The [checkpoint plan](PHASE_06_CHECKPOINTS.md) lists acceptance gates.

Checkpoint 6.2 adds Manager-only [guest management](GUEST_MANAGEMENT.md), [reservation management](RESERVATION_MANAGEMENT.md), [accepted pricing](RESERVATION_PRICING.md) and [historical entry](HISTORICAL_RESERVATIONS.md). Direct Confirmed creation captures the effective history rate. The Pending-to-Confirmed action remains scheduled for 6.4.

## Authorization and data exposure

| Capability | Manager | Owner | Super Admin |
| --- | --- | --- | --- |
| Browse reservations and calendar | Authorized organization | Assigned properties only | Explicit admin policy and selected organization |
| Create, edit, confirm, check in/out, cancel, record NoShow | Authorized organization | Denied | Deny ordinary booking mutations by default. Any administrative exception requires a separate policy and owner approval. |
| Release Pending or NoShow, manage calendar blocks | Authorized organization. NoShow release requires explicit approval. | Denied. An Owner request, when implemented, does not create a block. | Same administrative restriction |
| Guest contact and notes | Scoped details for operational use only | Omit names, contacts and notes by default | Scoped support access only when separately authorized and audited |
| Payments, cancellations and refunds | Scoped operational recording and history | Proposed reservation payment status and approved summary on owned properties. Omit payment references, payment instruments and private reasons. | Selected organization and explicit support access. No default financial writes. |
| Historical correction | Proposed explicit permission, mandatory reason and audit | Denied | Separate authorized path only if approved |

Resolve tenant from verified memberships or explicit administrative scope. Authorize the associated property before returning reservations, nights, payment/refund history or calendar blocks. Guest search is Manager-scoped. Owners get narrow DTOs with property, opaque booking reference, dates, status and guest counts when useful. Keep contact fields out of calendar feeds, URLs, telemetry, caches and audit payloads. Return consistent forbidden or scoped not-found results without revealing another tenant's IDs. Keep private responses out of PWA caches.

## Calendar design approved for 6.3

- Lazy-load a calendar feature in the existing shell. Use one bounded all-day interval feed with authorized property filters, Manager-only Owner filters, booking source/status filters, and explicit block inclusion. Separate the display filter from availability. Hidden events still occupy nights.
- For the individual property and aggregate portfolio event views, use Standard FullCalendar DayGrid for day, week and month navigation with an accessible list fallback. The official Angular connector supports Angular 21. Standard is MIT, including commercial use. Resource Timeline and Vertical Resource views are Premium. Verify the actual package versions and licenses before any later installation. [Pricing and license](https://fullcalendar.io/pricing) · [Plugin index](https://fullcalendar.io/docs/plugin-index) · [Angular connector](https://fullcalendar.io/docs/angular)
- If a property-per-row view is needed, use a small Angular table/grid with property rows and date columns, date-window paging and sticky labels. On narrow screens provide a stacked day list. Use shared tokens, visible status text, keyboard navigation, and FR/EN/AR labels. Preserve business dates as ISO date-only strings with an exclusive end. Set calendar direction from preferences and verify RTL navigation and date columns. [FullCalendar direction](https://fullcalendar.io/docs/direction)
- Keep one explicit Manager create/edit/cancel block form with a conflict message and refresh after success. Owner controls are read-only and the API enforces scope. Handle loading, empty and error states. No calendar dependency was installed in 6.0.
