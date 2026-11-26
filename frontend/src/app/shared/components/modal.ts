import { Component, ElementRef, effect, input, model, viewChild } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { LucideDynamicIcon, LucideX as X } from '@lucide/angular';

@Component({
  selector: 'hg-modal',
  imports: [TranslocoPipe, LucideDynamicIcon],
  template: `
    <dialog #dialog [class.drawer]="drawer()" [attr.aria-label]="title()" (close)="open.set(false)"
      (keydown.escape)="dismiss($event)">
      <div class="modal-header">
        <h2>{{ title() }}</h2>
        <button type="button" class="icon-button" [attr.aria-label]="'ui.close' | transloco" (click)="open.set(false)">
          <svg [lucideIcon]="closeIcon" [size]="19" aria-hidden="true"></svg>
        </button>
      </div>
      <div class="modal-body"><ng-content /></div>
    </dialog>`,
})
export class Modal {
  readonly title = input.required<string>();
  readonly open = model(false);
  readonly drawer = input(false);
  protected readonly closeIcon = X;
  private readonly dialog = viewChild<ElementRef<HTMLDialogElement>>('dialog');

  protected dismiss(event: Event): void {
    event.preventDefault();
    event.stopPropagation();
    this.open.set(false);
  }

  constructor() {
    effect(() => {
      const dialog = this.dialog()?.nativeElement;
      if (!dialog) return;
      if (this.open() && !dialog.open) dialog.showModal();
      else if (!this.open() && dialog.open) dialog.close();
    });
  }
}
