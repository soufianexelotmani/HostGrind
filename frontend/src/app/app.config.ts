import { ApplicationConfig, inject, provideAppInitializer, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideRouter } from '@angular/router';
import { provideTransloco, TranslocoService } from '@jsverse/transloco';
import { providePrimeNG } from 'primeng/config';
import { MessageService } from 'primeng/api';
import { firstValueFrom } from 'rxjs';
import { AuthService } from './core/auth/auth.service';
import { authInterceptor } from './core/auth/auth.interceptor';
import { Preferences } from './core/config/preferences';
import { HostGridPreset } from './core/config/theme';
import { UiTranslationLoader } from './core/config/ui-translations';

import { routes } from './app.routes';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    provideHttpClient(withInterceptors([authInterceptor])),
    providePrimeNG({ theme: { preset: HostGridPreset, options: {
      darkModeSelector: '.hostgrid-dark',
      cssLayer: { name: 'primeng', order: 'theme, base, primeng, components, utilities' },
    } } }),
    MessageService,
    provideTransloco({ config: { availableLangs: ['en', 'fr', 'ar'], defaultLang: 'en',
      fallbackLang: 'en', reRenderOnLangChange: true, prodMode: true }, loader: UiTranslationLoader }),
    provideAppInitializer(() => firstValueFrom(inject(TranslocoService).load(inject(Preferences).language()))),
    provideAppInitializer(() => firstValueFrom(inject(AuthService).refresh())),
  ]
};
