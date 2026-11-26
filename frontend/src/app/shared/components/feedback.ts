import { Component, input, model, output } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { ProgressSpinner } from 'primeng/progressspinner';
import { Skeleton } from 'primeng/skeleton';
import { ButtonModule } from 'primeng/button';
import { Modal } from './modal';

export type StatusTone = 'success' | 'warning' | 'error' | 'info' | 'muted';

@Component({
  selector: 'hg-status',
  template: `<span class="hg-status" [attr.data-tone]="tone()">{{ label() }}</span>`,
})
export class StatusBadge {
  readonly label = input.required<string>();
  readonly tone = input<StatusTone>('muted');
}

@Component({
  selector: 'hg-kpi',
  imports: [TranslocoPipe],
  template: `<section class="dashboard-panel"><p>{{ label() }}</p>
    <strong class="currency hg-kpi">{{ value() ?? ('ui.unknown' | transloco) }}</strong>
    @if (detail()) { <p>{{ detail() }}</p> }</section>`,
})
export class KpiCard {
  readonly label = input.required<string>();
  readonly value = input<string | number | null>(null);
  readonly detail = input('');
}

@Component({
  selector: 'hg-loading',
  imports: [ProgressSpinner, TranslocoPipe],
  template: `<div class="hg-loading" role="status"><p-progressSpinner [ariaLabel]="'ui.loading' | transloco"
     [style]="{ width: '2rem', height: '2rem' }" strokeWidth="4"/><span>{{ 'ui.loading' | transloco }}</span></div>`,
})
export class LoadingSpinner {}

@Component({
  selector: 'hg-skeleton',
  imports: [Skeleton, TranslocoPipe],
  template: `<div class="hg-skeleton" role="status" [attr.aria-label]="'ui.loading' | transloco">
    @for (row of rows(); track $index) { <p-skeleton height="1rem" [width]="$last ? '68%' : '100%'" aria-hidden="true" /> }
  </div>`,
})
export class SkeletonLoader {
  readonly rows = input<readonly number[]>([1, 2, 3]);
}

@Component({
  selector: 'hg-error-state',
  imports: [ButtonModule, TranslocoPipe],
  template: `<div class="empty-state" role="alert"><h3>{{ message() || ('ui.error' | transloco) }}</h3>
    @if (retryable()) { <button pButton type="button" (click)="retry.emit()"><span pButtonLabel>{{ 'ui.retry' | transloco }}</span></button> }
  </div>`,
})
export class ErrorState {
  readonly message = input('');
  readonly retryable = input(true);
  readonly retry = output<void>();
}

@Component({
  selector: 'hg-confirm',
  imports: [Modal, ButtonModule, TranslocoPipe],
  template: `<hg-modal [title]="title()" [(open)]="open">
    <p>{{ message() }}</p><div class="table-toolbar">
      <button pButton type="button" severity="secondary" (click)="open.set(false)"><span pButtonLabel>{{ 'ui.cancel' | transloco }}</span></button>
      <button pButton type="button" (click)="confirm()"><span pButtonLabel>{{ 'ui.confirm' | transloco }}</span></button>
    </div></hg-modal>`,
})
export class ConfirmationDialog {
  readonly title = input.required<string>();
  readonly message = input.required<string>();
  readonly open = model(false);
  readonly confirmed = output<void>();
  protected confirm(): void { this.open.set(false); this.confirmed.emit(); }
}
