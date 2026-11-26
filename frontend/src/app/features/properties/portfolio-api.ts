import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';

export interface Page<T> { items: T[]; pageNumber: number; pageSize: number; total: number }
export interface Option { id: string; code: string }
export interface Organization { id: string; name: string; isActive: boolean }
export interface Owner { id: string; reference: string; name: string; email: string | null; phone: string | null;
  userId: string | null; isArchived: boolean; accessEnabled: boolean; activated: boolean; version: string }
export interface OwnerPortfolio { owner: Owner; properties: PropertySummary[]; contracts: Contract[] }
export interface PropertySummary { id: string; reference: string; name: string; city: string; ownerId: string; ownerName: string;
  typeId: string; type: string; status: number; operationalStatus: number; baseNightlyRate: number; coverPhotoId: string | null }
export interface Contract { id: string; propertyId: string; startDate: string; endDate: string | null; rate: number;
  status: string; documentName: string | null; documentId: string | null; version: string }
export interface Commission { id: string; effectiveFrom: string; effectiveTo: string | null; rate: number }
export interface Inventory { id: string; name: string; quantity: number; condition: string | null; version: string }
export interface Season { id: string; startDate: string; endDate: string; daysOfWeek: number; nightlyRate: number; priority: number; version: string }
export interface Listing { id: string; source: number; url: string | null; externalReference: string | null; version: string }
export interface PrivateFile { id: string; name: string; contentType: string; sizeBytes: number; uploadedAt: string;
  uploaderId: string; scanStatus: number; sortOrder: number | null }
export interface PropertyView {
  summary: PropertySummary; description: string | null; address: string; latitude: number | null; longitude: number | null;
  maxGuests: number; bedrooms: number; bathrooms: number; checkInTime: string; checkOutTime: string;
  createdAt: string; updatedAt: string; version: string; defaultCommission: number;
  contracts: Contract[]; commissionRates: Commission[]; inventory: Inventory[]; amenities: string[];
  seasonalRates: Season[]; listings: Listing[]; photos: PrivateFile[]; documents: PrivateFile[];
  history: { action: string; occurredAt: string }[];
}
export type FileGroup = 'photos' | 'documents';
type Query = Record<string, string | number | boolean | null | undefined>;

@Injectable({ providedIn: 'root' })
export class PortfolioApi {
  private readonly http = inject(HttpClient);

  path(org: string, suffix: string): string {
    return `/api/v1/organizations/${encodeURIComponent(org)}/${suffix}`;
  }
  organizations(page: number) {
    return this.http.get<Organization[]>('/api/v1/admin/organizations', { params: { page } });
  }
  get<T>(org: string, suffix: string, query: Query = {}) {
    const params = new HttpParams({ fromObject: Object.fromEntries(Object.entries(query)
      .filter(([, value]) => value !== '' && value !== null && value !== undefined)
      .map(([key, value]) => [key, String(value)])) });
    return this.http.get<T>(this.path(org, suffix), { params });
  }
  post<T>(org: string, suffix: string, body: object) {
    return this.http.post<T>(this.path(org, suffix), body);
  }
  put(org: string, suffix: string, body: object) {
    return this.http.put<void>(this.path(org, suffix), body);
  }
  remove(org: string, suffix: string, body?: object) {
    return this.http.delete<void>(this.path(org, suffix), { body });
  }
  upload(org: string, property: string, group: FileGroup, file: File) {
    const body = new FormData();
    body.append('file', file);
    return this.http.post<PrivateFile>(this.path(org, `properties/${property}/${group}`), body);
  }
  blob(org: string, property: string, group: FileGroup, file: string) {
    return this.http.get(this.path(org, `properties/${property}/${group}/${file}`), { responseType: 'blob' });
  }
}
