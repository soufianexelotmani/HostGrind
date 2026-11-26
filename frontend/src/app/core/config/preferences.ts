import { DOCUMENT } from '@angular/common';
import { DestroyRef, Injectable, computed, inject, signal } from '@angular/core';
import { TranslocoService } from '@jsverse/transloco';

export type Language = 'en' | 'fr' | 'ar';
export type Theme = 'light' | 'dark' | 'system';
export const LOCALES: Record<Language, string> = { en: 'en-MA', fr: 'fr-MA', ar: 'ar-MA' };
const key = 'hostgrid.ui.v1';

@Injectable({ providedIn: 'root' })
export class Preferences {
  private readonly document = inject(DOCUMENT);
  private readonly transloco = inject(TranslocoService);
  private readonly window = this.document.defaultView;
  private readonly stored = this.read();
  private readonly media = this.window?.matchMedia?.('(prefers-color-scheme: dark)');
  private readonly systemDark = signal(this.media?.matches ?? false);
  private readonly languageState = signal<Language>(this.stored.language ?? this.browserLanguage());
  private readonly themeState = signal<Theme>(this.stored.theme ?? 'system');
  private readonly collapsedState = signal(this.stored.collapsed ?? false);

  readonly language = this.languageState.asReadonly();
  readonly theme = this.themeState.asReadonly();
  readonly collapsed = this.collapsedState.asReadonly();
  readonly locale = computed(() => LOCALES[this.language()]);
  readonly dark = computed(() => this.theme() === 'dark' || this.theme() === 'system' && this.systemDark());

  constructor() {
    const changed = (event: MediaQueryListEvent) => { this.systemDark.set(event.matches); this.renderTheme(); };
    this.media?.addEventListener('change', changed);
    inject(DestroyRef).onDestroy(() => this.media?.removeEventListener('change', changed));
    this.renderLanguage();
    this.renderTheme();
  }

  setLanguage(language: Language): void { this.languageState.set(language); this.renderLanguage(); this.save(); }
  setTheme(theme: Theme): void { this.themeState.set(theme); this.renderTheme(); this.save(); }
  toggleSidebar(): void { this.collapsedState.update(value => !value); this.save(); }

  private renderLanguage(): void {
    this.document.documentElement.lang = this.language();
    this.document.documentElement.dir = this.language() === 'ar' ? 'rtl' : 'ltr';
    this.transloco.setActiveLang(this.language());
  }

  private renderTheme(): void {
    this.document.documentElement.classList.toggle('hostgrid-dark', this.dark());
  }

  private browserLanguage(): Language {
    const language = this.window?.navigator.language.toLowerCase().split('-')[0];
    return language === 'fr' || language === 'ar' ? language : 'en';
  }

  private read(): { language?: Language; theme?: Theme; collapsed?: boolean } {
    try {
      const value = JSON.parse(this.window?.localStorage.getItem(key) ?? '{}');
      return {
        language: ['en', 'fr', 'ar'].includes(value?.language) ? value.language : undefined,
        theme: ['light', 'dark', 'system'].includes(value?.theme) ? value.theme : undefined,
        collapsed: typeof value?.collapsed === 'boolean' ? value.collapsed : undefined,
      };
    } catch { return {}; }
  }

  private save(): void {
    try {
      this.window?.localStorage.setItem(key, JSON.stringify({
        language: this.language(), theme: this.theme(), collapsed: this.collapsed(),
      }));
    } catch { /* Private or restricted storage does not prevent UI changes. */ }
  }
}
