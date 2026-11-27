# Management contracts and commission rates

## Contract lifecycle

A contract belongs to the property and records the Owner associated with that property at creation. The API takes the Owner from the authorized property. Dates use the Casablanca business calendar. An interval starts on its start date and ends just before its optional end date. Contracts can meet at a shared boundary. They cannot overlap, including with historical or archived agreements.

| State | Behavior |
|---|---|
| Scheduled | A Manager or explicitly scoped Super Admin can edit dates and the agreed rate before the start date. Edits require the current contract rowversion. A readable PDF can be attached if none has been linked. |
| Active | The property overview returns the contract that covers today's business date. Terms stay fixed after the contract takes effect. A contract with no end date can receive a future end date after its start. |
| Ended | The agreement remains visible in history with its original terms. |
| Archived | Archiving is an explicit action for ended contracts. It records `ArchivedAt` and retains the agreement, its dates, its Owner, and its PDF. |

The existing create endpoint can record a historical agreement in a gap if no closed month would be affected. Once attached, the signed PDF is retained. Upload it in the property Documents tab and select it from the contract form. Contract downloads use the same scoped private document route. See [Private property files](FILE_STORAGE.md).

Contract creation and edits check the affected monthly closure records. Termination checks the portion of the schedule being ended. Archival changes only the archive timestamp and audit history.

## Which commission rate applies

`CommissionRateHistory` is the source of the property's configured effective rate. Each record applies from `EffectiveFrom`, inclusive, until `EffectiveTo`, exclusive. If no property record covers the requested date, the organization default applies. The default is initialized to 20%.

A contract stores the agreed rate independently as part of its terms. Leaving its rate empty copies the property's effective rate as of the contract start date. Editing the agreement does not schedule a property commission change. Scheduling a property rate does not rewrite any existing contract. The UI labels the agreed contract rate and the current property rate separately. This follows the approved decision to require explicit rate changes and keep historical terms fixed.

The active contract shows a warning when its agreed rate differs from the effective property rate on the current business date. The warning is visible to authorized Managers and Owners. Neither value is changed by the warning.

Rates accept 0% through 100% with at most two decimal places. A rate can start today or later and must follow the latest recorded start. Adding a rate closes the previously open rate interval at the new start date. Existing rates stay intact. Scheduling is rejected if the affected range intersects a closed property month. An earlier date or previously entered rate requires a separate correction policy.

## Authorization, validation and API

All routes below use `/api/v1/organizations/{organizationId}/properties/{propertyId}`. A Manager can write within their authorized organization. A Super Admin reaches the resource through an explicit organization path. An Owner can read their own property's contract history and rates. Property authorization applies on every nested request.

| Operation | Endpoint relative to the property |
|---|---|
| Active contract, contract history, current commission and rate history | `GET /` |
| Create a contract | `POST /contracts` |
| Edit a scheduled contract | `PUT /contracts/{contractId}` |
| End an open contract on a future date | `PUT /contracts/{contractId}/end` |
| Archive an ended contract | `POST /contracts/{contractId}/archive` |
| Schedule a property commission rate | `POST /commission-rates` |
| Get the current or as-of property commission rate | `GET /commission-rates/effective?date=YYYY-MM-DD` |
| Download an attached PDF | `GET /documents/{documentId}` |

The same transaction-owned property lock serializes configuration writes, attachment changes and property archival. Contract edits, termination and archival check rowversion. The rate schedule has a unique property-and-start-date constraint. The service checks ordering and the previous interval under the lock, and writes a structured audit event with each successful mutation.

## Deferred financial rules

The approved basis remains net platform revenue, with platform fees deducted once from gross revenue or omitted when the input is already net. Manager commission uses that net amount and the applicable rate. Property expenses reduce Owner profit after commission. This checkpoint stores and retrieves rates and terms. Reservation snapshot timing, allocation and rounding, cancellation recognition, mixed-cash settlement, and any policy for correcting effective terms or replacing signed PDFs remain outside its scope.
