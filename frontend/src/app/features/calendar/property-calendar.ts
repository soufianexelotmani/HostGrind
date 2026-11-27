import { Component, computed, input, output, viewChild } from '@angular/core';
import { FullCalendarComponent, FullCalendarModule } from '@fullcalendar/angular';
import { CalendarOptions, EventInput } from '@fullcalendar/core';
import arabic from '@fullcalendar/core/locales/ar';
import french from '@fullcalendar/core/locales/fr';
import dayGrid from '@fullcalendar/daygrid';
import interaction from '@fullcalendar/interaction';
import { businessToday, CalendarEvent, CalendarHold, DateWindow } from './calendar-models';

@Component({
  selector: 'hg-property-calendar', imports: [FullCalendarModule],
  template: '<full-calendar [options]="options()" [events]="entries()" />',
  styleUrl: './property-calendar.scss',
})
export class PropertyCalendar {
  readonly day = input.required<string>();
  readonly language = input.required<string>();
  readonly events = input.required<CalendarEvent[]>();
  readonly holds = input.required<CalendarHold[]>();
  readonly windowChanged = output<DateWindow>();
  readonly datesSelected = output<DateWindow>();
  readonly eventSelected = output<CalendarEvent>();
  private readonly surface = viewChild(FullCalendarComponent);

  protected readonly options = computed<CalendarOptions>(() => ({
    plugins: [dayGrid, interaction], locales: [french, arabic], locale: this.language(),
    direction: this.language() === 'ar' ? 'rtl' : 'ltr', timeZone: 'UTC', initialDate: this.day(), now: () => businessToday(),
    initialView: 'dayGridMonth', headerToolbar: { start: 'prev,next today', center: 'title', end: 'dayGridDay,dayGridWeek,dayGridMonth' },
    editable: false, eventStartEditable: false, eventDurationEditable: false,
    selectable: true, dayMaxEvents: true, height: 'auto',
    datesSet: change => this.windowChanged.emit({ startDate: change.startStr.slice(0, 10), endDate: change.endStr.slice(0, 10) }),
    select: selection => this.datesSelected.emit({ startDate: selection.startStr.slice(0, 10), endDate: selection.endStr.slice(0, 10) }),
    eventClick: info => {
      const event = info.event.extendedProps['calendarEvent'] as CalendarEvent | undefined;
      if (event) this.eventSelected.emit(event);
    },
  }));

  protected readonly entries = computed<EventInput[]>(() => [
    ...this.holds().map(hold => ({ start: hold.startDate, end: hold.endDate, allDay: true,
      display: 'background', classNames: ['hg-calendar-hold'] })),
    ...this.events().map(event => ({ id: event.id, start: event.startDate, end: event.endDate, allDay: true, title: event.title,
      classNames: ['hg-calendar-event', event.tone], extendedProps: { calendarEvent: event } })),
  ]);

  goto(day: string): void { this.surface()?.getApi().gotoDate(day); }
}
