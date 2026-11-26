import { TestBed } from '@angular/core/testing';
import { provideTransloco } from '@jsverse/transloco';
import { Preferences } from './preferences';
import { navigationFor, navigationSearch } from './navigation';
import { UiTranslationLoader } from './ui-translations';
import { CurrentUser } from '../auth/auth.service';

describe('workspace preferences and navigation', () => {
  const key = 'hostgrid.ui.v1';
  const owner: CurrentUser = { id: 'test', email: 'owner@example.test', mfaEnabled: false, superAdmin: false,
    memberships: [{ organizationId: 'one', role: 'Owner', ownerId: 'owner' }] };

  beforeEach(() => {
    localStorage.removeItem(key);
    TestBed.configureTestingModule({ providers: [
      provideTransloco({ config: { defaultLang: 'en', availableLangs: ['en', 'fr', 'ar'] }, loader: UiTranslationLoader }),
    ] });
  });

  afterEach(() => {
    localStorage.removeItem(key);
    document.documentElement.dir = 'ltr';
    document.documentElement.lang = 'en';
    document.documentElement.classList.remove('hostgrid-dark');
  });

  it('switches theme and persists only UI choices', () => {
    const preferences = TestBed.inject(Preferences);
    preferences.setTheme('dark');
    preferences.toggleSidebar();
    expect(document.documentElement.classList.contains('hostgrid-dark')).toBe(true);
    expect(preferences.collapsed()).toBe(true);
    expect(Object.keys(JSON.parse(localStorage.getItem(key)!)).sort()).toEqual(['collapsed', 'language', 'theme']);
    preferences.setTheme('light');
    expect(document.documentElement.classList.contains('hostgrid-dark')).toBe(false);
  });

  it('restores an Arabic dark workspace and mirrors document direction', () => {
    localStorage.setItem(key, JSON.stringify({ language: 'ar', theme: 'dark', collapsed: true }));
    const preferences = TestBed.inject(Preferences);
    expect(preferences.collapsed()).toBe(true);
    expect(preferences.dark()).toBe(true);
    expect(document.documentElement.dir).toBe('rtl');
    expect(document.documentElement.lang).toBe('ar');
    preferences.setLanguage('fr');
    expect(document.documentElement.dir).toBe('ltr');
    expect(preferences.locale()).toBe('fr-MA');
  });

  it('returns menus only for roles the account actually has', () => {
    expect(navigationFor('SuperAdmin', owner)).toEqual([]);
    expect(navigationFor('Manager', owner)).toEqual([]);
    expect(navigationFor('Owner', owner).some(item => item.label === 'myProperties')).toBe(true);
    expect(navigationFor('SuperAdmin', { ...owner, superAdmin: true }).some(item => item.label === 'audit')).toBe(true);
    const manager: CurrentUser = { ...owner, memberships: [{ organizationId: 'one', role: 'Manager', ownerId: null }] };
    expect(navigationFor('Manager', manager).some(item => item.label === 'payouts')).toBe(true);
  });

  it('matches localized authorized links and omits upcoming modules', () => {
    const menu = navigationFor('Owner', owner);
    expect(navigationSearch.find(menu, 'fr', 'tableau').map(link => link.route)).toEqual(['/owner']);
    expect(navigationSearch.find(menu, 'en', 'properties').map(link => link.route)).toEqual(['/owner/properties']);
    expect(navigationSearch.find(menu, 'en', 'reservations')).toEqual([]);
    expect(navigationSearch.find(menu, 'ar', 'لوحة').map(link => link.route)).toEqual(['/owner']);
  });
});
