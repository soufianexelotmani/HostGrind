# Calendar UI

Use the portfolio date controls to move by 30 days, jump to Today, or choose an exact date. Selecting a column date updates the daily indicators. Select a property name to open its individual calendar. The property calendar offers day, week and month controls and a keyboard-accessible agenda below the calendar.

Available cells open a date selection form with an exclusive end date. The same action is available from the agenda. Event labels combine the reference or block reason with a status. Source colors reuse the HostGrid semantic theme tokens. The preview and block forms use the shared native-dialog component. The layout follows the selected workspace language and text direction.

The property component is deferred until the individual property route renders. `@fullcalendar/core`, `@fullcalendar/angular`, `@fullcalendar/daygrid`, and `@fullcalendar/interaction` are pinned to `6.1.20` and licensed under MIT. The Angular package's published peer dependencies cover Angular 12 through 21. The portfolio uses Angular and the existing design system. [FullCalendar changelog](https://github.com/fullcalendar/fullcalendar/blob/main/CHANGELOG.md) · [Standard license](https://fullcalendar.io/license)

## Live verification fixture

Use a disposable SQL Server catalog, Mailpit, a synthetic Manager, and a synthetic Owner assigned to the Active property used by the booking scenario. Set `HOSTGRID_E2E_CALENDAR_MANAGER_RECOVERY_CODE` to an unused recovery code distinct from the booking scenario's code. The calendar scenario also reads `HOSTGRID_E2E_BOOKING_PROPERTY` and the existing `HOSTGRID_E2E_FILES_OWNER_EMAIL` and `HOSTGRID_E2E_FILES_OWNER_PASSWORD` fixture settings. For the cancellation display check, seed a synthetic Cancelled Direct reservation with reference `CALENDAR-CANCELLED`, no actual check-in, and the interval from Casablanca today plus 22 days through today plus 23 days. Set `HOSTGRID_CALENDAR_SCREENSHOT_DIR` to capture the screens. Leave authentication rate limiting enabled and allow the provisioning window to expire before the suite starts.

## Synthetic screenshots

- [Portfolio](screenshots/phase63-portfolio.png), [filters](screenshots/phase63-filters.png), and [daily indicators](screenshots/phase63-daily-indicators.png)
- Property [month](screenshots/phase63-property-month.png), [week](screenshots/phase63-property-week.png), and [day](screenshots/phase63-property-day.png)
- [Reservation preview](screenshots/phase63-reservation-preview.png) and [prefilled reservation form](screenshots/phase63-quick-reservation.png)
- [Block creation](screenshots/phase63-block-create.png) and [block revision](screenshots/phase63-block-edit.png)
- [Arabic RTL](screenshots/phase63-arabic-rtl.png), [dark theme](screenshots/phase63-dark.png), and [390 px mobile agenda](screenshots/phase63-mobile.png)
