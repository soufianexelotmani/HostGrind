import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, inject, signal, viewChild } from '@angular/core';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { TranslocoService } from '@jsverse/transloco';
import { catchError, EMPTY, firstValueFrom, of, switchMap, timer } from 'rxjs';
import { Preferences } from '../../core/config/preferences';
import { LoadingSpinner } from '../../shared/components/feedback';
import { Modal } from '../../shared/components/modal';
import { PageHeader } from '../../shared/components/page-elements';
import { BookingPage } from '../bookings/booking-page';
import { payments, sources, statuses } from '../bookings/booking-models';
import { Owner, Page } from '../properties/portfolio-api';
import { PortfolioContext } from '../properties/portfolio-context';
import { calendarCopy } from './calendar-copy';
import { addDates, businessToday, CalendarBlock, CalendarEvent, CalendarHold, CalendarProperty, calendarReasons,
  CalendarSummary, CalendarView, DateWindow, indicators, overlaps, validIsoDate } from './calendar-models';
import { PropertyCalendar } from './property-calendar';

type Filter = { search: string; ownerId: string; bookingSource: string; reservationStatus: string; blockReason: string;
  includeCancelledReservations: boolean; includeCancelledBlocks: boolean; page: number };

function groupByProperty<T extends { propertyId: string }>(rows: T[]): Map<string, T[]> {
  const groups = new Map<string, T[]>();
  for (const row of rows) {
    const group = groups.get(row.propertyId);
    if (group) group.push(row);
    else groups.set(row.propertyId, [row]);
  }
  return groups;
}

@Component({
  selector: 'hg-calendar-page', providers: [PortfolioContext],
  imports: [FormsModule, RouterLink, LoadingSpinner, Modal, PageHeader, PropertyCalendar],
  templateUrl: './calendar-page.html', styleUrls: ['../properties/portfolio-page.scss', './calendar-page.scss'],
})
export class CalendarPage extends BookingPage {
  private readonly calendarTranslator = inject(TranslocoService);
  protected readonly calendarPreferences = inject(Preferences);
  protected readonly propertyId = this.route.snapshot.paramMap.get('propertyId');
  private readonly initialDay = this.route.snapshot.queryParamMap.get('date');
  protected readonly day = signal(validIsoDate(this.initialDay) ? this.initialDay : businessToday());
  protected readonly referenceDate = signal(this.day());
  protected readonly propertyWindow = signal<DateWindow | null>(null);
  protected readonly window = computed(() => this.propertyId ? this.propertyWindow() :
    { startDate: this.referenceDate(), endDate: addDates(this.referenceDate(), 30) });
  protected readonly filters = signal<Filter>({ search: '', ownerId: '', bookingSource: '', reservationStatus: '', blockReason: '',
    includeCancelledReservations: false, includeCancelledBlocks: false, page: 1 });
  private readonly refreshVersion = signal(0);
  protected readonly owners = signal<Owner[]>([]);
  protected readonly data = signal<CalendarView | null>(null);
  protected readonly summary = signal<CalendarSummary | null>(null);
  protected readonly feedLoading = signal(true);
  protected readonly feedError = signal(false);
  protected readonly summaryError = signal(false);
  protected readonly indicators = indicators;
  protected readonly sources = sources;
  protected readonly statuses = statuses;
  protected readonly reasons = calendarReasons;
  protected readonly dates = computed(() => Array.from({ length: 30 }, (_, index) => addDates(this.referenceDate(), index)));
  protected readonly calendar = viewChild<PropertyCalendar>('propertyCalendar');
  protected readonly previewOpen = signal(false);
  protected readonly selectionOpen = signal(false);
  protected readonly blockOpen = signal(false);
  protected readonly cancellationOpen = signal(false);
  protected readonly selectedEvent = signal<CalendarEvent | null>(null);
  protected readonly draftProperty = signal<CalendarProperty | null>(null);
  protected readonly editingBlock = signal<CalendarBlock | null>(null);
  protected readonly draftStart = signal('');
  protected readonly draftEnd = signal('');
  protected readonly draftReason = signal<number | null>(null);
  protected readonly description = signal('');
  protected readonly cancelReason = signal('');
  protected readonly actionError = signal('');
  protected readonly saved = signal('');
  protected readonly selectionAccepted = signal(false);
  private readonly actionBusy = signal(false);
  protected readonly writing = this.actionBusy.asReadonly();

