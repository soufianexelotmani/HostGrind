# Guest management

Managers use **Guests** to search the organization directory by name, filter by contact availability, sort and page results, and open a guest's linked reservation history. Name is required. Email and telephone are optional. Creating a guest never merges profiles. Contact edits use RowVersion and keep existing bookings linked to the original guest identifier.

Reservation forms let Managers search names, check explicit email or telephone matches, and choose a profile or create a separate one. Contact matching uses a case-normalized email and normalized phone digits. A standard Moroccan local number beginning with zero matches its +212 or 00212 form. Keep the entered phone display value. Unprefixed international numbers are compared as entered digits.

Under `/api/v1/organizations/{organizationId}/guests` use GET and POST for list and creation, GET and PUT `/{id}` for detail and update, POST `/suggestions` for body-only contact matching, and GET `/{id}/reservations` for paged history. Suggested contacts never appear in a GET URL. Audit records identify the guest and actor without storing contact values. Owners and non-Manager administrators do not have access to the guest directory.
