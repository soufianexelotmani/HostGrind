# Calendar management

Managers open Calendar in the workspace to see 30 consecutive Casablanca business dates and paged property rows. The calendar can search by property name or reference, filter by owner, and narrow the event display by source, status and block reason. Cancelled reservations and blocks have explicit display controls. Hidden events still occupy applicable nights.

Daily indicators use the selected day and the matching property scope. The portfolio grid uses a scrollable Angular table with sticky property labels and dates. A stacked agenda keeps property links, event previews, and date selection usable on mobile. The individual property calendar offers all-day day, week, and month views using FullCalendar Standard. [API and indicator definitions](CALENDAR_API.md) · [Manual blocks](CALENDAR_BLOCKS.md) · [Calendar UI](CALENDAR_UI_GUIDE.md)

Select an available range and choose Create reservation or Block dates. The existing reservation form opens with the property and dates prefilled and waits for explicit submission. Calendar writes refresh the projections. A reservation preview links to the existing detail and eligible revision forms. Dragging and resizing are disabled.

## Local performance measurements

The disposable catalog had 100 synthetic calendar properties, each with 13 Pending reservations and 13 blocks across the larger window. Authenticated requests went through the Angular development proxy into LocalDB. Times are the median of three runs after one warm-up per case, including response transfer. Sizes are uncompressed JSON bytes.

| Property rows | 30 days | 93 days |
| --- | --- | --- |
| 10 | 68 ms, 46,863 B | 72 ms, 128,881 B |
| 50 | 90 ms, 233,703 B | 136 ms, 644,081 B |
| 100 | 128 ms, 467,260 B | 196 ms, 1,288,096 B |

Edge showed the default page of 30 matching properties in 1,658 ms after changing the date and search controls. This includes input synchronization, debounce, fetch, and rendering. A horizontal scroll moved the desktop grid and painted within the two-frame sample of 33 ms. These are local observations from one synthetic workload. The default UI paginates properties in groups of 30.

The populated portfolio feed uses a property count and page, two event projections, and two hold projections. SQL logs showed server-side paging and date filtering. The statement count excludes authorization and the separate daily summary request. No production query plan or timing target was assumed from the local run.
