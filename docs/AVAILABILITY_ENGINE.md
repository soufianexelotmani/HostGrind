# Availability engine

## Read contract

`GET /api/v1/organizations/{organizationId}/properties/{id}/availability?checkInDate=2032-02-27&checkOutDate=2032-03-01`

The response includes only `checkInDate`, `checkOutDate`, `available` and `status`. Status is one of `available`, `booking_conflict`, `calendar_block_conflict` or `property_not_bookable`. When the queried range crosses both a reservation and a block, the reservation category takes precedence. The response is private and uncached. It contains no booking IDs, notes, guest or payment data.

A Manager reads properties in an authorized organization. An Owner reads assigned properties only. Super Admin reads require the explicitly selected active organization and existing tenant selection audit. The public request has no exclusion field. A successful read is advisory. Every mutation must acquire the SQL lock and recheck availability.

| Result | HTTP behavior |
| --- | --- |
| Available, overlap or property not bookable | GET returns 200 and the relevant response status. |
| Missing, zero or reversed range | 400 ProblemDetails with `invalid_date_range`. Unparseable dates return standard model-binding 400. |
| No session, no tenant access, or inaccessible property | 401, 403 or scoped 404. |
| Rejected future write due to overlap or property state | 409 with `booking_conflict`, `calendar_block_conflict` or `property_not_bookable`. |
| Invalid party size | 400 with `guest_capacity_invalid`. |
| Stale reservation or block state | 409 with `availability_stale`. |
| Lock or transient SQL failure | 503 with `availability_lock_timeout`, `availability_lock_cancelled`, `availability_lock_deadlock`, `availability_lock_failed` or `availability_database_busy`. |
| Uncertain commit result | 503 with `availability_commit_unknown`. Reload state before retrying. |

## Server service

`PropertyAvailability.QueryAsync` verifies read scope and checks both interval sources. The Checkpoint 6.2 [reservation service](RESERVATION_MANAGEMENT.md) uses the same coordinator and core admission checks for all public reservation writes. `SaveBlockAsync` and `ReleaseAsync` remain internal. Release records the authorized Manager's first released night, UTC time and actor. An early checkout requires an actual checkout date. The effective release starts on or after that Casablanca business date and today, within the contractual stay. NoShow must have no actual check-in.

Stays use `[check-in, checkout)` and compare `start < requestedEnd && end > requestedStart`. A reservation and adjacent stay can share a checkout/check-in date. Pending, Confirmed and CheckedIn hold nights. CheckedOut protects historical occupancy and its unused booked interval until an authorized release. A Cancelled reservation without evidence of check-in releases the whole hold, and a NoShow blocks until its recorded release. An Active block holds its dates. Cancelled blocks do not hold. Existing historical dates are readable. Backdated booking writes require the approved historical workflow and closed financial periods remain protected.

`AvailabilityTransactions.ExecuteAsync` acquires transaction-owned locks on the EF connection in canonical GUID order. All write delegates execute inside the same Read Committed transaction, with freshly read property state. The database saves changes and audit before commit. `UpdatePropertyAsync` puts existing status, archival and capacity writes in that protocol. For status changes the original owner and portfolio locks follow the availability lock. Delegates are database-only and replayable. See [lock design](RESERVATION_CONCURRENCY_DESIGN.md).

The migration adds nullable release date/time/actor evidence with a check and restrictive actor FK. It also makes reservation-level commission rate and amount nullable for new Pending holds. Existing values remain in place. ReservationNight, historical dates, prices and actual timestamps retain their existing meaning. Existing range indexes cover the overlap queries. No occupancy table is required.

## Integration boundary for later checkpoints

Use the shared protocol for booking creation, confirmation or reactivation with occupancy effects, date edits, pending reassignment, extensions, cancellation and manual expiry, check-in/out outcomes with availability changes, NoShow release, and manual or future request/maintenance block creation, change, cancellation and activation. Keep changes to booking eligibility under the property lock. Future finance closure creation needs to share coordination with booking writes that check closed periods. Manual Pending expiry must record `PendingExpired`, the Manager and release time as specified in [the business rules](RESERVATION_BUSINESS_RULES.md).
