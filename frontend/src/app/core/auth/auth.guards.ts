import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';

export const authGuard: CanActivateFn = () =>
  inject(AuthService).user() ? true : inject(Router).parseUrl('/unauthorized');

export const mfaGuard: CanActivateFn = () =>
  inject(AuthService).pending() ? true : inject(Router).parseUrl('/login');

export const roleGuard: CanActivateFn = route => {
  const user = inject(AuthService).user();
  const router = inject(Router);
  const role = route.data['role'];
  const organization = route.paramMap.get('organizationId');
  const permitted = role === 'SuperAdmin' ? user?.superAdmin :
    user?.memberships.some(scope => scope.role === role && (!organization || scope.organizationId === organization));
  return permitted ? true : router.parseUrl(user ? '/forbidden' : '/unauthorized');
};

export const permissionGuard: CanActivateFn = route => {
  const user = inject(AuthService).user();
  const router = inject(Router);
  const organization = route.paramMap.get('organizationId');
  const member = user?.memberships.find(scope => scope.organizationId === organization);
  const permitted = organization && (user?.superAdmin || (route.data['write'] ? member?.role === 'Manager' : member));
  return permitted ? true : router.parseUrl(user ? '/forbidden' : '/unauthorized');
};

export const homeGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  return inject(Router).parseUrl(auth.user() ? auth.home() : '/login');
};
