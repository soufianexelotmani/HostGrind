import { Component, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { DataTable, TableColumn, TableSort } from '../../shared/components/data-table';
import { ConfirmationDialog, ErrorState, LoadingSpinner, StatusBadge } from '../../shared/components/feedback';
import { EmptyState, PageHeader } from '../../shared/components/page-elements';
import { Owner, OwnerPortfolio, Page } from '../properties/portfolio-api';
import { PortfolioContext } from '../properties/portfolio-context';
import { PortfolioPage } from '../properties/portfolio-page';

const pageStyle = '../properties/portfolio-page.scss';
function accountState(owner: Owner): string {
  return !owner.userId ? 'unlinked' : !owner.accessEnabled ? 'disabled' : owner.activated ? 'enabled' : 'pending';
}

@Component({
  selector: 'hg-owner-list', providers: [PortfolioContext],
  imports: [RouterLink, DataTable, PageHeader, LoadingSpinner, ErrorState, EmptyState],
  styleUrl: pageStyle,
  template: `
    <div class="portfolio-page">
      @if (ready()) {
        <hg-page-header [title]="ctx.label('owners')">
          @if (organization) { <a class="portfolio-button" [routerLink]="ctx.link('owners','new')" [queryParams]="query">{{ ctx.label('newOwner') }}</a> }
        </hg-page-header>
        @if (organization) {
          <div class="portfolio-toolbar">
            @if (ctx.choices().length > 1 || ctx.role === 'SuperAdmin') {
              <label>{{ ctx.label('organization') }} <select [value]="organization" (change)="changeScope($event)">
                @for (choice of ctx.choices(); track choice.id) { <option [value]="choice.id">{{ choice.name }}</option> }
              </select></label>
            }
            <label>{{ ctx.label('status') }} <select [value]="archived()" (change)="changeFilter($event)">
              <option value="">{{ ctx.label('all') }}</option><option value="false">{{ ctx.label('active') }}</option><option value="true">{{ ctx.label('archived') }}</option>
            </select></label>
          </div>
          <hg-data-table [columns]="columns()" [rows]="rows()" [trackRow]="track" [label]="ctx.label('owners')" [page]="page()"
            [total]="total()" [search]="search()" [sort]="sort()" [state]="busy() ? 'loading' : problem() ? 'error' : 'ready'"
            [actionLabel]="ctx.label('open')" (searchChanged)="changeSearch($event)" (sortChanged)="changeSort($event)"
            (pageChanged)="changePage($event)" (retry)="changePage(page())" (rowSelected)="open($event)"/>
        } @else { <hg-empty-state [title]="ctx.label('noOrganization')"/> }
      } @else if (problem()) { <hg-error-state [retryable]="false"/> }
      @else { <hg-loading/> }
    </div>`,
})
export class OwnerList extends PortfolioPage {
  protected readonly rows = signal<Owner[]>([]);
  protected readonly page = signal(1);
  protected readonly total = signal(0);
  protected readonly search = signal('');
  protected readonly sort = signal<TableSort | null>(null);
  protected readonly archived = signal('');
  protected readonly track = (row: Owner) => row.id;
  private generation = 0;
  protected readonly columns = computed<TableColumn<Owner>[]>(() => [
    { key: 'reference', label: this.ctx.label('reference'), value: row => row.reference, sortable: true },
    { key: 'name', label: this.ctx.label('name'), value: row => row.name, sortable: true },
    { key: 'email', label: this.ctx.label('email'), value: row => row.email, sortable: true },
    { key: 'phone', label: this.ctx.label('phone'), value: row => row.phone },
    { key: 'account', label: this.ctx.label('account'), value: row => this.ctx.label(row.isArchived ? 'archived' : accountState(row)) },
  ]);
  constructor() { super(); void this.start(() => this.load()); }

  protected changeScope(event: Event): void { this.page.set(1); this.setOrganization(event, () => this.load()); }
  protected changeSearch(value: string): void { this.search.set(value); this.changePage(1); }
  protected changeSort(value: TableSort): void { this.sort.set(value); this.changePage(1); }
  protected changeFilter(event: Event): void { this.archived.set((event.target as HTMLSelectElement).value); this.changePage(1); }
  protected changePage(value: number): void { this.page.set(value); void this.load(); }
  protected open(row: Owner): void { void this.router.navigate([this.ctx.link('owners', row.id)], { queryParams: this.query }); }

  private async load(): Promise<void> {
    const generation = ++this.generation;
    const sort = this.sort();
    this.problem.set('');
    this.busy.set(true);
    try {
      const result = await firstValueFrom(this.api.get<Page<Owner>>(this.organization, 'owners', { page: this.page(), size: 20,
        search: this.search(), archived: this.archived(), sort: sort ? (sort.direction === 'desc' ? '-' : '') + sort.key : undefined }));
      if (generation === this.generation) { this.rows.set(result.items); this.total.set(result.total); }
    } catch { if (generation === this.generation) this.problem.set('error'); }
    finally { if (generation === this.generation) this.busy.set(false); }
  }
}

@Component({
  selector: 'hg-owner-form', providers: [PortfolioContext],
  imports: [RouterLink, ReactiveFormsModule, PageHeader, LoadingSpinner, ErrorState],
  styleUrl: pageStyle,
  template: `
    <div class="portfolio-page">
      @if (ready() && organization) {
        <hg-page-header [title]="ctx.label(id ? 'editOwner' : 'newOwner')"/>
        <form [formGroup]="form" (ngSubmit)="save()" class="portfolio-form dashboard-panel">
          <label>{{ ctx.label('name') }} <input formControlName="name" required maxlength="200"></label>
          <label>{{ ctx.label('email') }} <input type="email" formControlName="email" maxlength="254"></label>
          <label>{{ ctx.label('phone') }} <input type="tel" formControlName="phone" maxlength="32"></label>
          @if (problem() || form.touched && form.invalid) {
            <p class="error wide" role="alert">{{ ctx.label(problem() || 'invalid') }}</p>
          }
          <div class="actions">
            <button class="portfolio-button" type="submit" [disabled]="busy()">{{ ctx.label(busy() ? 'loading' : 'save') }}</button>
            <a class="hg-secondary" [routerLink]="ctx.link('owners', id || undefined)" [queryParams]="query">{{ ctx.label('cancel') }}</a>
          </div>
        </form>
      } @else if (problem() || ready()) { <hg-error-state [retryable]="false"/> } @else { <hg-loading/> }
    </div>`,
})
export class OwnerForm extends PortfolioPage {
  protected readonly id = this.route.snapshot.paramMap.get('id') ?? '';
  private version: string | null = null;
  protected readonly form = inject(FormBuilder).nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(200)]],
    email: ['', Validators.email],
    phone: [''],
  });

  constructor() {
    super(); void this.start(async () => {
      if (!this.id) return;
      const { owner } = await firstValueFrom(this.api.get<OwnerPortfolio>(this.organization, 'owners/' + this.id));
      this.form.setValue({ name: owner.name, email: owner.email ?? '', phone: owner.phone ?? '' });
      this.version = owner.version;
    });
  }
  protected save(): void {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    void this.run(async () => {
      const body = { ...this.form.getRawValue(), version: this.version };
      let id = this.id;
      if (id) await firstValueFrom(this.api.put(this.organization, 'owners/' + id, body));
      else id = (await firstValueFrom(this.api.post<{ id: string }>(this.organization, 'owners', body))).id;
      await this.router.navigate([this.ctx.link('owners', id)], { queryParams: this.query });
    });
  }
}

