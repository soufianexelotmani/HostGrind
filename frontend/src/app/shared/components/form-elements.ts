import { Component, computed, inject, input, model, output, signal } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { Preferences } from '../../core/config/preferences';
import { formatMoney } from '../utils/formatters';

@Component({
  selector: 'hg-search-input',
  imports: [TranslocoPipe],
  template: `<input class="hg-input" type="search" [attr.aria-label]="label() || ('ui.search' | transloco)"
    [placeholder]="label() || ('ui.search' | transloco)" [value]="value()" (input)="changed($event)">`,
})
export class SearchInput {
  readonly label = input('');
  readonly value = model('');
  protected changed(event: Event): void { this.value.set((event.target as HTMLInputElement).value); }
}

@Component({
  selector: 'hg-filter-toolbar',
  imports: [TranslocoPipe],
  template: `<div class="table-toolbar" role="group" [attr.aria-label]="'ui.filters' | transloco"><ng-content /></div>`,
})
export class FilterToolbar {}

@Component({
  selector: 'hg-form-field',
  template: `<div class="hg-field">
    <label [attr.for]="controlId()">{{ label() }}</label><ng-content />
    @if (message()) { <p class="field-error" [id]="controlId() + '-error'">{{ message() }}</p> }
  </div>`,
})
export class FormField {
  readonly controlId = input.required<string>();
  readonly label = input.required<string>();
  readonly message = input('');
}

export interface DateRange { readonly start: string; readonly end: string }

@Component({
  selector: 'hg-date-range',
  imports: [TranslocoPipe],
  template: `<div class="table-toolbar date-range">
    <label>{{ 'ui.from' | transloco }}<input class="hg-input" type="date" [value]="value().start"
      [attr.aria-invalid]="invalid()" (input)="update('start', $event)"></label>
    <label>{{ 'ui.to' | transloco }}<input class="hg-input" type="date" [value]="value().end" [attr.min]="value().start || null"
      [attr.aria-invalid]="invalid()" (input)="update('end', $event)"></label>
  </div>
  @if (invalid()) { <p class="field-error" role="status">{{ 'ui.invalidRange' | transloco }}</p> }`,
})
export class DateRangeInput {
  readonly value = model<DateRange>({ start: '', end: '' });
  readonly accepted = output<DateRange>();
  protected readonly invalid = computed(() => !!this.value().start && !!this.value().end && this.value().start > this.value().end);
  protected update(key: keyof DateRange, event: Event): void {
    this.value.update(value => ({ ...value, [key]: (event.target as HTMLInputElement).value }));
    if (!this.invalid()) this.accepted.emit(this.value());
  }
}

@Component({
  selector: 'hg-currency',
  imports: [TranslocoPipe],
  template: `<bdi class="currency">{{ display() ?? ('ui.unknown' | transloco) }}</bdi>`,
})
export class CurrencyDisplay {
  readonly amount = input.required<number | null>();
  private readonly prefs = inject(Preferences);
  protected readonly display = computed(() => formatMoney(this.amount(), this.prefs.locale()));
}

@Component({
  selector: 'hg-file-picker',
  imports: [TranslocoPipe],
  template: `<label class="hg-field">{{ 'ui.upload' | transloco }}
    <input type="file" [attr.accept]="accept() || null" [multiple]="multiple()" [disabled]="disabled()" (change)="select($event)">
    </label>@if (rejected()) { <p class="field-error" role="status">{{ 'ui.invalidFiles' | transloco }}</p> }`,
})
export class FilePicker {
  readonly accept = input('');
  readonly multiple = input(false);
  readonly maxBytes = input<number | null>(10 * 1024 * 1024);
  readonly disabled = input(false);
  readonly selected = output<readonly File[]>();
  protected readonly rejected = signal(false);

  protected select(event: Event): void {
    const inputElement = event.target as HTMLInputElement;
    const rules = this.accept().toLowerCase().split(',').map(rule => rule.trim()).filter(Boolean);
    const files = Array.from(inputElement.files ?? []);
    const limit = this.maxBytes();
    const accepted = files.filter(file => (limit === null || file.size <= limit) &&
      (!rules.length || rules.some(rule => rule.startsWith('.') ? file.name.toLowerCase().endsWith(rule)
        : rule.endsWith('/*') ? file.type.toLowerCase().startsWith(rule.slice(0, -1))
        : file.type.toLowerCase() === rule)));
    this.rejected.set(accepted.length !== files.length);
    this.selected.emit(accepted);
    inputElement.value = '';
  }
}
