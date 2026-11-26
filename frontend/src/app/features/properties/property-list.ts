import { Component, computed, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { DataTable, Pagination, TableColumn, TableSort } from '../../shared/components/data-table';
import { EmptyState, PageHeader } from '../../shared/components/page-elements';
import { ErrorState, LoadingSpinner, StatusBadge } from '../../shared/components/feedback';
import { CurrencyDisplay, SearchInput } from '../../shared/components/form-elements';
import { formatMoney } from '../../shared/utils/formatters';
import { Option, Owner, Page, PropertySummary } from './portfolio-api';
import { PortfolioContext } from './portfolio-context';
import { PortfolioPage } from './portfolio-page';
import { PrivatePhoto } from './private-photo';

export const PROPERTY_STATES = ['active', 'inactive', 'archived'];
export const OPERATING_STATES = ['ready', 'needsCleaning', 'cleaning', 'maintenance', 'unavailable'];

@Component({
  selector: 'hg-property-list',
  providers: [PortfolioContext],
  imports: [RouterLink, DataTable, Pagination, EmptyState, PageHeader, LoadingSpinner, StatusBadge,
    SearchInput, ErrorState, CurrencyDisplay, PrivatePhoto],
  templateUrl: './property-list.html',
  styleUrl: './portfolio-page.scss',
})
export class PropertyList extends PortfolioPage {
  protected readonly types = signal<Option[]>([]);
  protected readonly owners = signal<Owner[]>([]);
  protected readonly rows = signal<PropertySummary[]>([]);
  protected readonly view = signal<'grid' | 'table'>('grid');
  protected readonly page = signal(1);
  protected readonly total = signal(0);
  protected readonly search = signal('');
  protected readonly sort = signal<TableSort | null>(null);
  protected readonly selectedOwner = signal('');
  protected readonly selectedType = signal('');
  protected readonly selectedStatus = signal('');
  protected readonly states = PROPERTY_STATES;
  protected readonly track = (row: PropertySummary) => row.id;
  private generation = 0;
  protected readonly columns = computed<TableColumn<PropertySummary>[]>(() => [
    { key: 'reference', label: this.ctx.label('reference'), value: row => row.reference, sortable: true },
    { key: 'name', label: this.ctx.label('name'), value: row => row.name, sortable: true },
    { key: 'city', label: this.ctx.label('city'), value: row => row.city, sortable: true },
    { key: 'type', label: this.ctx.label('type'), value: row => this.ctx.label(row.type) },
    { key: 'status', label: this.ctx.label('status'), value: row => this.ctx.label(this.states[row.status]) },
    { key: 'baseNightlyRate', label: this.ctx.label('rate'), value: row => formatMoney(row.baseNightlyRate, this.ctx.locale), sortable: true },
  ]);

  constructor() { super(); void this.start(() => this.loadScope()); }
  protected changeScope(event: Event): void {
    this.page.set(1);
    this.selectedOwner.set('');
    this.setOrganization(event, () => this.loadScope());
  }
  protected filter(kind: 'owner' | 'type' | 'status', event: Event): void {
    const value = (event.target as HTMLSelectElement).value;
    const controls = { owner: this.selectedOwner, type: this.selectedType, status: this.selectedStatus };
    controls[kind].set(value);
    this.changePage(1);
  }
  protected changeSearch(value: string): void { this.search.set(value); this.changePage(1); }
  protected changeSort(value: TableSort): void { this.sort.set(value); this.changePage(1); }
  protected changePage(page: number): void { this.page.set(page); void this.loadRows(); }
  protected open(row: PropertySummary): void { void this.router.navigate([this.ctx.link('properties', row.id)], { queryParams: this.query }); }

  private async loadScope(): Promise<void> {
    this.types.set(await firstValueFrom(this.api.get<Option[]>(this.organization, 'properties/types')));
    if (this.ctx.manage) this.owners.set((await firstValueFrom(this.api.get<Page<Owner>>(this.organization, 'owners', { size: 100 }))).items);
    await this.loadRows();
  }
  private async loadRows(): Promise<void> {
    const id = ++this.generation;
    const sort = this.sort();
    this.busy.set(true);
    this.problem.set('');
    try {
      const result = await firstValueFrom(this.api.get<Page<PropertySummary>>(this.organization, 'properties', {
        page: this.page(), size: 20, search: this.search(), sort: sort ? (sort.direction === 'desc' ? '-' : '') + sort.key : undefined,
        owner: this.selectedOwner(), type: this.selectedType(), status: this.selectedStatus(),
      }));
      if (id === this.generation) { this.rows.set(result.items); this.total.set(result.total); }
    } catch { if (id === this.generation) this.problem.set('error'); }
    finally { if (id === this.generation) this.busy.set(false); }
  }
}
