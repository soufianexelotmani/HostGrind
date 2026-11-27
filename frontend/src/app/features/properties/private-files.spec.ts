import { HttpEventType, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { provideTransloco, TranslocoService } from '@jsverse/transloco';
import { firstValueFrom } from 'rxjs';
import { UiTranslationLoader } from '../../core/config/ui-translations';
import { FilePicker } from '../../shared/components/form-elements';
import { PrivateFile } from './portfolio-api';
import { PortfolioContext } from './portfolio-context';
import { PrivatePhoto } from './private-photo';
import { PropertyDetails } from './property-details';

const path = '/api/v1/organizations/org-1/properties/property-1';
const pdf: PrivateFile = {
  id: 'pdf-1', name: 'Example.pdf', contentType: 'application/pdf', sizeBytes: 100,
  uploadedAt: '2026-11-26T10:00:00Z', uploaderId: 'manager-1', scanStatus: 0, readable: true, sortOrder: null,
};

describe('property details acceptance', () => {
  let http: HttpTestingController;
  let manage: boolean;

  beforeEach(async () => {
    manage = true;
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([]),
        provideTransloco({ config: { defaultLang: 'en' }, loader: UiTranslationLoader }),
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: convertToParamMap({ id: 'property-1' }) } } }],
    }).overrideComponent(PropertyDetails, { set: { providers: [{
      provide: PortfolioContext,
      useFactory: () => ({
        manage, organization: signal('org-1'), initialize: async () => {}, locale: 'en-GB',
        label: (key: string) => key, link: () => '/manager/properties',
      }),
    }] } });
    http = TestBed.inject(HttpTestingController);
    await firstValueFrom(TestBed.inject(TranslocoService).load('en'));
  });

  afterEach(() => {
    http.verify();
    vi.restoreAllMocks();
    vi.unstubAllGlobals();
  });

  it('reports upload progress, clears it after a server rejection and reloads the gallery', async () => {
    const fixture = await details();
    await vi.waitFor(() => {
      fixture.detectChanges();
      expect(fixture.nativeElement.querySelector('input[type=file]').disabled).toBe(false);
    });
    const picker = fixture.debugElement.query(element => element.componentInstance instanceof FilePicker)
      .componentInstance as FilePicker;
    picker.selected.emit([new File(['example'], 'example.png')]);
    const upload = await vi.waitFor(() => http.expectOne({ method: 'POST', url: path + '/photos' }));
    expect(upload.request.reportProgress).toBe(true);
    expect(upload.request.body.get('file').name).toBe('example.png');
    upload.event({ type: HttpEventType.UploadProgress, loaded: 50, total: 100 });
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('progress').value).toBe(50);
    upload.flush({}, { status: 415, statusText: 'Unsupported Media Type' });
    const reload = await vi.waitFor(() => http.expectOne(path));
    reload.flush(property());
    await vi.waitFor(() => {
      fixture.detectChanges();
      expect(fixture.nativeElement.querySelector('progress')).toBeNull();
    });
    expect(fixture.nativeElement.querySelector('[role=alert]').textContent).toContain('invalidFiles');
  });

  it('shows available Owner documents with download controls only', async () => {
    manage = false;
    const fixture = await details();
    fixture.nativeElement.querySelector('#tab-documents').click();
    fixture.detectChanges();
    const panel: HTMLElement = fixture.nativeElement.querySelector('#property-tab-content');
    expect(panel.textContent).toContain('Example.pdf');
    expect(panel.querySelector('input[type=file]')).toBeNull();
    expect(Array.from(panel.querySelectorAll('button'), button => button.textContent?.trim())).toEqual(['download']);
  });

  it('rejects reversed contract dates and submits an empty agreement rate as null', async () => {
    const fixture = await details();
    fixture.nativeElement.querySelector('#tab-contracts').click();
    fixture.detectChanges();
    const form = fixture.nativeElement.querySelector('form') as HTMLFormElement;
    setInput(form, 'startDate', '2026-12-01');
    setInput(form, 'endDate', '2026-11-30');
    submit(form);
    http.expectNone(path + '/contracts');
    setInput(form, 'endDate', '');
    submit(form);
    const request = http.expectOne({ method: 'POST', url: path + '/contracts' });
    expect(request.request.body).toMatchObject({ startDate: '2026-12-01', endDate: null, rate: null, documentId: null });
    request.flush(null);
    const refreshed = await vi.waitFor(() => http.expectOne(path));
    refreshed.flush(property());
    await fixture.whenStable();
  });

  it('rejects past or invalid commission changes and confirms a valid effective date', async () => {
    const fixture = await details();
    fixture.nativeElement.querySelector('#tab-pricing').click();
    fixture.detectChanges();
    const form = fixture.nativeElement.querySelector('form') as HTMLFormElement;
    setInput(form, 'effectiveFrom', '2026-11-25');
    setInput(form, 'rate', '20');
    submit(form);
    setInput(form, 'effectiveFrom', '2026-11-27');
    setInput(form, 'rate', '101');
    submit(form);
    http.expectNone(path + '/commission-rates');
    expect(fixture.componentInstance['confirmOpen']()).toBe(false);
    setInput(form, 'rate', '24.25');
    submit(form);
    expect(fixture.componentInstance['confirmOpen']()).toBe(true);
    http.expectNone(path + '/commission-rates');
    fixture.componentInstance['confirmOpen'].set(false);
    fixture.componentInstance['confirmed']();
    const request = http.expectOne({ method: 'POST', url: path + '/commission-rates' });
    expect(request.request.body).toEqual({ effectiveFrom: '2026-11-27', rate: 24.25 });
    request.flush(null);
    const refreshed = await vi.waitFor(() => http.expectOne(path));
    refreshed.flush(property());
    await fixture.whenStable();
  });

  it('shows the active agreement and effective rate to the Owner without mutation forms', async () => {
    manage = false;
    const fixture = await details();
    const root: HTMLElement = fixture.nativeElement;
    root.querySelector<HTMLButtonElement>('#tab-contracts')!.click();
    fixture.detectChanges();
    const panel = root.querySelector('#property-tab-content')!;
    expect(panel.textContent).toContain('activeContract');
    expect(panel.textContent).toContain('25%');
    expect(panel.textContent).toContain('commissionMismatch');
    expect(panel.querySelector('form')).toBeNull();
    root.querySelector<HTMLButtonElement>('#tab-pricing')!.click();
    fixture.detectChanges();
    expect(panel.textContent).toContain('24%');
    expect(panel.querySelector('[formcontrolname=effectiveFrom]')).toBeNull();
  });

  it('loads protected previews, shows a fetch error and revokes retired object URLs', async () => {
    vi.stubGlobal('IntersectionObserver', undefined);
    const create = vi.fn(() => 'blob:preview');
    const revoke = vi.fn();
    vi.stubGlobal('URL', Object.assign(class extends URL {}, { createObjectURL: create, revokeObjectURL: revoke }));
    const fixture = TestBed.createComponent(PrivatePhoto);
    fixture.componentRef.setInput('organization', 'org-1');
    fixture.componentRef.setInput('property', 'property-1');
    fixture.componentRef.setInput('loadingLabel', 'Loading photo');
    fixture.componentRef.setInput('errorLabel', 'Photo unavailable');
    fixture.componentRef.setInput('photo', 'one');
    fixture.detectChanges();
    const image = http.expectOne(path + '/photos/one');
    expect(fixture.nativeElement.textContent).toContain('Loading photo');
    expect(image.request.responseType).toBe('blob');
    image.flush(new Blob(['fixture'], { type: 'image/png' }));
    fixture.detectChanges();
    expect(create).toHaveBeenCalledOnce();
    expect(fixture.nativeElement.querySelector('img').getAttribute('src')).toBe('blob:preview');
    fixture.componentRef.setInput('photo', 'two');
    fixture.detectChanges();
    expect(revoke).toHaveBeenCalledWith('blob:preview');
    http.expectOne(path + '/photos/two').flush(new Blob(), { status: 404, statusText: 'Not Found' });
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Photo unavailable');
  });

  it('validates whole inventory quantities and sends notes with the item', async () => {
    const fixture = await details();
    fixture.nativeElement.querySelector('#tab-inventory').click();
    fixture.detectChanges();
    const form = fixture.nativeElement.querySelector('#property-tab-content form') as HTMLFormElement;
    setInput(form, 'name', 'Coffee maker');
    setInput(form, 'quantity', '1.5');
    submit(form);
    http.expectNone(path + '/inventory');
    setInput(form, 'quantity', '2');
    setInput(form, 'notes', 'Spare filter');
    submit(form);
    const request = http.expectOne({ method: 'POST', url: path + '/inventory' });
    expect(request.request.body).toMatchObject({ name: 'Coffee maker', quantity: 2, notes: 'Spare filter' });
    request.flush(null);
    const reload = await vi.waitFor(() => http.expectOne(path));
    reload.flush(property());
  });

  it('offers Owners a bounded preview while hiding pricing write actions', async () => {
    manage = false;
    const fixture = await details();
    fixture.nativeElement.querySelector('#tab-pricing').click();
    fixture.detectChanges();
    const panel: HTMLElement = fixture.nativeElement.querySelector('#property-tab-content');
    expect(panel.querySelector('[formControlName=nightlyRate]')).toBeNull();
    const form = panel.querySelector('form')!;
    setInput(form, 'start', '2026-12-01');
    setInput(form, 'end', '2026-12-01');
    submit(form);
    http.expectNone(request => request.url === path + '/pricing-preview');
    setInput(form, 'end', '2026-12-02');
    submit(form);
    const request = http.expectOne(path + '/pricing-preview?start=2026-12-01&end=2026-12-02');
    request.flush([{ night: '2026-12-01', amount: 900, currency: 'MAD', source: 'custom', ruleId: 'rule' }]);
    await vi.waitFor(() => {
      fixture.detectChanges();
      expect(panel.querySelector('tbody')?.textContent).toContain('custom');
    });
  });

  async function details() {
    const fixture = TestBed.createComponent(PropertyDetails);
    await Promise.resolve();
    http.expectOne(path).flush(property());
    const amenities = await vi.waitFor(() => http.expectOne('/api/v1/organizations/org-1/properties/amenities'));
    amenities.flush([]);
    await fixture.whenStable();
    await vi.waitFor(() => {
      fixture.detectChanges();
      expect(fixture.componentInstance['busy']()).toBe(false);
    });
    fixture.nativeElement.querySelector('#tab-photos').click();
    fixture.detectChanges();
    return fixture;
  }

  function property() {
    const contract = { id: 'contract-1', startDate: '2026-11-01', endDate: null, rate: 25, status: 'active', version: 'version-1' };
    return {
      summary: { name: 'Example studio', status: 0, operationalStatus: 0, baseNightlyRate: 0 },
      photos: [], documents: [pdf], amenities: [], checkInTime: '15:00', checkOutTime: '11:00',
      createdAt: pdf.uploadedAt, updatedAt: pdf.uploadedAt, latitude: null, longitude: null,
      businessDate: '2026-11-26', defaultCommission: 20, currentCommission: 24,
      activeContract: contract, contracts: [contract], commissionRates: [], seasonalRates: [], listings: [], inventory: [],
    };
  }

  function setInput(form: HTMLFormElement, name: string, value: string) {
    const input = form.querySelector<HTMLInputElement>(`[formControlName="${name}"]`)!;
    input.value = value;
    input.dispatchEvent(new Event('input'));
  }

  function submit(form: HTMLFormElement) {
    form.dispatchEvent(new Event('submit', { cancelable: true }));
  }
});
