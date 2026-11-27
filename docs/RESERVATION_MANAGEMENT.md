# Reservation management

Managers select an Active property, guest, manual booking source, contractual dates, guest counts and pricing. They can save a Pending hold or create a Confirmed reservation directly. The availability check is advisory. The write locks the affected organization/property keys, verifies the date range, capacity, closure status and conflicts, and commits the booking, accepted prices and audit together. Africa/Casablanca determines the business date. Persisted system timestamps are UTC.

Pending holds can move between properties in one transaction. Confirmed revisions preserve their property, guest identity, source, external reference, pricing basis and captured rate. Date or price changes that alter accepted prices require explicit approval on the edit form. CheckedIn, CheckedOut, Cancelled and NoShow records reject general editing. For a legacy Confirmed booking without accepted prices, show the gap and use a future correction workflow. Pending-to-Confirmed, manual expiry and stay actions stay with the lifecycle checkpoint.

Audit records retain the actor, operation, property, dates, status and accepted price revision number. Guest, external-reference and notes changes are recorded as flags. Contact values, external identifiers, free-text notes and document content stay out of the audit payload.

Under `/api/v1/organizations/{organizationId}/reservations`, GET lists scoped summaries and accepts property, owner, source, status, payment, arrival/departure ranges, internal reference, external reference and guest name filters. The list supports explicit arrival, departure and reference sort and bounded pagination. POST creates, GET `/{id}` returns details, PUT `/{id}` revises using RowVersion, and GET `/{id}/history` returns paged accepted prices. POST `/pricing-preview` proposes direct pricing. See [historical entry](HISTORICAL_RESERVATIONS.md) for its separate routes.

External references are trimmed, optional and case-sensitive as platform identifiers under SQL Server binary collation. A filtered unique index covers organization, source and external reference across properties. A duplicate returns `409 external_booking_reference_duplicate`, a stale write returns `409 availability_stale`, an overlap uses its conflict code, and temporary lock failures return `503`. Owners and administrators without a scoped Manager membership cannot use this module. The later Owner portal will define a separate read projection.

## Screenshots

The following captures use fictitious local data.

- [Guest directory](screenshots/phase6-guests.png) and [guest details](screenshots/phase6-guest-details.png)
- [Reservation directory](screenshots/phase6-reservations.png), [creation](screenshots/phase6-reservation-create.png) and [nightly price review](screenshots/phase6-reservation-pricing.png)
- [Reservation details](screenshots/phase6-reservation-details.png), [revision](screenshots/phase6-reservation-revision.png) and [historical entry](screenshots/phase6-historical-entry.png)
- [Dark theme](screenshots/phase6-reservation-dark.png) and [Arabic RTL at 390 px](screenshots/phase6-reservation-arabic-mobile.png)
