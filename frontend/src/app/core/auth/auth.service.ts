import { inject, Injectable, signal } from '@angular/core';
import { HttpClient, HttpContext, HttpContextToken } from '@angular/common/http';
import { catchError, defer, finalize, firstValueFrom, from, map, Observable, of, shareReplay, tap, throwError } from 'rxjs';

export const skipSessionRetry = new HttpContextToken(() => false);
const authUrl = '/api/v1/auth';

export interface Membership {
  organizationId: string;
  role: 'Manager' | 'Owner';
  ownerId: string | null;
}

export interface CurrentUser {
  id: string;
  email: string;
  superAdmin: boolean;
  mfaEnabled: boolean;
  memberships: Membership[];
}

export interface AuthResponse {
  status: 'authenticated' | 'mfa_required' | 'mfa_enrollment_required';
  accessToken?: string;
  user?: CurrentUser;
  challenge?: string;
  recoveryCodes?: string[];
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly userState = signal<CurrentUser | null>(null);
  private readonly accessState = signal<string | null>(null);
  private readonly pendingState = signal<AuthResponse | null>(null);
  private refreshRequest?: Observable<string | null>;

  readonly user = this.userState.asReadonly();
  readonly accessToken = this.accessState.asReadonly();
  readonly pending = this.pendingState.asReadonly();

  login(email: string, password: string) {
    return this.post<AuthResponse>('login', { email, password }).pipe(tap(result => this.accept(result)));
  }

  enrollment() {
    return this.post<{ setupKey: string }>('mfa/enrollment', { challenge: this.pending()?.challenge });
  }

  startMfa(password: string) {
    return this.post<AuthResponse>('mfa/start', { password }).pipe(tap(result => this.pendingState.set(result)));
  }

  verify(code: string, recovery: boolean) {
    return this.post<AuthResponse>('mfa/verify', { challenge: this.pending()?.challenge, code, recovery })
      .pipe(tap(result => this.accept(result)));
  }

  resetRequest(email: string) {
    return this.post<void>('password/reset-request', { email });
  }

  completePasswordLink(activation: boolean, email: string, token: string, password: string) {
    const action = activation ? 'activate' : 'password/reset';
    return this.post<void>(action, { email, token, password }).pipe(tap(() => this.clear()));
  }

  changePassword(currentPassword: string, password: string) {
    return this.post<void>('password/change', { currentPassword, password }).pipe(tap(() => this.clear()));
  }

  resetMfa(password: string, code: string, recovery: boolean) {
    return this.post<void>('mfa/reset', { password, code, recovery }).pipe(tap(() => this.clear()));
  }

  refresh(): Observable<string | null> {
    this.refreshRequest ??= defer(() => this.withSessionLock(() =>
      this.post<AuthResponse>('refresh', {}))).pipe(
      tap(result => this.accept(result)),
      map(result => result.accessToken ?? null),
      catchError(() => {
        this.clear();
        return of(null);
      }),
      finalize(() => { this.refreshRequest = undefined; }),
      shareReplay({ bufferSize: 1, refCount: false }),
    );
    return this.refreshRequest;
  }

  logout() {
    return defer(() => this.withSessionLock(() => this.post<void>('logout', {}))).pipe(
      tap(() => this.clear()),
      catchError(error => {
        this.clear();
        return throwError(() => error);
      }),
    );
  }

  home(): string {
    const user = this.user();
    return user?.superAdmin ? '/admin' : user?.memberships.some(scope => scope.role === 'Manager') ? '/manager' : '/owner';
  }

  private withSessionLock<T>(operation: () => Observable<T>): Observable<T> {
    const coordinated = async (): Promise<T> =>
      await navigator.locks.request('hostgrid-session', () => firstValueFrom(operation()));
    return typeof navigator !== 'undefined' && navigator.locks
      ? from(coordinated())
      : operation();
  }

  private post<T>(action: string, body: object) {
    return this.http.post<T>(`${authUrl}/${action}`, body, {
      context: new HttpContext().set(skipSessionRetry, true),
    });
  }

  private accept(result: AuthResponse) {
    this.accessState.set(result.accessToken ?? null);
    this.userState.set(result.user ?? null);
    this.pendingState.set(result.challenge ? result : null);
  }

  private clear(): void {
    this.accessState.set(null);
    this.userState.set(null);
    this.pendingState.set(null);
  }
}
