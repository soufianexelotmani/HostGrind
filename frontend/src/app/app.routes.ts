import { Routes } from '@angular/router';
import { authGuard, homeGuard, mfaGuard, roleGuard } from './core/auth/auth.guards';

const loadComponent = () => import('./features/auth/auth-page').then(module => module.AuthPage);
const dashboard = () => import('./features/dashboard/dashboard').then(module => module.Dashboard);
const propertyList = () => import('./features/properties/property-list').then(module => module.PropertyList);
const propertyForm = () => import('./features/properties/property-form').then(module => module.PropertyForm);
const propertyDetails = () => import('./features/properties/property-details').then(module => module.PropertyDetails);
const ownerList = () => import('./features/owners/owner-pages').then(module => module.OwnerList);
const ownerDetails = () => import('./features/owners/owner-pages').then(module => module.OwnerDetails);
const ownerForm = () => import('./features/owners/owner-pages').then(module => module.OwnerForm);

function portfolio(path: string, role: 'Manager' | 'Owner' | 'SuperAdmin'): Routes {
  const route = (suffix: string, loadComponent: NonNullable<Routes[number]['loadComponent']>) =>
    ({ path: path + '/' + suffix, canActivate: [roleGuard], data: { role }, loadComponent });
  const read = [route('properties', propertyList)];
  const detail = route('properties/:id', propertyDetails);
  if (role === 'Owner') return [...read, detail];
  return [...read, route('properties/new', propertyForm), route('properties/:id/edit', propertyForm), detail,
    route('owners', ownerList), route('owners/new', ownerForm), route('owners/:id/edit', ownerForm), route('owners/:id', ownerDetails)];
}

export const routes: Routes = [
  { path: '', pathMatch: 'full', canActivate: [homeGuard], loadComponent },
  { path: 'login', data: { mode: 'login' }, loadComponent },
  { path: 'forgot-password', data: { mode: 'forgot' }, loadComponent },
  { path: 'reset-password', data: { mode: 'reset' }, loadComponent },
  { path: 'activate', data: { mode: 'activate' }, loadComponent },
  { path: 'mfa', canActivate: [mfaGuard], data: { mode: 'mfa' }, loadComponent },
  { path: 'unauthorized', data: { mode: 'unauthorized' }, loadComponent },
  { path: 'forbidden', data: { mode: 'forbidden' }, loadComponent },
  {
    path: '', canActivate: [authGuard],
    loadComponent: () => import('./layouts/app-layout/app-layout').then(module => module.AppLayout),
    children: [
      { path: 'security', data: { mode: 'security' }, loadComponent },
      { path: 'admin', canActivate: [roleGuard], data: { role: 'SuperAdmin' }, loadComponent: dashboard },
      { path: 'manager', canActivate: [roleGuard], data: { role: 'Manager' }, loadComponent: dashboard },
      { path: 'owner', canActivate: [roleGuard], data: { role: 'Owner' }, loadComponent: dashboard },
      ...portfolio('admin', 'SuperAdmin'),
      ...portfolio('manager', 'Manager'),
      { path: 'manager', canActivate: [roleGuard], data: { role: 'Manager' },
        loadChildren: () => import('./features/bookings/booking.routes').then(module => module.bookingRoutes) },
      ...portfolio('owner', 'Owner'),
    ],
  },
  { path: '**', redirectTo: '' },
];
