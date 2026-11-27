import { HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { TranslocoService } from '@jsverse/transloco';
import { Preferences } from '../../core/config/preferences';
import { PortfolioPage } from '../properties/portfolio-page';
import { copy } from './booking-copy';

@Injectable()
export abstract class BookingPage extends PortfolioPage {
  private readonly translator = inject(TranslocoService);
  private readonly preferences = inject(Preferences);

  protected begin(work: () => Promise<void>): void {
    void this.start(async () => {
      for (const language of ['en', 'fr', 'ar'] as const)
        this.translator.setTranslation({ booking: copy[language] }, language, { merge: true, emitChange: false });
      await work();
    });
  }

  protected t(key: string): string {
    const lang = this.preferences.language();
    return Object.hasOwn(copy[lang], key) ? this.translator.translate('booking.' + key, {}, lang) : this.ctx.label(key);
  }

  protected date(day: string | null): string {
    return day ? new Intl.DateTimeFormat(this.ctx.locale, { dateStyle: 'medium', timeZone: 'UTC' })
      .format(new Date(day.slice(0, 10) + 'T00:00:00Z')) : '—';
  }

  protected money(value: number | null): string {
    return value === null ? '—' : new Intl.NumberFormat(this.ctx.locale, { style: 'currency', currency: 'MAD' }).format(value);
  }

  protected capture(error: unknown): void {
    if (!(error instanceof HttpErrorResponse)) { this.problem.set('error'); return; }
    const code = error.error?.code;
    this.problem.set(typeof code === 'string' && Object.hasOwn(copy.en, code) ? code :
      ({ 400: 'invalid', 403: 'forbidden', 404: 'missing', 409: 'conflict', 503: 'temporary' } as Record<number, string>)[error.status] ?? 'error');
  }

  protected async action(work: () => Promise<void>): Promise<void> {
    await this.run(async () => {
      try { await work(); }
      catch (error) { this.capture(error); }
    });
  }
}
