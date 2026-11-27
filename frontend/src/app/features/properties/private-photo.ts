import { Component, DestroyRef, ElementRef, effect, inject, input, signal } from '@angular/core';
import { PortfolioApi } from './portfolio-api';

@Component({
  selector: 'hg-private-photo',
  template: `
    @if (url() && !failed()) {
      <img [src]="url()" [alt]="alt()" loading="lazy" decoding="async" (error)="failed.set(true)">
    } @else if (failed() && errorLabel()) {
      <p role="status">{{ errorLabel() }}</p>
    } @else if (loading() && loadingLabel()) {
      <p role="status">{{ loadingLabel() }}</p>
    }`,
  styles: [':host { display:block; background:var(--surface-alt); overflow:hidden; aspect-ratio: 16 / 10; }',
    'img { inline-size:100%; block-size:100%; object-fit:cover; }'],
})
export class PrivatePhoto {
  readonly organization = input.required<string>();
  readonly property = input.required<string>();
  readonly photo = input<string | null>(null);
  readonly alt = input('');
  readonly loadingLabel = input('');
  readonly errorLabel = input('');
  protected readonly url = signal('');
  protected readonly loading = signal(false);
  protected readonly failed = signal(false);
  private readonly element = inject(ElementRef<HTMLElement>);
  private readonly api = inject(PortfolioApi);
  private readonly visible = signal(false);

  constructor() {
    const observer = typeof IntersectionObserver === 'undefined' ? null :
      new IntersectionObserver(entries => {
        if (entries.some(entry => entry.isIntersecting)) { this.visible.set(true); observer?.disconnect(); }
      }, { rootMargin: '160px' });
    if (observer) observer.observe(this.element.nativeElement);
    else this.visible.set(true);
    inject(DestroyRef).onDestroy(() => observer?.disconnect());
    effect(cleanup => {
      this.url.set('');
      this.failed.set(false);
      this.loading.set(false);
      const photo = this.photo();
      if (!photo || !this.visible()) return;
      this.loading.set(true);
      let objectUrl = '';
      const subscription = this.api.blob(this.organization(), this.property(), 'photos', photo).subscribe({
        next: blob => { objectUrl = URL.createObjectURL(blob); this.url.set(objectUrl); this.loading.set(false); },
        error: () => { this.failed.set(true); this.loading.set(false); },
      });
      cleanup(() => { subscription.unsubscribe(); if (objectUrl) URL.revokeObjectURL(objectUrl); });
    });
  }
}
