import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { provideTransloco } from '@jsverse/transloco';
import { UiTranslationLoader } from '../../core/config/ui-translations';
import { Owner, PortfolioApi } from '../properties/portfolio-api';
import { PortfolioContext } from '../properties/portfolio-context';
import { PropertyForm } from '../properties/property-form';
import { OwnerDetails, OwnerForm } from './owner-pages';

const base = '/api/v1/organizations/org-1/owners';
const owner: Owner = {
  id: 'owner-1', reference: 'OWN-1', name: 'Fictitious owner', email: 'owner@example.test',
  phone: null, userId: 'user-1', isArchived: false, activated: true, accessEnabled: false, version: 'version-1',
};

describe('owner management acceptance', () => {
  let http: HttpTestingController;
  let manage: boolean;
  let id: string | null;

  beforeEach(() => {
    manage = true;
    id = null;
    const context = () => ({
      manage, organization: signal('org-1'), initialize: async () => {},
      label: (key: string) => key, link: (area: string, suffix: string) => '/manager/' + area + '/' + suffix,
    });
    const providers = [{ provide: PortfolioContext, useFactory: context }];
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([]),
        provideTransloco({ config: { defaultLang: 'en' }, loader: UiTranslationLoader }),
        { provide: ActivatedRoute, useFactory: () => ({ snapshot: { paramMap: convertToParamMap({ id }) } }) }],
    }).overrideComponent(OwnerDetails, { set: { providers } }).overrideComponent(OwnerForm, { set: { providers } })
      .overrideComponent(PropertyForm, { set: { providers } });
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('blocks an invalid owner form and sends the displayed rowversion on edit', async () => {
    id = owner.id;
    const fixture = TestBed.createComponent(OwnerForm);
    await Promise.resolve();
    http.expectOne(base + '/' + id).flush({ owner });
    await fixture.whenStable();
    const root: HTMLElement = fixture.nativeElement;
    const set = (field: string, value: string) => {
      const input = root.querySelector<HTMLInputElement>('input[formControlName="' + field + '"]')!;
      input.value = value;
      input.dispatchEvent(new Event('input'));
    };
    const submit = () => root.querySelector('form')!.dispatchEvent(new Event('submit', { cancelable: true }));
    set('email', 'invalid');
    submit();
    await fixture.whenStable();
    expect(root.querySelector('[role="alert"]')?.textContent).toContain('invalid');
    http.expectNone(base + '/' + id);
    set('email', 'changed@example.test');
    set('name', 'Updated owner');
    submit();
    const edit = http.expectOne({ method: 'PUT', url: base + '/' + id });
    expect(edit.request.body).toEqual({ name: 'Updated owner', email: 'changed@example.test', phone: '', version: 'version-1' });
    edit.flush({}, { status: 409, statusText: 'Conflict' });
    await fixture.whenStable();
    expect(root.querySelector('[role="alert"]')?.textContent).toContain('conflict');
  });

  it('keeps list filtering, sorting and pagination in the organization API request', () => {
    TestBed.inject(PortfolioApi).get('org-1', 'owners', {
      search: 'Fès', archived: false, sort: '-name', page: 2, size: 20, unused: '',
    }).subscribe();
    const list = http.expectOne(request => request.url === base);
    expect(list.request.method).toBe('GET');
    expect(list.request.params.keys().sort()).toEqual(['archived', 'page', 'search', 'size', 'sort']);
    expect(list.request.params.get('archived')).toBe('false');
    expect(list.request.params.get('page')).toBe('2');
    expect(list.request.params.get('search')).toBe('Fès');
    expect(list.request.params.get('sort')).toBe('-name');
    list.flush({ items: [], total: 0 });
  });

  it('requires an owner on the property form and submits the selected owner and type', async () => {
    const fixture = TestBed.createComponent(PropertyForm);
    await Promise.resolve();
    const properties = '/api/v1/organizations/org-1/properties';
    http.expectOne(properties + '/types').flush([{ id: 'studio-1', code: 'Studio' }]);
    await Promise.resolve();
    http.expectOne(base + '?size=100&archived=false').flush({ items: [owner], total: 1 });
    await fixture.whenStable();
    const root: HTMLElement = fixture.nativeElement;
    const set = (field: string, value: string) => {
      const control = root.querySelector<HTMLInputElement | HTMLSelectElement>('[formControlName="' + field + '"]')!;
      control.value = value;
      control.dispatchEvent(new Event(control instanceof HTMLSelectElement ? 'change' : 'input'));
    };
    const submit = () => root.querySelector('form')!.dispatchEvent(new Event('submit', { cancelable: true }));
    set('name', 'Test studio');
    set('city', 'Fes');
    set('address', 'Test address');
    set('typeId', 'studio-1');
    submit();
    await fixture.whenStable();
    expect(root.querySelector('[role="alert"]')?.textContent).toContain('invalid');
    http.expectNone(properties);
    set('ownerId', owner.id);
    submit();
    const create = http.expectOne({ method: 'POST', url: properties });
    expect(create.request.body).toMatchObject({
      ownerId: owner.id, typeId: 'studio-1', name: 'Test studio', checkInTime: '15:00:00', checkOutTime: '11:00:00',
    });
    create.flush({}, { status: 400, statusText: 'Bad Request' });
    await fixture.whenStable();
  });

  it('shows reactivation for Managers with disabled owner accounts', async () => {
    const root = await renderDetails();
    expect(actions(root)).toContain('enableAccess');
    expect(actions(root)).not.toContain('disable');
  });

  it('hides account and editing controls from read-only roles', async () => {
    manage = false;
    const root = await renderDetails();
    expect(actions(root)).not.toContain('enableAccess');
    expect(actions(root)).not.toContain('archive');
    expect(root.textContent).not.toContain('editOwner');
  });

  async function renderDetails(): Promise<HTMLElement> {
    id = owner.id;
    const fixture = TestBed.createComponent(OwnerDetails);
    await Promise.resolve();
    http.expectOne(base + '/' + id).flush({ owner, properties: [], contracts: [] });
    await fixture.whenStable();
    return fixture.nativeElement;
  }

  function actions(root: HTMLElement): string[] {
    return Array.from(root.querySelectorAll('.actions button'), button => button.textContent!.trim());
  }
});
