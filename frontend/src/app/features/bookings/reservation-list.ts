import { Component, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { DataTable, TableColumn, TableSort } from '../../shared/components/data-table';
import { ErrorState, LoadingSpinner } from '../../shared/components/feedback';
import { EmptyState, PageHeader } from '../../shared/components/page-elements';
import { Owner, Page, PropertySummary } from '../properties/portfolio-api';
import { PortfolioContext } from '../properties/portfolio-context';
import { payments, sources, statuses, Stay } from './booking-models';
import { BookingPage } from './booking-page';

@Component({
  selector: 'hg-reservation-list', providers: [PortfolioContext],
  imports: [RouterLink, ReactiveFormsModule, DataTable, PageHeader, ErrorState, LoadingSpinner, EmptyState],
  styleUrl: '../properties/portfolio-page.scss', templateUrl: './reservation-list.html',
})
export class ReservationList extends BookingPage {
  protected readonly rows = signal<Stay[]>([]);
  protected readonly properties = signal<PropertySummary[]>([]);
  protected readonly owners = signal<Owner[]>([]);
  protected readonly total = signal(0);
  protected readonly page = signal(1);
  protected readonly search = signal('');
  protected readonly sort = signal<TableSort | null>(null);
  protected readonly sources = sources;
  protected readonly statuses = statuses;
  protected readonly payments = payments;
  protected readonly track = (row: Stay) => row.id;
  protected readonly controls = inject(FormBuilder).nonNullable.group({
    property: '', owner: '', source: '', status: '', paymentStatus: '', externalReference: '', guest: '',
    arrivalFrom: '', arrivalTo: '', departureFrom: '', departureTo: '',
  });
  protected readonly ranges = ['arrivalFrom', 'arrivalTo', 'departureFrom', 'departureTo'] as const;
  protected readonly columns = computed<TableColumn<Stay>[]>(() => [
    { key: 'reference', label: this.t('reference'), value: row => row.reference, sortable: true },
    { key: 'property', label: this.t('property'), value: row => row.propertyName },
    { key: 'guest', label: this.t('guest'), value: row => row.guestName },
    { key: 'arrival', label: this.t('arrival'), value: row => this.date(row.checkInDate), sortable: true },
    { key: 'departure', label: this.t('departure'), value: row => this.date(row.checkOutDate), sortable: true },
    { key: 'source', label: this.t('source'), value: row => this.t(sources[row.source]) },
    { key: 'status', label: this.t('status'), value: row => this.t(statuses[row.status]) },
    { key: 'payment', label: this.t('payment'), value: row => this.t(payments[row.paymentStatus]) },
    { key: 'net', label: this.t('net'), value: row => this.money(row.netAmount) },
  ]);

  private generation = 0;
  constructor() { super(); this.begin(() => this.loadScope()); }
  protected changeScope(event: Event): void {
    this.controls.reset();
    this.page.set(1);
    this.setOrganization(event, () => this.loadScope());
  }
  protected changePage(page: number): void { this.page.set(page); void this.load(); }
  protected changeSearch(value: string): void { this.search.set(value); this.changePage(1); }
  protected changeSort(value: TableSort): void { this.sort.set(value); this.changePage(1); }
  protected open(row: Stay): void { void this.router.navigate(['/manager/reservations', row.id], { queryParams: this.query }); }

  private async loadScope(): Promise<void> {
    const [properties, owners] = await Promise.all([
      firstValueFrom(this.api.get<Page<PropertySummary>>(this.organization, 'properties', { size: 100 })),
      firstValueFrom(this.api.get<Page<Owner>>(this.organization, 'owners', { size: 100 })),
    ]);
    this.properties.set(properties.items);
    this.owners.set(owners.items);
    await this.load();
  }

  private async load(): Promise<void> {
    const generation = ++this.generation;
    const sort = this.sort();
    this.busy.set(true);
    this.problem.set('');
    try {
      const data = await firstValueFrom(this.api.get<Page<Stay>>(this.organization, 'reservations', {
        ...this.controls.getRawValue(), reference: this.search(), page: this.page(),
        sort: sort ? (sort.direction === 'desc' ? '-' : '') + sort.key : undefined,
      }));
      if (generation === this.generation) { this.rows.set(data.items); this.total.set(data.total); }
    } catch (error) { if (generation === this.generation) this.capture(error); }
    finally { if (generation === this.generation) this.busy.set(false); }
  }
}
