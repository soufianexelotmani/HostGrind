import { Page } from '../properties/portfolio-api';
import { BUSINESS_TIME_ZONE } from '../../shared/utils/formatters';

export interface CalendarProperty {
  id: string; reference: string; name: string; type: string; ownerName: string | null; status: number; operationalStatus: number;
}
export interface CalendarHold { propertyId: string; startDate: string; endDate: string }
export interface CalendarReservation {
  id: string; propertyId: string; reference: string; source: number; status: number; paymentStatus: number | null;
  checkInDate: string; checkOutDate: string; effectiveStart: string | null; effectiveEnd: string | null;
  guestName: string | null; canViewDetails: boolean; canEdit: boolean;
}
export interface CalendarBlock extends CalendarHold {
  id: string; reason: number | null; legacyReason: string | null; description: string | null; status: number;
  availabilityReleasedFrom: string | null; version: string | null; canEdit: boolean; canCancel: boolean;
}
export interface CalendarView {
  startDate: string; endDate: string; businessDate: string; properties: Page<CalendarProperty>;
  reservations: CalendarReservation[]; blocks: CalendarBlock[];
  reservationHolds: CalendarHold[]; blockHolds: CalendarHold[];
}
export interface CalendarSummary {
  scheduledArrivals: number; scheduledDepartures: number; occupiedProperties: number;
  availableProperties: number; blockedProperties: number; pendingHeldProperties: number;
}
export interface CalendarEvent extends CalendarHold {
  id: string; title: string; tone: string; reservation?: CalendarReservation; block?: CalendarBlock;
}
export interface DateWindow { startDate: string; endDate: string }
export const calendarReasons = ['maintenance', 'ownerUse', 'unavailability', 'renovation', 'other'] as const;
export const indicators = ['scheduledArrivals', 'scheduledDepartures', 'occupiedProperties', 'availableProperties',
  'blockedProperties', 'pendingHeldProperties'] as const satisfies (keyof CalendarSummary)[];

export function businessToday(now = new Date()): string {
  const parts = new Intl.DateTimeFormat('en', { timeZone: BUSINESS_TIME_ZONE, year: 'numeric', month: '2-digit', day: '2-digit' })
    .formatToParts(now);
  return ['year', 'month', 'day'].map(type => parts.find(part => part.type === type)!.value).join('-');
}

export function addDates(day: string, days: number): string {
  const date = new Date(day + 'T00:00:00Z');
  date.setUTCDate(date.getUTCDate() + days);
  return date.toISOString().slice(0, 10);
}

export function validIsoDate(day: string | null): day is string {
  return !!day && /^\d{4}-\d{2}-\d{2}$/.test(day) && Number.isFinite(Date.parse(day)) && addDates(day, 0) === day;
}

export function overlaps(left: DateWindow, right: DateWindow): boolean {
  return left.startDate < right.endDate && left.endDate > right.startDate;
}
