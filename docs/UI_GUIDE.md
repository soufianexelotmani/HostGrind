# HostGrid UI guide

## Identity and tokens

HostGrid uses an original roof-and-grid mark in `frontend/public/brand/`. The compact mark is used at narrow widths and in the collapsed sidebar. The expanded wordmark pairs the mark with the localized application's typeface. The full SVG is available for export. The original riad image was generated for HostGrid and is stored as a local JPEG. The `BRANDING` injection token supplies the default name and assets and can accept authorized server-provided branding in a later phase.

| Token | Light | Dark |
|---|---|---|
| Brand Terracotta | `#B45338` | `#B45338` |
| Brand Warm Sand | `#E8C49C` | `#E8C49C` |
| Page | `#FAF7F2` | `#201914` |
| Surface | `#FFFDF9` | `#2A211B` |
| Text | `#30251F` | `#FAEFE2` |
| Muted text | `#68584E` | `#CEBAAA` |
| Primary control | `#9B442E` | `#EFAD91` |
| Control outline | `#938073` | `#9A8071` |

The stylesheet also defines success, warning, error, information, divider, focus, and radius tokens. Use `--border` for decorative separators and `--control-border` when a boundary conveys an interactive control. Include text or an accessible label for statuses. Financial values use tabular digits where the selected font supports them.

Manrope Variable serves French and English. Tajawal serves Arabic at weights 400, 500, and 700. Both families are bundled by Angular from Fontsource under OFL-1.1. Responsive page titles use `clamp()`. Maintain the base font size for primary copy and reserve smaller sizes for secondary labels.

## Theme and translation

The Signals-based preferences service applies light, dark, or system to the document. The same `.hostgrid-dark` selector drives the CSS tokens and PrimeNG Aura preset. Tailwind CSS 4 runs through PostCSS. The cascade order is `theme, base, primeng, components, utilities`. Store only the language, theme mode, and sidebar collapse choice under `hostgrid.ui.v1`.

Transloco loads the shared UI messages and the original Phase 3 auth messages through one local loader. The preference service updates document `lang` and `dir`. Use logical CSS properties, keep email addresses, one-time codes, and financial display values directionally isolated, and label controls in each language. Shared formatters use Moroccan locales, MAD, and `Africa/Casablanca` for business instants. Date-only form values remain ISO date strings. The browser supplies the native date picker UI.

## Layout and routes

The public authentication layout pairs a riad photograph with the existing auth forms, their recovery flows, and language and theme controls. The protected shell has the collapsible sidebar, breadcrumbs, profile menu, notification panel, search, and a mobile More drawer. The existing authentication service and route guards still determine role access.

- Dashboard routes are `/admin`, `/manager`, and `/owner`. Account security is `/security`.
- The desktop sidebar gives way to bottom navigation at 900 px. The login image hides at 840 px. Dashboard panels stack at 650 px. Use safe-area insets for fixed navigation.
- The menu configuration is centralized in `core/config/navigation.ts`. The command palette opens with Ctrl+K or Cmd+K and filters available routes within the selected authorized workspace. The search interface is separate from future business-entity retrieval.
- Dashboard foundations show empty states and account actions. Upcoming modules and notifications are labeled as such. Dashboard data, reporting, and notification delivery integrate in later phases.

## Component conventions

Use the typed standalone components in `shared/components/`. Page headers accept projected actions, breadcrumbs accept translated labels, and the feedback primitives cover KPI cards, status badges, empty and error states, a spinner, a skeleton, confirmation dialogs, and PrimeNG toasts. Native dialogs and drawers supply Escape, background inertness, and focus restoration. Shell skip links and focus rings support keyboard users.

Forms can use the search field, filter toolbar, labeled field shell, date range, MAD currency display, and file picker. Connect form error text to its control with `aria-describedby`. File picker rules provide client feedback. Backend upload validation and persistence will be added with file workflows.

The table accepts server rows, total, page, search, sort, and loading state. Its typed outputs are requests for the consuming feature to fetch data. It does not synthesize rows or paginate locally. Feature-specific filter controls project into the toolbar. Column visibility is local to the component. Use real headers, stable row keys, and an accessible name for the keyboard-scrollable region.

## Accessibility checks

The verified light palette measures 14.66:1 for body text, 6.68:1 for muted text, and 6.44:1 for primary-button text. The dark equivalents are 13.90:1, 8.43:1, and 7.84:1. Control boundaries measured 3.71:1 in light mode and 4.28:1 in dark mode. These are token-pair checks. They do not certify every component or future layout.

The browser suite verifies 360, 390, 768, 1024, and 1440 px widths, palette focus, Escape dismissal, Arabic direction, and role restrictions. Reduced-motion settings suppress ornamental animation. Future production hosting must provide a CSP nonce for dynamic Angular and PrimeNG styles in keeping with the existing security policy.

## Screenshots

The screenshots are from the implemented Angular screens. Dashboard captures use fictitious role fixtures. Passwords, MFA secrets, and recovery codes are excluded.

To regenerate them, set `HOSTGRID_SCREENSHOT_DIR` to the absolute path of `docs/screenshots` and run `npm.cmd run test:e2e` from `frontend/`. The suite uses Chromium by default and supports installed Edge via `HOSTGRID_BROWSER_CHANNEL=msedge`.

| Screen | Capture |
|---|---|
| English sign-in, desktop | [Login](screenshots/login-desktop.png) |
| Super Admin, desktop light | [Administrator](screenshots/admin-desktop.png) |
| Manager, French tablet dark | [Manager](screenshots/manager-tablet-dark.png) |
| Owner, Arabic mobile dark | [Owner](screenshots/owner-mobile-rtl.png) |
