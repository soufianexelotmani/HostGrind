# Owner portal

## Getting access

A Manager creates the Owner profile, assigns its properties and sends an invitation to the profile email. The Owner follows the email link, sets a password and signs in. Owners can enable an authenticator in Profile and keep their one-time recovery codes. The browser restores active sessions through the refresh cookie. Profile also provides password and authenticator settings. Sign out revokes the current session.

Disabling an Owner membership revokes existing sessions. Re-enabling the membership restores eligibility to sign in. Owners authenticate again to obtain a new session. Development uses Mailpit for fictitious invitations. See the [authentication guide](AUTHENTICATION.md).

## My Properties

The portfolio shows assigned properties in grid and table views. Search, type and status filters, sortable table columns and paging query the API. Cards show property and operational status, base nightly rate and a private cover photo when available. The list handles an empty portfolio and a search without matches.

Property details provide the overview, photos, active contract and retained agreements, applicable commission, amenities, inventory, pricing previews and readable documents. Historical quotes use configured pricing rules and the current base fallback. They do not establish the price charged for any reservation.

The same shared pages support French, English and Arabic. Arabic switches the shell and tab keyboard direction to RTL. Light, dark and system themes use the shared preferences. At narrow widths the table scrolls inside its own region, and the workspace uses mobile navigation.

## Permissions and private files

Each API request requires an authenticated session and an authorized organization. Owner reads are scoped to the persisted Owner profile linked to the account. Changing the property or file identifier cannot expand that scope. The API enforces read-only access even when invoked directly. Owner file views omit internal uploader identifiers and audit events. Manager and platform account APIs enforce their own authorization policies.

Photo and document requests carry the session bearer token in the authorization header. File links contain opaque IDs. Downloads use private, no-store responses, and the browser revokes temporary preview and download URLs. Unavailable previews show a localized error. Quarantined file metadata is suppressed from the Owner view unless the Development preview exception is enabled. Production publication still requires a configured scanner and clean result. Retained contract PDFs use the same property-scoped download endpoint. See [private property files](FILE_STORAGE.md).

## Routes and upcoming modules

- `/owner` opens the dashboard and `/owner/properties` opens the assigned portfolio. `/owner/properties/{id}` opens a read-only property. `/security` opens the signed-in user's security settings.
- The underlying property routes are `GET /api/v1/organizations/{organizationId}/properties` and `GET /api/v1/organizations/{organizationId}/properties/{propertyId}`. Related reads include `photos`, `documents` and `pricing-preview` under the property.
- Reservations, calendar, revenue, expenses, payouts, interventions, the consolidated document area, requests and reports are marked Upcoming. Current documents are available within each property. The portal does not show placeholder financial figures.

## Local browser acceptance

Run `owner-portal.spec.ts` with a local Angular proxy, a Development API backed by a disposable LocalDB catalog, and Mailpit. Set `HOSTGRID_E2E_MANAGER_EMAIL`, `HOSTGRID_E2E_MANAGER_PASSWORD`, `HOSTGRID_E2E_OWNER_PORTAL_MANAGER_RECOVERY_CODE` and `HOSTGRID_E2E_MAILPIT_URL` for the fictitious fixtures. Set `HOSTGRID_OWNER_SCREENSHOT_DIR` to collect captures. The scenario creates a new Owner and properties, uses one Manager recovery code and checks the issued Owner session before and after revocation.

Checkpoint 5.6 passed **8 of 8** Edge scenarios, including the full Owner journey. The backend suite passed **42 of 42** and the Angular unit suite passed **26 of 26**. These checks used fictitious accounts and isolated test catalogs. See [Phase 5 acceptance](PHASE_05_ACCEPTANCE.md) for the final phase-wide regression results.

- [Desktop portfolio](screenshots/phase5-owner-desktop.png)
- [Mobile portfolio](screenshots/phase5-owner-mobile.png)
- [Property details](screenshots/phase5-owner-details.png)
- [Contract and commission](screenshots/phase5-owner-contract.png)
- [Private documents](screenshots/phase5-owner-documents.png)
- [Arabic RTL portal](screenshots/phase5-owner-arabic.png)
- [Dark mode portal](screenshots/phase5-owner-dark.png)