  protected readonly events = computed<CalendarEvent[]>(() => {
    const data = this.data();
    if (!data) return [];
    return [
      ...data.reservations.map(row => ({ id: row.id, propertyId: row.propertyId, startDate: row.checkInDate,
        endDate: row.checkOutDate, tone: 'source-' + row.source, reservation: row,
        title: `${row.reference} · ${this.t(statuses[row.status])}` })),
      ...data.blocks.map(row => ({ id: row.id, propertyId: row.propertyId, startDate: row.startDate,
        endDate: row.endDate, tone: 'block-event', block: row,
        title: `${this.blockLabel(row)} · ${this.c(row.status === 0 ? 'active' : 'cancelState')}` })),
    ].sort((left, right) => left.startDate.localeCompare(right.startDate));
  });

  private readonly eventsByProperty = computed(() => groupByProperty(this.events()));
  private readonly holdsByProperty = computed(() => groupByProperty([
    ...(this.data()?.reservationHolds ?? []), ...(this.data()?.blockHolds ?? []),
  ]));
  protected readonly propertyHolds = computed(() => this.holdsByProperty().get(this.propertyId ?? '') ?? []);
  protected readonly summaryFilters = computed(() => ({ date: this.day(), propertyId: this.propertyId,
    ownerId: this.filters().ownerId, search: this.filters().search }), {
    equal: (left, right) => left.date === right.date && left.propertyId === right.propertyId &&
      left.ownerId === right.ownerId && left.search === right.search,
  });

  constructor() {
    super();
    this.begin(async () => {
      for (const language of ['en', 'fr', 'ar'] as const)
        this.calendarTranslator.setTranslation({ calendar: calendarCopy[language] }, language, { merge: true, emitChange: false });
      if (!this.propertyId)
        this.owners.set((await firstValueFrom(this.api.get<Page<Owner>>(this.organization, 'owners', { size: 100 }))).items);
    });
    const feed = computed(() => ({ organization: this.ctx.organization(), stay: this.window(), filters: this.filters(),
      version: this.refreshVersion() }));
    toObservable(feed).pipe(switchMap(({ organization, stay, filters }) => {
      if (!organization || !stay) return EMPTY;
      this.feedLoading.set(true);
      this.feedError.set(false);
      this.data.set(null);
      return timer(120).pipe(switchMap(() => this.api.get<CalendarView>(organization, 'calendar',
        { ...stay, ...filters, propertyId: this.propertyId })),
        catchError(() => { this.feedError.set(true); return of(null); }));
    }), takeUntilDestroyed()).subscribe(result => { this.data.set(result); this.feedLoading.set(false); });

    const summary = computed(() => ({ organization: this.ctx.organization(), filters: this.summaryFilters(),
      version: this.refreshVersion() }));
    toObservable(summary).pipe(switchMap(({ organization, filters }) => {
      if (!organization) return EMPTY;
      this.summary.set(null);
      this.summaryError.set(false);
      return timer(120).pipe(switchMap(() => this.api.get<CalendarSummary>(organization, 'calendar/summary', filters)),
        catchError(() => { this.summaryError.set(true); return of(null); }));
    }), takeUntilDestroyed()).subscribe(result => this.summary.set(result));
  }

  protected c(key: keyof typeof calendarCopy.en): string {
    const language = this.calendarPreferences.language();
    const translation = this.calendarTranslator.translate('calendar.' + key, {}, language);
    return translation === 'calendar.' + key ? calendarCopy[language][key] : translation;
  }

  protected blockLabel(block: CalendarBlock): string {
    return block.reason === null ? block.legacyReason ?? this.c('legacy') : this.c(calendarReasons[block.reason]);
  }

  protected setFilter<K extends keyof Filter>(key: K, value: Filter[K]): void {
    this.filters.update(previous => ({ ...previous, page: 1, [key]: value }));
  }

  protected shift(days: number): void { this.navigateDate(addDates(this.referenceDate(), days)); }
  protected changePage(delta: number): void { this.filters.update(filter => ({ ...filter, page: filter.page + delta })); }
  protected navigateDate(day: string): void {
    if (!validIsoDate(day)) return;
    this.day.set(day);
    this.referenceDate.set(day);
    this.calendar()?.goto(day);
  }
  protected today(): void { this.navigateDate(this.data()?.businessDate ?? businessToday()); }
  protected refresh(): void { this.refreshVersion.update(value => value + 1); }
  protected changeWindow(value: DateWindow): void {
    if (this.window()?.startDate !== value.startDate || this.window()?.endDate !== value.endDate) this.propertyWindow.set(value);
    if (this.day() < value.startDate || this.day() >= value.endDate) this.day.set(value.startDate);
  }

