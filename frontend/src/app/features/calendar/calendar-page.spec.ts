import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { provideTransloco } from '@jsverse/transloco';
import { UiTranslationLoader } from '../../core/config/ui-translations';
import { PortfolioContext } from '../properties/portfolio-context';
import { CalendarView } from './calendar-models';
import { CalendarPage } from './calendar-page';

describe('portfolio calendar', () => {
  it('loads 30 dates, cancels obsolete queries and protects a hold hidden by a display filter', async () => {
    const requestBase = '/api/v1/organizations/test-org/';
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([]),
        provideTransloco({ config: { availableLangs: ['en', 'fr', 'ar'], defaultLang: 'en' }, loader: UiTranslationLoader }),
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: convertToParamMap({}),
          queryParamMap: convertToParamMap({ date: '2032-01-01' }) } } }],
    }).overrideComponent(CalendarPage, { set: { providers: [{ provide: PortfolioContext, useValue: {
      organization: signal('test-org'), initialize: async () => {}, locale: 'en-US', label: (key: string) => key,
    } }] } });
    await TestBed.compileComponents();
    const fixture = TestBed.createComponent(CalendarPage);
    const root: HTMLElement = fixture.nativeElement;
    const http = TestBed.inject(HttpTestingController);
    fixture.detectChanges();
    (await vi.waitFor(() => http.expectOne(req => req.url === requestBase + 'owners'))).flush({ items: [] });
    (await vi.waitFor(() => http.expectOne(req => req.url === requestBase + 'calendar/summary'))).flush({});
    const first = await vi.waitFor(() => http.expectOne(req => req.url === requestBase + 'calendar' &&
      !req.params.has('bookingSource')));
    expect(first.request.params.get('startDate')).toBe('2032-01-01');
    expect(first.request.params.get('endDate')).toBe('2032-01-31');

    const source = Array.from(root.querySelectorAll<HTMLSelectElement>('label select')).find(select =>
      select.parentElement?.textContent?.startsWith('Booking source'))!;
    source.value = '0';
    source.dispatchEvent(new Event('change'));
    fixture.detectChanges();
    const filtered = await vi.waitFor(() => http.expectOne(req => req.url === requestBase + 'calendar' &&
      req.params.get('bookingSource') === '0'));
    expect(first.cancelled).toBe(true);

    const feed: CalendarView = {
      startDate: '2032-01-01', endDate: '2032-01-31', businessDate: '2032-01-01',
      properties: { pageNumber: 1, pageSize: 30, total: 1, items: [{ id: 'p', name: 'Fixture', reference: 'P1',
        type: 'riad', ownerName: null, status: 0, operationalStatus: 1 }] },
      reservations: [], blocks: [], blockHolds: [],
      reservationHolds: [{ propertyId: 'p', startDate: '2032-01-01', endDate: '2032-01-02' }],
    };
    filtered.flush(feed);
    fixture.detectChanges();
    expect(root.querySelectorAll('[data-calendar-date]')).toHaveLength(30);
    expect(root.querySelector('[data-date="2032-01-01"]')?.classList.contains('held')).toBe(true);
    expect(root.querySelector('[data-date="2032-01-01"] button')).toBeNull();
    expect(root.querySelector('[data-date="2032-01-02"] button')).not.toBeNull();
    // The summary can refresh with the property filters but does not use event-only filters.
    for (const pending of http.match(req => req.url === requestBase + 'calendar/summary')) pending.flush({});
    http.verify();
  });
});
