import { Injectable, inject, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { TranslocoService } from '@jsverse/transloco';
import { firstValueFrom } from 'rxjs';
import { AuthService } from '../../core/auth/auth.service';
import { Preferences } from '../../core/config/preferences';
import { PORTFOLIO_COPY } from './portfolio-copy';
import { PortfolioApi } from './portfolio-api';

@Injectable()
export class PortfolioContext {
  private readonly auth = inject(AuthService);
  private readonly route = inject(ActivatedRoute);
  private readonly api = inject(PortfolioApi);
  private readonly translations = inject(TranslocoService);
  private readonly preferences = inject(Preferences);
  readonly organization = signal('');
  readonly choices = signal<{ id: string; name: string }[]>([]);
  readonly role = this.route.snapshot.data['role'] as 'Manager' | 'Owner' | 'SuperAdmin';
  readonly workspace = this.role === 'SuperAdmin' ? 'admin' : this.role === 'Manager' ? 'manager' : 'owner';
  readonly manage = this.role !== 'Owner';

  label(key: string): string { return this.translations.translate('portfolio.' + key, {}, this.preferences.language()); }
  get locale(): string { return this.preferences.locale(); }

  async initialize(): Promise<void> {
    const languages = ['en', 'fr', 'ar'] as const;
    await Promise.all(languages.map(language => firstValueFrom(this.translations.load(language))));
    for (const language of languages)
      this.translations.setTranslation({ portfolio: PORTFOLIO_COPY[language] }, language, { merge: true, emitChange: false });

    if (this.role === 'SuperAdmin') {
      const organizations: { id: string; name: string }[] = [];
      for (let page = 1; page <= 100; page++) {
        const batch = await firstValueFrom(this.api.organizations(page));
        organizations.push(...batch.filter(row => row.isActive));
        if (batch.length < 50) break;
      }
      this.choices.set(organizations);
    } else {
      this.choices.set(this.auth.user()?.memberships.filter(member => member.role === this.role)
        .map(member => ({ id: member.organizationId, name: member.organizationId })) ?? []);
    }
    const requested = this.route.snapshot.queryParamMap.get('organization');
    this.organization.set(requested && this.choices().some(choice => choice.id === requested)
      ? requested : this.choices()[0]?.id ?? '');
  }

  link(area: 'properties' | 'owners', id?: string): string {
    return '/' + this.workspace + '/' + area + (id ? '/' + id : '');
  }
}
