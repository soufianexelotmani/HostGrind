import { Routes } from '@angular/router';

const role = { role: 'Manager' };
const guests = () => import('./guest-page').then(module => module.GuestPage);
const reservations = () => import('./reservation-list').then(module => module.ReservationList);
const form = () => import('./reservation-form').then(module => module.ReservationForm);
export const bookingRoutes: Routes = [
  { path: 'guests', data: role, loadComponent: guests },
  { path: 'guests/new', data: { ...role, edit: true }, loadComponent: guests },
  { path: 'guests/:id/edit', data: { ...role, edit: true }, loadComponent: guests },
  { path: 'guests/:id', data: role, loadComponent: guests },
  { path: 'reservations', data: role, loadComponent: reservations },
  { path: 'reservations/new', data: role, loadComponent: form },
  { path: 'reservations/historical', data: { ...role, historical: true }, loadComponent: form },
  { path: 'reservations/:id/edit', data: role, loadComponent: form },
  { path: 'reservations/:id', data: { ...role, read: true }, loadComponent: form },
];