  protected propertiesEvents(id: string): CalendarEvent[] { return this.eventsByProperty().get(id) ?? []; }
  protected nightEvents(id: string, date: string): CalendarEvent[] {
    return this.propertiesEvents(id).filter(event => event.startDate <= date && date < event.endDate);
  }
  protected nightUnavailable(id: string, date: string): boolean {
    return this.held(id, { startDate: date, endDate: addDates(date, 1) });
  }
  private held(id: string, interval: DateWindow): boolean {
    return this.holdsByProperty().get(id)?.some((hold: CalendarHold) => overlaps(hold, interval)) ?? false;
  }

  protected inspect(event: CalendarEvent): void {
    this.selectedEvent.set(event);
    this.previewOpen.set(true);
  }

  protected selectPropertyDates(interval: DateWindow): void {
    const property = this.data()?.properties.items[0];
    if (property) this.selectDates(property, interval.startDate, interval.endDate);
  }

  protected selectDates(property: CalendarProperty, startDate: string, endDate = addDates(startDate, 1)): void {
    this.day.set(startDate);
    this.draftProperty.set(property);
    this.draftStart.set(startDate);
    this.draftEnd.set(endDate);
    this.selectionAccepted.set(false);
    this.actionError.set('');
    this.selectionOpen.set(true);
    this.checkSelection();
  }

  protected checkSelection(): void {
    const record = this.data();
    const property = this.draftProperty();
    const start = this.draftStart();
    const end = this.draftEnd();
    const today = record?.businessDate ?? businessToday();
    let error = '';
    if (!validIsoDate(start) || !validIsoDate(end) || end <= start || start < today) error = this.c('rangeInvalid');
    else if (!record || !property || start < record.startDate || end > record.endDate) error = this.c('outOfRange');
    else if (this.held(property.id, { startDate: start, endDate: end })) error = this.c('rangeUnavailable');
    this.actionError.set(error);
    this.selectionAccepted.set(!error && property?.status !== 2);
  }

  protected reservationQuery() {
    return { ...this.query, calendarDate: this.referenceDate(), calendarProperty: this.propertyId,
      propertyId: this.draftProperty()?.id, checkInDate: this.draftStart(), checkOutDate: this.draftEnd() };
  }
  protected paymentLabel(payment: number): string { return this.t(payments[payment]); }

  protected openBlock(block: CalendarBlock | null): void {
    this.editingBlock.set(block);
    if (block) {
      this.draftStart.set(block.startDate);
      this.draftEnd.set(block.endDate);
    }
    this.draftReason.set(block?.reason ?? null);
    this.description.set(block?.description ?? '');
    this.actionError.set('');
    this.selectionOpen.set(false);
    this.previewOpen.set(false);
    this.blockOpen.set(true);
  }

  protected saveBlock(): void {
    const prior = this.editingBlock();
    const startDate = this.draftStart();
    const endDate = this.draftEnd();
    const reason = this.draftReason();
    const description = this.description().trim();
    if (!validIsoDate(startDate) || !validIsoDate(endDate) || endDate <= startDate || reason === null ||
        (reason === 4 && description.length < 3)) { this.actionError.set(this.c('invalid')); return; }
    const block = { startDate, endDate, reason, description, version: prior?.version };
    void this.write(() => prior ? this.api.put(this.organization, 'calendar-blocks/' + prior.id, block) :
      this.api.post(this.organization, 'calendar-blocks', { propertyId: this.draftProperty()?.id, block }), this.blockOpen, 'saved');
  }

  protected openCancel(): void {
    this.cancelReason.set('');
    this.actionError.set('');
    this.previewOpen.set(false);
    this.cancellationOpen.set(true);
  }

  protected cancelBlock(): void {
    const block = this.selectedEvent()?.block;
    if (!block || !this.cancelReason().trim()) { this.actionError.set(this.c('invalid')); return; }
    void this.write(() => this.api.post(this.organization, `calendar-blocks/${block.id}/cancel`, {
      version: block.version, reason: this.cancelReason().trim(),
    }), this.cancellationOpen, 'released');
  }

  private async write(request: () => ReturnType<typeof this.api.post>, dialog: typeof this.blockOpen,
    success: 'saved' | 'released'): Promise<void> {
    if (this.actionBusy()) return;
    this.actionBusy.set(true);
    this.actionError.set('');
    try {
      await firstValueFrom(request());
      dialog.set(false);
      this.saved.set(this.c(success));
      this.refresh();
    } catch (error) {
      const problem = error instanceof HttpErrorResponse ? error : null;
      this.actionError.set(this.c(problem?.error?.code === 'availability_stale' ? 'stale' :
        problem?.status === 503 ? 'temporary' : problem?.status === 409 ? 'conflict' : 'invalid'));
    } finally { this.actionBusy.set(false); }
  }
}
