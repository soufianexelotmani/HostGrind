import { Component, inject, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslocoPipe } from '@jsverse/transloco';
import { BRANDING } from '../../core/config/branding';
import { PreferenceControls } from '../../shared/components/preference-controls';

@Component({
  selector: 'hg-auth-layout',
  imports: [RouterLink, TranslocoPipe, PreferenceControls],
  template: `
    <div class="auth-layout" [class.embedded]="embedded()">
      @if (!embedded()) {
        <aside class="auth-visual" aria-hidden="true">
          <img [src]="brand.hero" alt="" fetchpriority="high">
          <div class="auth-caption"><span>{{ brand.name }}</span><p>{{ 'ui.tagline' | transloco }}</p></div>
        </aside>
      }
      <div class="auth-panel" [attr.role]="embedded() ? null : 'main'">
        @if (!embedded()) {
          <header class="auth-toolbar">
            <a routerLink="/" class="wordmark"><img [src]="brand.mark" alt="" width="40" height="40"><span>{{ brand.name }}</span></a>
            <hg-preferences/>
          </header>
        }
        <div class="auth-content"><ng-content/></div>
      </div>
    </div>`,
})
export class AuthLayout {
  protected readonly brand = inject(BRANDING);
  readonly embedded = input(false);
}
