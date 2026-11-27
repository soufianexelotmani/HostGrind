import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { provideTransloco, TranslocoService } from '@jsverse/transloco';
import { firstValueFrom } from 'rxjs';
import { UiTranslationLoader } from '../../core/config/ui-translations';
import { PortfolioContext } from '../properties/portfolio-context';
import { ReservationForm } from './reservation-form';

const api = '/api/v1/organizations/org-1/';
const date = '2031-06-20';

describe('reservation form', () => {
  let http: HttpTestingController;
  let historical: boolean;

  beforeEach(() => {
    historical = false;
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([]),
        provideTransloco({ config: { availableLangs: ['en', 'fr', 'ar'], defaultLang: 'en' }, loader: UiTranslationLoader }),
        { provide: ActivatedRoute, useFactory: () => ({ snapshot: { paramMap: convertToParamMap({}),
          queryParamMap: convertToParamMap({}), data: { historical } } }) }],
    }).overrideComponent(ReservationForm, { set: { providers: [{
      provide: PortfolioContext, useValue: { organization: signal('org-1'), initialize: async () => {}, locale: 'en',
        label: (key: string) => key, choices: signal([]) },
    }] } });
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => http.verify());

  it('rejects invalid stay dates and a party exceeding the selected property capacity', async () => {
    const root = await render();
    choose(root, 'propertyId', 'p1');
    const property = await vi.waitFor(() => http.expectOne(api + 'properties/p1'));
    property.flush({ maxGuests: 2 });
    await fixture.whenStable();
    choose(root, 'guestId', 'g1');
    set(root, 'checkInDate', date);
    set(root, 'checkOutDate', date);
    submit(root);
    await fixture.whenStable();
    expect(root.querySelector('[role=alert]')?.textContent).toContain('Checkout must be after check-in');
    http.expectNone(api + 'reservations');
    set(root, 'checkOutDate', '2031-06-22');
    set(root, 'adults', '3');
    submit(root);
    await fixture.whenStable();
    expect(root.querySelector('[role=alert]')?.textContent).toContain('exceeds the property capacity');
    http.expectNone(api + 'reservations');
  });

  it('requests matching contacts in the body and explicitly selects a suggested guest', async () => {
    const root = await render();
    button(root, 'New guest').click();
    await fixture.whenStable();
    set(root, 'email', 'guest@example.test');
    set(root, 'phone', '+212 612 345 678');
    button(root, 'Find matching guests').click();
    const request = http.expectOne(api + 'guests/suggestions');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ email: 'guest@example.test', phone: '+212 612 345 678' });
    request.flush([{ id: 'match', name: 'Matching guest' }]);
    await fixture.whenStable();
    button(root, 'Matching guest').click();
    await fixture.whenStable();
    expect(root.querySelector<HTMLSelectElement>('[formControlName=guestId]')?.value).toBe('match');
  });

  it('sends an explicit override and net mode, and exposes conflicts and temporary failure', async () => {
    const root = await render();
    await fillStay(root);
    button(root, 'Preview nightly prices').click();
    http.expectOne(api + 'reservations/pricing-preview').flush([{ night: date, amount: 100, source: 'custom' }]);
    await fixture.whenStable();
    const override = root.querySelector<HTMLInputElement>('[aria-label="Manual override ' + date + '"]')!;
    override.value = '120.25';
    override.dispatchEvent(new Event('input'));
    submit(root);
    const write = http.expectOne({ method: 'POST', url: api + 'reservations' });
    expect(write.request.body).toMatchObject({ mode: 2, grossAmount: null, platformFees: null, confirm: false,
      nightlyPrices: [{ night: date, amount: 120.25 }] });
    write.flush({ code: 'booking_conflict' }, { status: 409, statusText: 'Conflict' });
    await fixture.whenStable();
    expect(root.querySelector('[role=alert]')?.textContent).toContain('overlap a reservation');
    submit(root);
    http.expectOne({ method: 'POST', url: api + 'reservations' })
      .flush({ code: 'availability_lock_timeout' }, { status: 503, statusText: 'Unavailable' });
    await fixture.whenStable();
    expect(root.querySelector('[role=alert]')?.textContent).toContain('Try again');
  });

  it('requires private evidence before submitting historical details', async () => {
    historical = true;
    const root = await render();
    await fillStay(root);
    set(root, 'confirmationDate', '2031-05-20');
    set(root, 'reason', 'Fictitious evidence missing');
    submit(root);
    await fixture.whenStable();
    expect(root.querySelector('[role=alert]')?.textContent).toContain('Attach a supporting private PDF');
    http.expectNone(api + 'reservations/historical');
  });

  let fixture: ReturnType<typeof TestBed.createComponent<ReservationForm>>;
  async function render(): Promise<HTMLElement> {
    await firstValueFrom(TestBed.inject(TranslocoService).load('en'));
    fixture = TestBed.createComponent(ReservationForm);
    const properties = await vi.waitFor(() => http.expectOne(api + 'properties?status=Active&size=100'));
    properties.flush({ items: [{ id: 'p1', name: 'Test property' }] });
    const guests = await vi.waitFor(() => http.expectOne(api + 'guests?size=100'));
    guests.flush({ items: [{ id: 'g1', name: 'Test guest' }] });
    await fixture.whenStable();
    await vi.waitFor(() => expect(fixture.nativeElement.querySelector('form button[type=submit]').disabled).toBe(false));
    return fixture.nativeElement;
  }

  async function fillStay(root: HTMLElement): Promise<void> {
    choose(root, 'propertyId', 'p1');
    const property = await vi.waitFor(() => http.expectOne(api + 'properties/p1'));
    property.flush({ maxGuests: 2 });
    await fixture.whenStable();
    choose(root, 'guestId', 'g1');
    set(root, 'checkInDate', date);
    set(root, 'checkOutDate', '2031-06-22');
  }

  function choose(root: HTMLElement, field: string, value: string): void {
    const element = root.querySelector<HTMLSelectElement>('[formControlName=' + field + ']')!;
    element.value = value;
    element.dispatchEvent(new Event('change'));
  }
  function set(root: HTMLElement, field: string, value: string): void {
    const element = root.querySelector<HTMLInputElement>('[formControlName=' + field + ']')!;
    element.value = value;
    element.dispatchEvent(new Event('input'));
  }
  function submit(root: HTMLElement): void { root.querySelector('form')!.dispatchEvent(new Event('submit', { cancelable: true })); }
  function button(root: HTMLElement, title: string): HTMLButtonElement {
    return [...root.querySelectorAll('button')].find(item => item.textContent.trim() === title)!;
  }
});
