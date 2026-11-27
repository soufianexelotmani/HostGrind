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
- Use Standard FullCalendar DayGrid on the individual property page for day, week and month navigation with an accessible list fallback. Verify Angular compatibility, versions and licenses before installation. [Pricing and license](https://fullcalendar.io/pricing) · [Plugin index](https://fullcalendar.io/docs/plugin-index) · [Angular connector](https://fullcalendar.io/docs/angular)
- Default to an Angular portfolio grid with property rows and 30 consecutive dates from the Casablanca business date. Use date-window paging, sticky labels, and a narrow-screen stacked alternative. Preserve ISO date-only boundaries with an exclusive end. Use source color plus accessible status and reason labels in French, English and Arabic, including RTL. [FullCalendar direction](https://fullcalendar.io/docs/direction)
- An available date selection prefills the existing reservation form for explicit review and submission. All booking edits use the existing revision form. Reservation dragging and resizing are disabled.
- Managers can create, change or audit-cancel blocks on exactly one property. Every block has a finite exclusive end. Historical records are retained, retroactive creation is forbidden, and existing conflicts remain strict under the shared lock. The five translated reasons are Maintenance, OwnerUse, Unavailability, Renovation and Other.
- Cancelled reservations are hidden by default. Showing them does not change authoritative holds. The server computes scheduled arrivals and departures and distinct occupied, available and blocked property counts for the requested business date.
- Owner-facing calendar UI is deferred to 6.6. Any shared read endpoint enforces assigned-property scope and omits Manager-only information. See the [twelve approved decisions](../BUSINESS_RULES.md#calendrier-et-blocages-de-dates-63).

The owner confirmed three follow-up rules for 6.3. Daily occupancy counts Confirmed and CheckedIn nights plus realized historical nights. New manual blocks accept Active and Inactive properties. For ongoing block edits and cancellation, keep elapsed nights and allow future availability changes starting on today's Casablanca business date. See the [calendar API contract](CALENDAR_API.md) for the daily indicator definitions.
