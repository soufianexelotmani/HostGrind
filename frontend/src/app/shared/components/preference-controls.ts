import { Component, inject } from '@angular/core';
import { TranslocoDirective } from '@jsverse/transloco';
import { LucideGlobe as Globe2, LucideDynamicIcon, LucideMonitor as Monitor, LucideMoon as Moon, LucideSun as Sun } from '@lucide/angular';
import { Language, Preferences, Theme } from '../../core/config/preferences';

@Component({
  selector: 'hg-preferences',
  imports: [TranslocoDirective, LucideDynamicIcon],
  template: `
    <div class="preferences" *transloco="let t; prefix: 'ui'">
      <label class="preference">
        <svg [lucideIcon]="globe" [size]="16" aria-hidden="true"></svg>
        <select [attr.aria-label]="t('language')" [value]="prefs.language()" (change)="language($event)">
          <option value="en">English</option><option value="fr">Français</option><option value="ar">العربية</option>
        </select>
      </label>
      <label class="preference">
        <svg [lucideIcon]="prefs.theme() === 'system' ? system : prefs.dark() ? moon : sun" [size]="16" aria-hidden="true"></svg>
        <select [attr.aria-label]="t('theme')" [value]="prefs.theme()" (change)="theme($event)">
          <option value="system">{{ t('system') }}</option><option value="light">{{ t('light') }}</option><option value="dark">{{ t('dark') }}</option>
        </select>
      </label>
    </div>`,
})
export class PreferenceControls {
  protected readonly prefs = inject(Preferences);
  protected readonly globe = Globe2;
  protected readonly sun = Sun;
  protected readonly moon = Moon;
  protected readonly system = Monitor;
  protected language(event: Event): void { this.prefs.setLanguage((event.target as HTMLSelectElement).value as Language); }
  protected theme(event: Event): void { this.prefs.setTheme((event.target as HTMLSelectElement).value as Theme); }
}
