import { InjectionToken } from '@angular/core';

export interface Branding {
  readonly name: string;
  readonly logo: string;
  readonly mark: string;
  readonly hero: string;
}

export const BRANDING = new InjectionToken<Branding>('HostGrid branding', {
  providedIn: 'root',
  factory: () => ({
    name: 'HostGrid',
    logo: '/brand/logo.svg',
    mark: '/brand/mark.svg',
    hero: '/brand/riad.jpg',
  }),
});
