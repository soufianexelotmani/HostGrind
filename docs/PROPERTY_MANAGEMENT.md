# Property and Owner management

The properties directory is available under `/manager/properties`, `/admin/properties` and `/owner/properties`. The Manager and Super Admin owner directories are under `/manager/owners` and `/admin/owners`. Use the tenant selector when managing multiple organizations. Owner property pages grant read-only access to the linked Owner's own resources.

All portfolio routes are scoped to `/api/v1/organizations/{organizationId}`.

| Endpoint | Purpose |
|---|---|
| `GET /owners` and `GET /owners/{id}` | Search, filter, sort and page the owner directory, or view an owner and associated properties and contracts. |
| `POST /owners` and `PUT /owners/{id}` | Create a profile or edit contact details with its version. |
| `POST /owners/{id}/{action}` | Invitation or resend (`invitation`), account `link`, `disable-access`, `enable-access` or `archive`. |
| `GET /properties` and `GET /properties/{id}` | Search, filter, sort and page properties, or open the full scoped detail. |
| `GET /properties/types` and `GET /properties/amenities` | Read the type and amenity catalogs. |
| `POST /properties` and `PUT /properties/{id}` | Create or edit a property. |
| `PUT /properties/{id}/status` | Archive, reactivate or mark inactive with the current version. |
| `/properties/{id}/contracts` and `/properties/{id}/commission-rates` | Contract lifecycle and dated rate configuration. |
| `/properties/{id}/photos` and `/properties/{id}/documents` | Private upload, streaming, archive and photo ordering. |
| `/properties/{id}/amenities`, `inventory`, `base-rate`, `seasonal-rates` and `listings` | Property configuration. |
| `GET /properties/{id}/nightly-rate` and `GET /properties/{id}/pricing-preview` | Calculate configured quotes in MAD, using custom, seasonal, weekend then base pricing. |

Manager writes use authorized memberships. Super Admin access is explicit and scoped. Server-side rules enforce ownership, version checks and tenant-qualified associations. Property ownership reassignment remains restricted. An Owner profile cannot be archived until its properties are archived. Associated properties and contracts shown on Owner management details are each capped at 100 records.

See [Owner portal](OWNER_PORTAL.md), [contracts and commission](CONTRACTS_AND_COMMISSION.md), [private storage configuration](FILE_STORAGE.md), and [amenities, inventory and pricing](INVENTORY_AND_PRICING.md) for workflow details and limits.
