import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, convertToParamMap, provideRouter, RouterStateSnapshot } from '@angular/router';
import { authGuard, permissionGuard, roleGuard } from './auth.guards';
import { authInterceptor } from './auth.interceptor';
import { AuthService } from './auth.service';

const account = {
  id: 'user-1', email: 'owner@example.test', mfaEnabled: false, superAdmin: false,
  memberships: [{ organizationId: 'organization-1', role: 'Owner', ownerId: 'owner-1' }],
};
const session = (accessToken: string) => ({ status: 'authenticated', user: account, accessToken });

describe('authentication foundation', () => {
  let auth: AuthService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideRouter([]), provideHttpClient(withInterceptors([authInterceptor])), provideHttpClientTesting()],
    });
    auth = TestBed.inject(AuthService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('keeps the user and access token in memory and clears them on logout', () => {
    login();
    expect(auth.user()?.email).toBe(account.email);
    expect(auth.accessToken()).toBe('first');
    auth.logout().subscribe();
    http.expectOne('/api/v1/auth/logout').flush(null);
    expect(auth.user()).toBeNull();
    expect(auth.accessToken()).toBeNull();
  });

  it('keeps an MFA challenge separate from an authorized session', () => {
    auth.login(account.email, 'test').subscribe();
    http.expectOne('/api/v1/auth/login').flush({ status: 'mfa_required', challenge: 'pending' });
    expect(auth.pending()?.challenge).toBe('pending');
    expect(auth.accessToken()).toBeNull();
  });

  it('coalesces concurrent 401 responses into one refresh and retries with the new token', () => {
    login();
    const client = TestBed.inject(HttpClient);
    client.get('/api/one').subscribe();
    client.get('/api/two').subscribe();
    const one = http.expectOne('/api/one');
    const two = http.expectOne('/api/two');
    expect(one.request.headers.get('Authorization')).toBe('Bearer first');
    one.flush({}, { status: 401, statusText: 'Unauthorized' });
    two.flush({}, { status: 401, statusText: 'Unauthorized' });
    http.expectOne('/api/v1/auth/refresh').flush(session('second'));
    for (const path of ['/api/one', '/api/two']) {
      const retry = http.expectOne(path);
      expect(retry.request.headers.get('Authorization')).toBe('Bearer second');
      retry.flush({});
    }
  });

  it('denies unauthenticated and wrong-role navigation', () => {
    const route = {
      data: { role: 'Manager', write: true }, paramMap: convertToParamMap({ organizationId: 'organization-1' }),
    } as unknown as ActivatedRouteSnapshot;
    const state = {} as RouterStateSnapshot;
    expect(TestBed.runInInjectionContext(() => authGuard(route, state)).toString()).toBe('/unauthorized');
    login();
    expect(TestBed.runInInjectionContext(() => roleGuard(route, state)).toString()).toBe('/forbidden');
    expect(TestBed.runInInjectionContext(() => permissionGuard(route, state)).toString()).toBe('/forbidden');
  });

  function login(): void {
    auth.login(account.email, 'test').subscribe();
    const request = http.expectOne('/api/v1/auth/login');
    expect(request.request.headers.get('X-HostGrid-CSRF')).toBe('1');
    request.flush(session('first'));
  }
});