@Component({
  selector: 'hg-owner-details', providers: [PortfolioContext],
  imports: [RouterLink, PageHeader, LoadingSpinner, ErrorState, StatusBadge, ConfirmationDialog, EmptyState],
  styleUrl: pageStyle,
  template: `
    <div class="portfolio-page">
      @if (data(); as portfolio) {
        <hg-page-header [title]="portfolio.owner.name" [description]="portfolio.owner.reference">
          <a class="hg-secondary" [routerLink]="ctx.link('owners')" [queryParams]="query">{{ ctx.label('back') }}</a>
          <a class="portfolio-button" [routerLink]="ctx.link('owners', id + '/edit')" [queryParams]="query">{{ ctx.label('editOwner') }}</a>
        </hg-page-header>
        @if (problem()) { <p class="error" role="alert">{{ ctx.label(problem()) }}</p> }
        @if (message()) { <p class="success" role="status">{{ ctx.label(message()) }}</p> }
        <section class="dashboard-panel portfolio-page">
          <dl><div><dt>{{ ctx.label('email') }}</dt><dd><bdi>{{ portfolio.owner.email || '—' }}</bdi></dd></div>
            <div><dt>{{ ctx.label('phone') }}</dt><dd><bdi>{{ portfolio.owner.phone || '—' }}</bdi></dd></div>
            <div><dt>{{ ctx.label('account') }}</dt><dd><hg-status [label]="ctx.label(portfolio.owner.isArchived ? 'archived' : state(portfolio.owner))"/></dd></div></dl>
          @if (!portfolio.owner.isArchived) {
            <div class="actions">
              @if (portfolio.owner.email && (!portfolio.owner.userId || !portfolio.owner.activated && portfolio.owner.accessEnabled)) {
                <button type="button" class="portfolio-button" [disabled]="busy()" (click)="action('invitation')">
                  {{ ctx.label(portfolio.owner.userId ? 'resend' : 'invite') }}</button>
              }
              @if (portfolio.owner.userId && portfolio.owner.accessEnabled) {
                <button type="button" class="hg-secondary" (click)="request('disable-access')">{{ ctx.label('disable') }}</button>
              }
              <button type="button" class="hg-secondary" (click)="request('archive')">{{ ctx.label('archive') }}</button>
            </div>
            @if (!portfolio.owner.userId) {
              <form class="portfolio-toolbar" (submit)="link($event, user.value)">
                <label>{{ ctx.label('accountId') }} <input #user required></label>
                <button type="submit" class="hg-secondary" [disabled]="busy()">{{ ctx.label('linkAccount') }}</button>
              </form>
            }
          }
        </section>
        <section class="dashboard-panel portfolio-page">
          <h2>{{ ctx.label('properties') }}</h2>
          <ul class="record-list">
            @for (property of portfolio.properties; track property.id) {
              <li><a [routerLink]="ctx.link('properties', property.id)" [queryParams]="query">{{ property.name }}</a> · {{ property.city }}</li>
            } @empty { <li>{{ ctx.label('empty') }}</li> }
          </ul>
        </section>
        <section class="dashboard-panel portfolio-page"><h2>{{ ctx.label('contracts') }}</h2>
          <ul class="record-list">@for (contract of portfolio.contracts; track contract.id) {
            <li><a [routerLink]="ctx.link('properties',contract.propertyId)" [queryParams]="query"><bdi>{{ contract.startDate }}</bdi></a>
              · {{ ctx.label(contract.status) }} · {{ contract.rate }}%</li>
          } @empty { <li>{{ ctx.label('empty') }}</li> }</ul>
        </section>
        <hg-confirm [title]="ctx.label('confirm')" [message]="ctx.label(pending() === 'archive' ? 'archive' : 'disable')"
          [(open)]="confirmOpen" (confirmed)="action(pending())"/>
      } @else if (problem() || ready() && !organization) { <hg-error-state [retryable]="false"/> }
      @else { <hg-loading/> }
    </div>`,
})
export class OwnerDetails extends PortfolioPage {
  protected readonly data = signal<OwnerPortfolio | null>(null);
  protected readonly id = this.route.snapshot.paramMap.get('id') ?? '';
  protected readonly confirmOpen = signal(false);
  protected readonly pending = signal('archive');
  protected readonly state = accountState;
  constructor() { super(); void this.start(() => this.load()); }
  protected request(action: string): void { this.pending.set(action); this.confirmOpen.set(true); }
  protected action(action: string): void { this.write(action, { version: this.data()?.owner.version }); }
  protected link(event: Event, userId: string): void {
    event.preventDefault(); this.write('link', { userId: userId.trim(), version: this.data()?.owner.version });
  }
  private write(action: string, body: object): void {
    void this.run(async () => {
      await firstValueFrom(this.api.post(this.organization, `owners/${this.id}/${action}`, body));
      await this.load();
    }, true);
  }
  private async load(): Promise<void> {
    this.data.set(await firstValueFrom(this.api.get<OwnerPortfolio>(this.organization, 'owners/' + this.id)));
  }
}
