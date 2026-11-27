# Reservation business rules

## Approved decisions

1. Pending reservations immediately block availability.
2. An authorized Manager expires or releases Pending reservations manually in V1. No automatic hold-expiry background job is required.
3. Capture the commission rate at confirmation using the authoritative effective `CommissionRateHistory` lookup.
4. Date or price edits after confirmation preserve the original rate snapshot.
5. Same-day checkout and check-in are allowed. Cleaning and operational readiness remain independent of booking availability.
6. Reject a reservation whose adults plus children exceed `Property.MaxGuests`.
7. Completed reservations preserve history. Corrections require an explicit, authorized and audited workflow.
8. A NoShow may release remaining nights only after explicit Manager approval.

## Checkpoint 6.1 approvals

- New reservations require an Active property. Inactive and Archived properties remain visible where authorized. Operational status, including NeedsCleaning, does not create a booking conflict. Use an explicit block for dates that are unavailable.
- Use Africa/Casablanca for business dates. Allow same-day reservations and adjacent stays. Historical entry is allowed in principle under scope, conflict, audit and closed-period protection. Its complete workflow needs approval and cannot invent historical confirmation times.
- An authorized Manager manually expires Pending as Cancelled with reason `PendingExpired`. Retain the reservation, release time, reason and actor. The lifecycle action ships in a later checkpoint.
- Only Pending reservations can move directly to another property. Capture the effective commission on the Casablanca calendar date of confirmation. Unconfirmed Pending starts without a captured rate. Preserve legacy snapshots and post-confirmation rate history.
- A Manager explicitly releases unused NoShow or early-checkout nights. Retain contractual dates, actual times and original money. Persist the first released night, time and actor. The release primitive checks today's Casablanca date, the contractual boundaries and actual checkout where applicable. Exceptional scenarios still require approval.
- FullCalendar Standard is approved for classic views. Use an Angular grid for a property per row. Checkpoint 6.3 owns calendar UI.

## Checkpoint 6.2 approvals

- Use a distinct audited workflow for completed historical stays. Require an Active property, its current capacity, the actual historical confirmation date and supporting private PDF evidence accepted by the scan policy. Record date-only information without synthesizing timestamps.
- Create in Pending or directly in Confirmed through the authoritative confirmation logic. Resolve the historical confirmation date and the normal Casablanca confirmation date from the property rate history. Return an explicit error if a rate is absent or ambiguous.
- Propose Direct nightly rates and allow explicit overrides. Record actual Airbnb and Booking.com amounts. Allocate a remaining total in whole cents across unresolved nights, with residual cents starting at the earliest unresolved date. Label every generated value.
- Preserve unchanged nightly prices after confirmation. Propose new nights and retain removed prices in previous revisions. Allow gross-minus-fees and direct net without double deduction. Round the reservation commission half away from zero to cents and retain the exact net and captured rate as inputs.
- Suggest existing guests from normalized contact matches. Require explicit selection and never merge. External references are optional and unique per organization and source. CheckedIn changes require dedicated stay actions in 6.4.

## Proposed lifecycle

`ReservationStatus` already provides Pending, Confirmed, CheckedIn, CheckedOut, Cancelled and NoShow. The following is a proposed allowed transition list, subject to the open decisions below. Manager actions require an authorized organization, property access, the expected RowVersion, a server UTC action time, and atomic status-history and structured audit records with actor, old/new status, reservation and correlation. Record reasons for exceptional or release actions without copying sensitive free text into logs. Owners are readers. Any administrative mutation needs separately approved authority.

| Transition | Preconditions and authorization | Availability | Commission | Payment | Timestamps and evidence |
| --- | --- | --- | --- | --- | --- |
| New → Pending | Scoped Manager, valid property, guest, party size and non-conflicting dates | Hold full interval immediately | No captured rate | Start Unpaid. Receipts use a separate action. | Record creation and actor. Current history requires an old status, so use a creation audit event. |
| Pending → Confirmed | Scoped Manager, accepted pricing, revalidate capacity and overlap under the same lock | Hold continues | Capture once using approved lookup date | Unchanged | Persist confirmation UTC time and lookup date, and record history. |
| Pending → Cancelled | Scoped Manager, explicit cancellation or manual expiry with `PendingExpired` | Release held nights atomically and retain original dates | Leave uncaptured | Unchanged absent separate receipt/refund | Record UTC action time, reason and Manager. |
| Confirmed → CheckedIn | Scoped Manager, check-in timing policy satisfied, actual time recorded | Hold booked nights | Preserve | Unchanged | Set ActualCheckInAt and append history. |
| Confirmed → Cancelled | Scoped Manager, before check-in, reason required | Release future availability, preserve dates and history | Preserve | Unchanged absent separate refund | Cancellation record and UTC action time. No fee calculation implied. |
| Confirmed → NoShow | Scoped Manager after the approved arrival cutoff, no check-in, reason required | Continue holding nights until separately released | Preserve | Unchanged | Status history and NoShow action time. |
| CheckedIn → CheckedOut | Scoped Manager, actual checkout at or after actual check-in | Ordinary checkout ends the scheduled stay. Early checkout release requires a separate approved rule. | Preserve | Unchanged | ActualCheckOutAt and history. |
| NoShow → NoShow with release | Explicit scoped Manager action, reason and approved effective date | Release only the approved remaining interval | Preserve | Unchanged | Record release date, approving actor, UTC time and audit. No duplicate state-history row required. |

