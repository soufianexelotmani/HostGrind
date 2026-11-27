import { Component, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { DataTable, TableColumn, TableSort, Pagination } from '../../shared/components/data-table';
import { ErrorState, LoadingSpinner } from '../../shared/components/feedback';
import { EmptyState, PageHeader } from '../../shared/components/page-elements';
import { Page } from '../properties/portfolio-api';
import { PortfolioContext } from '../properties/portfolio-context';
import { Guest, Stay, statuses } from './booking-models';
import { BookingPage } from './booking-page';

@Component({
  selector: 'hg-guest-page', providers: [PortfolioContext],
  imports: [RouterLink, ReactiveFormsModule, DataTable, PageHeader, LoadingSpinner, ErrorState, EmptyState, Pagination],
  styleUrl: '../properties/portfolio-page.scss',
  templateUrl: './guest-page.html',
})
export class GuestPage extends BookingPage {
  protected readonly id = this.route.snapshot.paramMap.get('id');
  protected readonly editing = !!this.route.snapshot.data['edit'];
  protected readonly guest = signal<Guest | null>(null);
  protected readonly rows = signal<Guest[]>([]);
  protected readonly reservations = signal<Page<Stay> | null>(null);
  protected readonly statuses = statuses;
  protected readonly sort = signal<TableSort | null>(null);
  protected readonly term = signal('');
  protected readonly filter = signal('');
  protected readonly page = signal(1);
  protected readonly total = signal(0);
  protected readonly track = (row: Guest) => row.id;
  protected readonly columns = computed<TableColumn<Guest>[]>(() => [
    { key: 'name', label: this.t('name'), value: row => row.name, sortable: true },
    { key: 'created', label: this.t('entryDate'), value: row => this.date(row.createdAt), sortable: true },
    { key: 'email', label: this.t('email'), value: row => row.email },
    { key: 'phone', label: this.t('phone'), value: row => row.phone },
  ]);
  protected readonly form = inject(FormBuilder).nonNullable.group({
    name: ['', Validators.required], email: ['', Validators.email], phone: [''],
  });
  private sequence = 0;

  constructor() { super(); this.begin(() => this.load()); }

  protected changeScope(event: Event): void {
    this.page.set(1);
    this.setOrganization(event, () => this.load());
  }

  protected changeSearch(term: string): void { this.term.set(term); this.changePage(1); }
  protected changeSort(sort: TableSort): void { this.sort.set(sort); this.changePage(1); }
  protected changeFilter(event: Event): void {
    this.filter.set((event.target as HTMLSelectElement).value);
    this.changePage(1);
  }
  protected changePage(page: number): void { this.page.set(page); void this.load(); }
  protected open(row: Guest): void { void this.router.navigate(['/manager/guests', row.id], { queryParams: this.query }); }

  protected async history(page: number): Promise<void> {
    this.reservations.set(await firstValueFrom(this.api.get<Page<Stay>>(this.organization, `guests/${this.id}/reservations`, { page })));
  }

  protected save(): void {
    this.form.markAllAsTouched();
    if (this.form.invalid) return;
    void this.action(async () => {
      const input = { ...this.form.getRawValue(), version: this.guest()?.version };
      const result = this.id ? (await firstValueFrom(this.api.put(this.organization, 'guests/' + this.id, input)), { id: this.id })
        : await firstValueFrom(this.api.post<{ id: string }>(this.organization, 'guests', input));
      await this.router.navigate(['/manager/guests', result.id], { queryParams: this.query });
    });
  }

  private async load(): Promise<void> {
    if (!this.id && this.editing) return;
    if (this.id) {
      const guest = await firstValueFrom(this.api.get<Guest>(this.organization, 'guests/' + this.id));
      this.guest.set(guest);
      this.form.setValue({ name: guest.name, phone: guest.phone ?? '', email: guest.email ?? '' });
      if (!this.editing) await this.history(1);
      return;
    }
    const request = ++this.sequence;
    const sort = this.sort();
    this.busy.set(true);
    this.problem.set('');
    try {
      const result = await firstValueFrom(this.api.get<Page<Guest>>(this.organization, 'guests', {
        search: this.term(), hasContact: this.filter(), page: this.page(),
        sort: sort ? (sort.direction === 'desc' ? '-' : '') + sort.key : undefined,
      }));
      if (request === this.sequence) { this.rows.set(result.items); this.total.set(result.total); }
    } catch (error) { if (request === this.sequence) this.capture(error); }
    finally { if (request === this.sequence) this.busy.set(false); }
  }
}
