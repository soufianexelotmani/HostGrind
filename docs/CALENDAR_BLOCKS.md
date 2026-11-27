# Manual calendar blocks

A Manager can block an Active or Inactive property for a finite interval. Dates use Casablanca business days and an exclusive end. The first night can be today. Select Maintenance, Owner use, Unavailability, Renovation, or Other. Other requires at least three non-whitespace characters in the description. Descriptions and cancellation reasons are limited to 500 characters.

A block stays with its original property. To move a future block, cancel it and create a new block for the other property. The reservation-versus-block and block-versus-block checks run under the existing transaction-owned SQL Server property lock. A revision excludes only the block being revised. Conflicts and stale rowversions return HTTP 409, and transient lock failures return HTTP 503.

Future blocks can be revised with the current rowversion. For an ongoing block, elapsed nights and the original record are retained. A revision closes the old record's effective interval at today's Casablanca date, then creates the replacement for today or later in the same transaction. The returned ID identifies the replacement. A Manager can cancel an eligible block with a reason. Cancellation preserves dates, property and reason and records the actor, UTC timestamp, and first released night. An old block whose exclusive end has arrived is immutable. Changes that touch closed financial periods are rejected.

The additive `CalendarBlockLifecycle` migration keeps the `Reason` column and places new classifications in `Category`. Legacy blocks retain their existing reasons and nullable metadata. An active legacy block still holds its full interval. Legacy cancelled blocks with no release metadata remain cancelled without a hold. There is no delete route.