Reject transitions outside the allowed list, including Pending → CheckedIn, Confirmed → CheckedOut, CheckedIn → Cancelled or NoShow, and reopening terminal states, unless a later approved exception defines an explicit workflow. Disallow generic status edits or deletion. Keep payment status and property operational status independent.

## Occupancy and booking edits

Use business dates and half-open windows `[CheckInDate, CheckOutDate)`. The overlap predicate is `ExistingStart < RequestedEnd AND ExistingEnd > RequestedStart`. Adjacent stays do not overlap. For edits, exclude only the matching entity ID in its own source table and tenant, and check every other reservation and block. An availability preview is advisory. The write repeats the check under the property lock.

| Record | Effect |
| --- | --- |
| Pending, Confirmed, CheckedIn | Hold the booked interval. A checked-in extension needs a fresh conflict and capacity check. |
| CheckedOut | Historical booked and actual stay evidence remains. Normal checkout unblocks after the original exclusive end. An early checkout cannot silently make unelapsed nights bookable. |
| Cancelled | Releases the hold for current/future booking. Original dates and cancellation event remain for historical reporting. |
| NoShow | Blocks until explicit release or interval end. After release, truncate only the effective occupancy window at the approved first released night. Preserve original dates, snapshots and pre-release history. |
| Calendar block | An active manual block holds its half-open interval regardless of event filters. An ongoing cancellation retains elapsed nights and releases the interval from today's Casablanca business date. Future cancellations release the entire future block. |
| Maintenance and Owner-requested blocks | Only an explicitly authorized Manager block or later approved Manager action creates occupancy. Maintenance status and a submitted or approved request alone do not change availability. |

Historical reporting distinguishes planned occupancy, released holds and realized stays. Preserve stay dates and actual timestamps. Do not backfill occupancy or infer realized nights from a payment or status change. Existing obligations remain visible after a property goes inactive or is archived. Operational status continues independently.

The twelve approved calendar decisions are in the [root rules](../BUSINESS_RULES.md#calendrier-et-blocages-de-dates-63). The [daily summary contract](CALENDAR_API.md#daily-summary) records the approved occupancy definition.

## Pricing, commission and recorded cash

Direct booking uses the existing MAD nightly proposal in priority order Custom > Seasonal > Weekend > Base. A Manager may explicitly override rates. For Airbnb and Booking.com record actual amounts manually. Persist accepted amounts separately from pricing configuration. Preserve the original commission rate through post-confirmation date/price changes. A checked-out record may only be corrected through an authorized audit workflow. Closed or already allocated amounts remain protected.

```text
NetPlatformRevenue = GrossRevenue - PlatformFees
ManagerCommission = NetPlatformRevenue × CommissionRate
OwnerShareBeforeCharges = NetPlatformRevenue - ManagerCommission
OwnerProfit = OwnerShareBeforeCharges - PropertyExpenses
```

Convert a stored percentage such as 20 to 0.20 at the calculation boundary. For direct-net input use the entered net, leaving unknown gross and fee fields null. Net 14,000 MAD at 20% yields commission 2,800 MAD and an owner share of 11,200 MAD. After 100 MAD expenses, owner profit is 11,100 MAD.

Proposed actions record deposits and partial receipts as positive payment entries with received-on date, method, receiver and optional reference. Record refunds separately, with optional link to a payment from the same reservation. Validate cumulative refunds and retries within a transaction. Cancellation records a reason, timestamp and any explicitly recorded retained fee once that policy is agreed. A status transition never creates a fictitious receipt, refund or payout. Define remaining payable from the agreed amount due and verified receipts/refunds once the cancellation and payment rules are approved. Keep booked revenue, recorded cash and Owner payouts independent. Rounding, recognition, mixed-cash settlement, allocations and closure remain Phase 7.

## Decisions requiring approval

- **Before financial historical corrections** define how to coordinate future closure writers atomically and how to adjust already allocated financial nights. Checkpoint 6.2 protects closed periods and preserves historical dates, accepted prices and legacy financial rows.
- **Before confirmation and lifecycle actions** decide deterministic handling when a commission schedule change races with confirmation. Confirm earliest check-in, NoShow cutoff, and any exceptions for same-date release after disputed or ambiguous occupancy. Extended stays after checkout, NoShow reversal, missing actual times and terminal-state corrections need separate approval. Decide whether the manual Pending expiry screen needs optional due-date metadata.
- **Before payment and historical correction actions** confirm amount-due basis by booking source, receiver/payer eligibility, overpayments, refund caps with or without an original payment, retained fee input and lawful adjustments to completed stays. Decide whether V1 owner details need summarized booked or paid amounts. Keep actual cash, history and any future Phase 7 recognition separate.
