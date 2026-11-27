# Inventory, pricing and manual listings

## Amenities and inventory

The nine catalog entries are Wi-Fi, air conditioning, television, kitchen, refrigerator, washing machine, parking, elevator and balcony. EF applies the catalog migration once. The property uses a versioned replacement list of amenity IDs, with a unique association per amenity. Empty selection removes all associations. The Angular labels are localized.

Inventory tracks item name, whole nonnegative quantity, optional condition, and optional description or notes. Name, condition and notes are limited to 200, 120 and 1,000 characters respectively. Search matches name, condition or notes within the property inventory. Edit and delete require the current inventory rowversion, and successful mutations record property audit events.

## Nightly pricing

The same resolver serves a single night and a bounded multi-night preview. Each quote carries the night, MAD decimal amount, source and the winning rule identifier when a rule applies. Precedence is custom, seasonal, weekend, then the property's current base rate. The rule start is inclusive and end is exclusive, with dates on the Casablanca business calendar. Weekend rules can apply on Saturday, Sunday or both. Seasonal and custom rules use the selected days within their periods.

Different priorities may overlap. Same-priority rules may intersect by date only when their selected weekdays cannot match the same night. The existing transaction lock on the property serializes changes before the overlap check. Dates in the past cannot be introduced through the configuration form or API. A scheduled rule can be edited with its rowversion until its start day. Existing effective and expired rules remain in history.

The base rate is the current fallback, and changing it affects new quotes. It is not a dated rate history. A preview of earlier dates is a quote using the current base fallback, rather than a reconstruction of what was charged. Price configuration does not change stored reservation values. The preview neither creates a reservation nor computes platform fees or commissions.

## Manual platform metadata

Each property has at most one listing per source, covering Airbnb, Booking.com and Direct. The optional external reference and URL are entered manually. A URL must be absolute HTTP or HTTPS with a host and without embedded credentials. The application stores the link for navigation and never fetches it. Updates require the current listing rowversion.

## API

All routes are relative to `/api/v1/organizations/{organizationId}/properties`.

| Method and path | Purpose |
|---|---|
| `GET /amenities` | List reference amenities. |
| `PUT /{id}/amenities` | Replace the property amenity selection. |
| `POST /{id}/inventory` | Create an inventory item. |
| `PUT /{id}/inventory/{itemId}` | Edit a versioned item. |
| `DELETE /{id}/inventory/{itemId}` | Delete a versioned item. |
| `PUT /{id}/base-rate` | Set the current base nightly rate in MAD. |
| `POST /{id}/seasonal-rates` | Create a weekend, seasonal or custom rule. |
| `PUT /{id}/seasonal-rates/{rateId}` | Edit a future rule. |
| `GET /{id}/nightly-rate?night=YYYY-MM-DD` | Resolve one nightly quote. |
| `GET /{id}/pricing-preview?start=YYYY-MM-DD&end=YYYY-MM-DD` | Resolve 1 to 366 consecutive nights, excluding end. |
| `PUT /{id}/listings/{source}` | Save manual metadata by source. |
| `GET /{id}` | Read the inventory, selected amenities, rules and listings. |

Managers write within an authorized organization. Owners can read and preview rates for their own properties. Super Admin uses the explicit organization route. Nested writes validate the property, tenant and target resource. The pricing service performs the same scoped property authorization for both single and multi-night quotes. It is available to a future authorized booking flow without depending on a web controller.
