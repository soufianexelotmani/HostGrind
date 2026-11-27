import { HttpClient } from '@angular/common/http';
import { Component, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { firstValueFrom, merge } from 'rxjs';
import { ConfirmationDialog, ErrorState, LoadingSpinner } from '../../shared/components/feedback';
import { Pagination } from '../../shared/components/data-table';
import { PageHeader } from '../../shared/components/page-elements';
import { Page, PropertySummary, PropertyView } from '../properties/portfolio-api';
import { PortfolioContext } from '../properties/portfolio-context';
import { validIsoDate } from '../calendar/calendar-models';
import { Guest, Night, payments, Reservation, Revision, sources, statuses } from './booking-models';
import { BookingPage } from './booking-page';

@Component({
  selector: 'hg-reservation-form', providers: [PortfolioContext],
  imports: [RouterLink, ReactiveFormsModule, PageHeader, ErrorState, LoadingSpinner, Pagination, ConfirmationDialog],
  styleUrl: '../properties/portfolio-page.scss', templateUrl: './reservation-form.html',
})
export class ReservationForm extends BookingPage {
  private readonly http = inject(HttpClient);
  private readonly builder = inject(FormBuilder);
  protected readonly id = this.route.snapshot.paramMap.get('id');
  protected readonly read = !!this.route.snapshot.data['read'];
  protected readonly historical = !!this.route.snapshot.data['historical'];
  protected readonly row = signal<Reservation | null>(null);
  protected readonly properties = signal<PropertySummary[]>([]);
  protected readonly propertySearch = this.builder.nonNullable.control('');
  protected readonly selectedProperty = signal<PropertyView | null>(null);
  protected readonly guests = signal<Guest[]>([]);
  protected readonly suggestions = signal<Guest[] | null>(null);
  protected readonly nights = signal<Night[]>([]);
  protected readonly overrides = signal<Partial<Record<string, number>>>({});
  protected readonly revisions = signal<Page<Revision> | null>(null);
  protected readonly availability = signal('');
  protected readonly file = signal<File | null>(null);
  protected readonly dialog = signal(false);
  protected readonly inlineGuest = signal(false);
  protected readonly sources = sources;
  protected readonly statuses = statuses;
  protected readonly payments = payments;
  protected readonly canEdit = computed(() => {
    const row = this.row();
    return row?.summary.status === 0 || row?.summary.status === 1 && !!row.pricing;
  });
  protected readonly form = this.builder.group({
    propertyId: this.builder.nonNullable.control('', Validators.required),
    guestId: this.builder.nonNullable.control('', Validators.required),
    source: this.builder.nonNullable.control(2),
    externalBookingId: this.builder.nonNullable.control(''),
    checkInDate: this.builder.nonNullable.control('', Validators.required),
    checkOutDate: this.builder.nonNullable.control('', Validators.required),
    adults: this.builder.control<number | null>(1, [Validators.required, Validators.min(1)]),
    children: this.builder.control<number | null>(0, [Validators.required, Validators.min(0)]),
    mode: this.builder.nonNullable.control(2),
    amount: this.builder.control<number | null>(null, Validators.min(0)),
    fees: this.builder.control<number | null>(0, Validators.min(0)),
    notes: this.builder.nonNullable.control(''),
    acceptPriceChanges: this.builder.nonNullable.control(false),
    confirmationDate: this.builder.nonNullable.control(''),
    actualCheckInDate: this.builder.nonNullable.control(''),
    actualCheckOutDate: this.builder.nonNullable.control(''),
    reason: this.builder.nonNullable.control(''),
  });
  protected readonly guestForm = this.builder.nonNullable.group({
    name: ['', Validators.required], email: ['', Validators.email], phone: [''], search: [''],
  });
  private confirmed = false;

  constructor() {
    super();
    const { propertyId, checkInDate, checkOutDate, source, mode } = this.form.controls;
    merge(propertyId.valueChanges, checkInDate.valueChanges, checkOutDate.valueChanges, source.valueChanges, mode.valueChanges)
      .pipe(takeUntilDestroyed()).subscribe(() => {
        this.availability.set('');
        this.nights.set([]);
      });
    propertyId.valueChanges.pipe(takeUntilDestroyed()).subscribe(() => this.selectedProperty.set(null));
    this.begin(() => this.load());
  }

  protected newGuest(): void {
    this.guestForm.markAllAsTouched();
    if (this.guestForm.invalid) return;
    void this.action(async () => {
      const { name, email, phone } = this.guestForm.getRawValue();
      const created = await firstValueFrom(this.api.post<{ id: string }>(this.organization, 'guests', { name, email, phone }));
      this.guests.update(rows => [{ id: created.id, name, email, phone, version: '', createdAt: null, updatedAt: null }, ...rows]);
      this.selectGuest(created.id);
      this.inlineGuest.set(false);
    });
  }

  protected selectGuest(id: string): void {
    const guest = this.suggestions()?.find(row => row.id === id);
    if (guest && !this.guests().some(row => row.id === id)) this.guests.update(rows => [guest, ...rows]);
    this.form.controls.guestId.setValue(id);
  }
  protected findGuests(): void {
    void this.action(async () => {
      const { email, phone } = this.guestForm.getRawValue();
      this.suggestions.set(await firstValueFrom(this.api.post<Guest[]>(this.organization, 'guests/suggestions', { email, phone })));
    });
  }
  protected searchGuests(): void { void this.action(() => this.loadGuests()); }
  protected searchProperties(): void { void this.action(() => this.loadProperties()); }

  protected propertyChanged(): void {
    this.overrides.set({});
    void this.action(() => this.loadProperty());
  }

  protected check(): void {
    if (!this.validDates()) return;
    void this.action(async () => {
      const { propertyId, checkInDate, checkOutDate } = this.form.getRawValue();
      const result = await firstValueFrom(this.api.get<{ status: string }>(this.organization,
        `properties/${propertyId}/availability`, { checkInDate, checkOutDate }));
      if (this.form.controls.propertyId.value === propertyId && this.form.controls.checkInDate.value === checkInDate &&
        this.form.controls.checkOutDate.value === checkOutDate) this.availability.set(result.status);
    });
  }

  protected preview(): void {
    if (!this.validDates()) return;
    void this.action(async () => {
      const { propertyId, checkInDate, checkOutDate, source } = this.form.getRawValue();
      if (source === 2 && !this.historical) {
        this.nights.set(await firstValueFrom(this.api.post<Night[]>(this.organization, 'reservations/pricing-preview',
          { propertyId, checkInDate, checkOutDate, reservationId: this.id })));
      } else {
        const start = Date.parse(checkInDate), end = Date.parse(checkOutDate);
        const accepted = new Map(this.row()?.pricing?.nights.map(night => [night.night, night]));
        this.nights.set(Array.from({ length: (end - start) / 86400000 }, (_, index) => {
          const night = new Date(start + index * 86400000).toISOString().slice(0, 10);
          return accepted.get(night) ?? { night, amount: 0, source: 'allocated', ruleId: null, manualOverride: false };
        }));
      }
    });
  }

  protected setOverride(night: string, event: Event): void {
    const input = event.target as HTMLInputElement;
    this.overrides.update(prices => {
      const next = { ...prices };
      if (input.value === '') delete next[night];
      else next[night] = input.valueAsNumber;
      return next;
    });
  }

  protected selectFile(event: Event): void { this.file.set((event.target as HTMLInputElement).files?.item(0) ?? null); }
  protected loadHistory(page: number): void { void this.action(() => this.readHistory(page)); }
  protected download(): void {
    void this.action(async () => {
      const blob = await firstValueFrom(this.http.get(this.api.path(this.organization, `reservations/${this.id}/evidence`),
        { responseType: 'blob' }));
      const url = URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      link.download = 'evidence.pdf';
      link.click();
      setTimeout(() => URL.revokeObjectURL(url), 1000);
    });
  }

  protected submit(confirmed: boolean): void {
    this.form.markAllAsTouched();
    const value = this.form.getRawValue();
    if (!this.validDates() || this.form.invalid) { if (!this.problem()) this.problem.set('invalid'); return; }
    const property = this.selectedProperty();
    if (property && value.adults! + value.children! > property.maxGuests) { this.problem.set('guest_capacity_invalid'); return; }
    if (this.historical && (!value.confirmationDate || !value.reason.trim() || !this.file())) {
      this.problem.set(!this.file() ? 'historical_evidence_required' : 'historical_dates_invalid');
      return;
    }
    this.confirmed = confirmed;
    if (confirmed || this.historical || !!this.id) this.dialog.set(true);
    else this.save();
  }

  protected save(): void {
    void this.action(async () => {
      const { confirmationDate, actualCheckInDate, actualCheckOutDate, reason, amount, fees, ...value } = this.form.getRawValue();
      const arbitrary = amount !== null;
      const nightlyPrices = Object.entries(this.overrides()).filter(([night]) => value.checkInDate <= night && night < value.checkOutDate)
        .map(([night, amount]) => ({ night, amount: amount! }));
      const input = { ...value, grossAmount: value.mode === 0 && arbitrary ? amount : null,
        platformFees: value.mode === 0 ? fees : null, netAmount: value.mode === 2 && arbitrary ? amount : null,
        confirm: this.confirmed, nightlyPrices, version: this.row()?.version };
      let id = this.id;
      if (id) await firstValueFrom(this.api.put(this.organization, 'reservations/' + id, input));
      else if (this.historical) {
        const data = new FormData();
        data.append('request', JSON.stringify({ booking: input, confirmationDate,
          actualCheckInDate: actualCheckInDate || null, actualCheckOutDate: actualCheckOutDate || null, reason }));
        data.append('file', this.file()!);
        id = (await firstValueFrom(this.api.post<{ id: string }>(this.organization, 'reservations/historical', data))).id;
      } else id = (await firstValueFrom(this.api.post<{ id: string }>(this.organization, 'reservations', input))).id;
      const calendarDate = this.route.snapshot.queryParamMap.get('calendarDate');
      const calendarProperty = this.route.snapshot.queryParamMap.get('calendarProperty');
      if (validIsoDate(calendarDate) && !this.historical) {
        await this.router.navigate(calendarProperty === value.propertyId ? ['/manager/calendar', value.propertyId] : ['/manager/calendar'],
          { queryParams: { ...this.query, date: calendarDate } });
      } else await this.router.navigate(['/manager/reservations', id], { queryParams: this.query });
    });
  }

  private validDates(): boolean {
    const { propertyId, checkInDate, checkOutDate } = this.form.getRawValue();
    const nights = (Date.parse(checkOutDate) - Date.parse(checkInDate)) / 86400000;
    if (propertyId && nights >= 1 && nights <= 366) return true;
    this.problem.set('invalid_date_range');
    return false;
  }

  private async load(): Promise<void> {
    if (this.id) {
      const row = await firstValueFrom(this.api.get<Reservation>(this.organization, 'reservations/' + this.id));
      this.row.set(row);
      if (this.read) { await this.readHistory(1); return; }
      const summary = row.summary;
      this.form.patchValue({ propertyId: summary.propertyId, guestId: summary.guestId, source: summary.source,
        externalBookingId: summary.externalBookingId ?? '', checkInDate: summary.checkInDate, checkOutDate: summary.checkOutDate,
        adults: row.adults, children: row.children, mode: row.mode, notes: row.notes ?? '', fees: row.platformFees ?? 0,
        amount: summary.source === 2 ? null : row.mode === 0 ? row.grossAmount : summary.netAmount });
      this.nights.set(row.pricing?.nights ?? []);
      if (summary.status === 1) {
        for (const key of ['propertyId', 'guestId', 'source', 'externalBookingId', 'mode'] as const) this.form.controls[key].disable();
      }
    } else if (!this.historical) {
      const params = this.route.snapshot.queryParamMap;
      const propertyId = params.get('propertyId');
      const checkInDate = params.get('checkInDate');
      const checkOutDate = params.get('checkOutDate');
      if (propertyId && /^[0-9a-f-]{36}$/i.test(propertyId) && validIsoDate(checkInDate) &&
          validIsoDate(checkOutDate) && checkInDate < checkOutDate)
        this.form.patchValue({ propertyId, checkInDate, checkOutDate });
    }
    await this.loadProperties();
    await this.loadGuests();
    await this.loadProperty();
  }

  private async loadGuests(): Promise<void> {
    const list = await firstValueFrom(this.api.get<Page<Guest>>(this.organization, 'guests', {
      search: this.guestForm.controls.search.value, size: 100,
    }));
    const selected = this.form.controls.guestId.value;
    if (selected && !list.items.some(row => row.id === selected))
      list.items.push(await firstValueFrom(this.api.get<Guest>(this.organization, 'guests/' + selected)));
    this.guests.set(list.items);
  }

  private async loadProperties(): Promise<void> {
    const properties = await firstValueFrom(this.api.get<Page<PropertySummary>>(this.organization, 'properties', {
      status: 'Active', size: 100, search: this.propertySearch.value,
    }));
    const selected = this.selectedProperty()?.summary;
    if (selected && !properties.items.some(row => row.id === selected.id)) properties.items.unshift(selected);
    this.properties.set(properties.items);
  }

  private async loadProperty(): Promise<void> {
    const id = this.form.controls.propertyId.value;
    if (!id) return;
    const property = await firstValueFrom(this.api.get<PropertyView>(this.organization, 'properties/' + id));
    if (this.form.controls.propertyId.value !== id) return;
    this.selectedProperty.set(property);
    if (!this.properties().some(row => row.id === id)) this.properties.update(rows => [property.summary, ...rows]);
  }

  private async readHistory(page: number): Promise<void> {
    this.revisions.set(await firstValueFrom(this.api.get<Page<Revision>>(this.organization, `reservations/${this.id}/history`, { page })));
  }
}
