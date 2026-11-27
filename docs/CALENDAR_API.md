# Calendar API

Both read endpoints use `/api/v1/organizations/{organizationId}/calendar`. Responses are authenticated and marked `no-store`. The organization is resolved from verified membership or explicit Super Admin tenant selection.

## Calendar feed

`GET /calendar?startDate=2032-01-01&endDate=2032-01-31`

- The dates are ISO business dates with an inclusive start and exclusive end. The default maximum is 93 days. `Calendar:MaximumDays` ranges from 42 to 93 to accommodate six-week month views. Invalid, missing, reversed, and excessive ranges return HTTP 400.
- Filter properties using `propertyId`, `ownerId` and `search`. `page` starts at 1, and `size` defaults to 30 and is capped at 100. Properties are sorted by name and then ID. The response includes the total property count before pagination.
- Use `bookingSource`, `reservationStatus`, and `blockReason` to filter display events. `includeCancelledReservations` and `includeCancelledBlocks` default to false. A Cancelled reservation status filter requires the explicit inclusion flag. The reason filter matches the classified category. Legacy records keep their original text and an unknown category.
- `reservations` and `blocks` contain original dates, even when an interval crosses the visible window. `reservationHolds` and `blockHolds` are occupancy-only intervals for the property page, independent of display filters. They carry property and date boundaries without reservation or block identifiers. Writes recheck availability under the shared SQL transaction lock.
- Managers can see guest display name, authorized stay and payment status fields, and reservation detail and eligible revision links. Pending reservations and Confirmed reservations with stored nightly pricing can show the revision link. Contacts, notes, external references, money and documents are absent. Owners only see their assigned properties. Manager fields and actions are omitted or null for Owners and Super Admins. Owner and reason filters require Manager scope.

## Daily summary

`GET /calendar/summary?date=2032-01-01`

The summary uses the selected Casablanca business date across the matching property scope, with the same property, owner and search filters as the feed.

| Indicator | Definition |
| --- | --- |
| Scheduled arrivals and departures | Counts reservations in Confirmed, CheckedIn or CheckedOut with a contractual arrival or departure on the date. A same-day turnover contributes one departure and one arrival. |
| Occupied properties | Distinct properties having a Confirmed or CheckedIn effective night, or a realized historical night on a CheckedOut or Cancelled stay with actual arrival and departure evidence. Uses the historical dates when present, otherwise interprets actual timestamps in Casablanca. |
| Available properties | Active properties without a reservation hold or active block during the night. Operational readiness such as NeedsCleaning has no effect. |
| Blocked properties | Distinct properties with an effective block on the night, including retained elapsed nights of cancelled blocks. |
| Pending-held properties | Distinct properties with an effective Pending hold. These holds consume availability and are excluded from occupancy. |

Availability indicators describe the night. An authoritative write also checks its own authorization and admission rules.

## Manual block operations

The bounded feed supplies the block list. Manager-only routes under `/api/v1/organizations/{organizationId}/calendar-blocks` provide the following operations.

| Verb and path | Contract |
| --- | --- |
| `GET /{id}` | Read the authorized block with its rowversion and action flags. |
| `POST /` | Send `{ propertyId, block: { startDate, endDate, reason, description } }`. Returns HTTP 201 and the ID. |
| `PUT /{id}` | Send `{ startDate, endDate, reason, description, version }`. Returns HTTP 200 and the ID of the revised block or its future replacement. |
| `POST /{id}/cancel` | Send `{ version, reason }`. Returns HTTP 204. Here `reason` is the cancellation text. |

Write reason values are `0` Maintenance, `1` OwnerUse, `2` Unavailability, `3` Renovation, and `4` Other. Rowversions are base64 in JSON. Dates use the same half-open semantics as the feed. The effective hold for a cancelled block ends at its recorded `availabilityReleasedFrom` boundary. See [the block lifecycle](CALENDAR_BLOCKS.md).
