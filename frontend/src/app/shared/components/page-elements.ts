import { Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslocoPipe } from '@jsverse/transloco';
import { LucideArchive as Archive, LucideDynamicIcon } from '@lucide/angular';

export interface Crumb { readonly label: string; readonly route?: string }

@Component({
  selector: 'hg-breadcrumbs',
  imports: [RouterLink, TranslocoPipe],
  template: `
    <nav class="breadcrumbs" [attr.aria-label]="'ui.breadcrumb' | transloco">
      <ol>
        @for (crumb of items(); track $index; let last = $last) {
          <li>@if (crumb.route && !last) { <a [routerLink]="crumb.route">{{ crumb.label }}</a> }
              @else { <span [attr.aria-current]="last ? 'page' : null">{{ crumb.label }}</span> }</li>
        }
      </ol>
    </nav>`,
})
export class Breadcrumbs { readonly items = input.required<readonly Crumb[]>(); }

@Component({
  selector: 'hg-page-header',
  template: `<div class="page-header"><div><h1>{{ title() }}</h1>@if (description()) { <p>{{ description() }}</p> }</div><ng-content /></div>`,
})
export class PageHeader {
  readonly title = input.required<string>();
  readonly description = input('');
}

@Component({
  selector: 'hg-empty-state',
  imports: [LucideDynamicIcon],
  template: `<div class="empty-state"><div class="empty-icon"><svg [lucideIcon]="icon" [size]="23" aria-hidden="true"></svg></div>
    <h3>{{ title() }}</h3>@if (description()) { <p>{{ description() }}</p> }<ng-content /></div>`,
})
export class EmptyState {
  readonly title = input.required<string>();
  readonly description = input('');
  protected readonly icon = Archive;
}

@Component({
  selector: 'hg-avatar',
  template: `<span class="avatar" aria-hidden="true">{{ name().trim().charAt(0).toUpperCase() }}</span>`,
})
export class Avatar { readonly name = input.required<string>(); }
