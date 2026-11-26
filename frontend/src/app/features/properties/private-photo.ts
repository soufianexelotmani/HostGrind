import { Component, DestroyRef, ElementRef, effect, inject, input, signal } from '@angular/core';
import { PortfolioApi } from './portfolio-api';

@Component({
  selector: 'hg-private-photo',
  template: `@if (url()) { <img [src]="url()" [alt]="alt()" loading="lazy" decoding="async"> }`,
  styles: [':host { display:block; background:var(--surface-alt); overflow:hidden; aspect-ratio: 16 / 10; }',
    'img { inline-size:100%; block-size:100%; object-fit:cover; }'],
})
export class PrivatePhoto {
  readonly organization = input.required<string>();
  readonly property = input.required<string>();
  readonly photo = input<string | null>(null);
  readonly alt = input('');
  protected readonly url = signal('');
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
      const photo = this.photo();
      if (!photo || !this.visible()) return;
      let objectUrl = '';
      const subscription = this.api.blob(this.organization(), this.property(), 'photos', photo).subscribe({
        next: blob => { objectUrl = URL.createObjectURL(blob); this.url.set(objectUrl); },
        error: () => this.url.set(''),
      });
      cleanup(() => { subscription.unsubscribe(); if (objectUrl) URL.revokeObjectURL(objectUrl); });
    });
  }
}
