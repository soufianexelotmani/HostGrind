import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, of, switchMap, throwError } from 'rxjs';
import { AuthService, skipSessionRetry } from './auth.service';

export const authInterceptor: HttpInterceptorFn = (request, next) => {
  if (!request.url.startsWith('/api/')) return next(request);

  const session = inject(AuthService);
  const initialToken = session.accessToken();
  const headers = ['GET', 'HEAD'].includes(request.method) ? request.headers : request.headers.set('X-HostGrid-CSRF', '1');
  const send = (token: string | null) => next(request.clone({
    headers: token ? headers.set('Authorization', `Bearer ${token}`) : headers,
  }));
  return send(initialToken).pipe(catchError(error => {
    if (!(error instanceof HttpErrorResponse) || error.status !== 401 ||
        request.context.get(skipSessionRetry) || !initialToken) {
      return throwError(() => error);
    }
    const current = session.accessToken();
    return (current && current !== initialToken ? of(current) : session.refresh()).pipe(
      switchMap(token => token ? send(token) : throwError(() => error)),
    );
  }));
};
