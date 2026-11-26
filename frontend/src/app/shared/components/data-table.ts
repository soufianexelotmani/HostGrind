import { Component, computed, input, output, signal } from '@angular/core';
import { CdkScrollable } from '@angular/cdk/scrolling';
import { TranslocoPipe } from '@jsverse/transloco';
import { EmptyState } from './page-elements';
import { ErrorState, LoadingSpinner } from './feedback';
import { FilterToolbar, SearchInput } from './form-elements';

export interface TableColumn<T> {
  readonly key: string;
  readonly label: string;
  readonly value: (row: T) => string | number | null;
  readonly sortable?: boolean;
}

export interface TableSort { readonly key: string; readonly direction: 'asc' | 'desc' }
export type TableState = 'ready' | 'loading' | 'error';

@Component({
  selector: 'hg-pagination',
  imports: [TranslocoPipe],
  template: `<div class="pagination">
    <button type="button" class="hg-secondary" [disabled]="page() <= 1 || disabled()" (click)="changed.emit(page() - 1)">
      {{ 'ui.previous' | transloco }}</button>
    <span>{{ 'ui.page' | transloco: {page: page(), total: pages()} }}</span>
    <button type="button" class="hg-secondary" [disabled]="page() >= pages() || disabled()" (click)="changed.emit(page() + 1)">
      {{ 'ui.next' | transloco }}</button>
  </div>`,
})
export class Pagination {
  readonly page = input(1);
  readonly pageSize = input(20);
  readonly total = input(0);
  readonly disabled = input(false);
  readonly changed = output<number>();
  protected readonly pages = computed(() => Math.max(1, Math.ceil(this.total() / Math.max(1, this.pageSize()))));
}

@Component({
  selector: 'hg-data-table',
  imports: [CdkScrollable, TranslocoPipe, FilterToolbar, SearchInput, EmptyState, ErrorState, LoadingSpinner, Pagination],
  template: `
    <hg-filter-toolbar>
      <hg-search-input [value]="search()" (valueChange)="searchChanged.emit($event)"/>
      <ng-content select="[filters]" />
      <details class="column-picker">
        <summary>{{ 'ui.columns' | transloco }}</summary>
        @for (column of columns(); track column.key) {
          <label><input type="checkbox" [checked]="!hidden().has(column.key)"
            [disabled]="visible().length === 1 && !hidden().has(column.key)" (change)="toggle(column.key)"> {{ column.label }}</label>
        }
      </details>
    </hg-filter-toolbar>
    @switch (state()) {
      @case ('loading') { <hg-loading/> }
      @case ('error') { <hg-error-state (retry)="retry.emit()"/> }
      @default {
        @if (rows().length) {
          <div cdkScrollable class="table-scroll" tabindex="0" role="region" [attr.aria-label]="label() || ('ui.table' | transloco)">
            <table class="hg-table" [attr.aria-label]="label() || ('ui.table' | transloco)">
              <thead><tr>@for (column of visible(); track column.key) {
                <th scope="col" [attr.aria-sort]="ariaSort(column.key)">
                  @if (column.sortable) {
                    <button type="button" (click)="sortColumn(column.key)">{{ column.label }}
                      @if (sort()?.key === column.key) { <span aria-hidden="true">{{ sort()?.direction === 'asc' ? '↑' : '↓' }}</span> }
                    </button>
                  } @else { {{ column.label }} }
                </th>
              }@if (actionLabel()) { <th scope="col">{{ actionLabel() }}</th> }</tr></thead>
              <tbody>@for (row of rows(); track trackRow()(row)) {
                <tr>@for (column of visible(); track column.key) { <td>{{ column.value(row) ?? ('ui.unknown' | transloco) }}</td> }
                  @if (actionLabel()) { <td><button type="button" class="hg-secondary" (click)="rowSelected.emit(row)">{{ actionLabel() }}</button></td> }
                </tr>
              }</tbody>
            </table>
          </div>
        } @else { <hg-empty-state [title]="'ui.empty' | transloco"/> }
      }
    }
    @if (total() > pageSize()) {
      <hg-pagination [page]="page()" [pageSize]="pageSize()" [total]="total()"
        [disabled]="state() === 'loading'" (changed)="pageChanged.emit($event)"/>
    }`,
})
export class DataTable<T> {
  readonly columns = input.required<readonly TableColumn<T>[]>();
  readonly rows = input.required<readonly T[]>();
  readonly trackRow = input.required<(row: T) => string | number>();
  readonly state = input<TableState>('ready');
  readonly label = input('');
  readonly search = input('');
  readonly sort = input<TableSort | null>(null);
  readonly page = input(1);
  readonly pageSize = input(20);
  readonly total = input(0);
  readonly actionLabel = input('');
  readonly rowSelected = output<T>();
  readonly sortChanged = output<TableSort>();
  readonly searchChanged = output<string>();
  readonly pageChanged = output<number>();
  readonly retry = output<void>();
  protected readonly hidden = signal<ReadonlySet<string>>(new Set());
  protected readonly visible = computed(() => this.columns().filter(column => !this.hidden().has(column.key)));
  protected toggle(key: string): void {
    if (this.visible().length === 1 && !this.hidden().has(key)) return;
    this.hidden.update(previous => {
      const next = new Set(previous);
      if (next.has(key)) next.delete(key); else next.add(key);
      return next;
    });
  }
  protected sortColumn(key: string): void {
    this.sortChanged.emit({ key, direction: this.sort()?.key === key && this.sort()?.direction === 'asc' ? 'desc' : 'asc' });
  }
  protected ariaSort(key: string): 'ascending' | 'descending' | null {
    const sort = this.sort();
    return sort?.key === key ? sort.direction === 'asc' ? 'ascending' : 'descending' : null;
  }
}
