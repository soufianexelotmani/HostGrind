export interface Guest {
  id: string; name: string; email: string | null; phone: string | null; version: string;
  createdAt: string | null; updatedAt: string | null;
}
export interface Stay {
  id: string; reference: string; externalBookingId: string | null; propertyId: string; propertyName: string;
  guestId: string; guestName: string; source: number; checkInDate: string; checkOutDate: string; status: number;
  paymentStatus: number; netAmount: number;
}
export interface Night {
  night: string; amount: number; source: string; ruleId: string | null; manualOverride: boolean;
}
export interface Revision {
  number: number; checkInDate: string; checkOutDate: string; mode: number; netAmount: number;
  grossAmount: number | null; platformFees: number | null; createdAt: string; nights: Night[];
}
export interface Reservation {
  summary: Stay; adults: number; children: number; nightCount: number; mode: number; grossAmount: number | null;
  platformFees: number | null; commissionRate: number | null; commissionAmount: number | null;
  confirmationDate: string | null; confirmedAt: string | null; confirmedBy: string | null; notes: string | null;
  pricing: Revision | null; version: string; createdAt: string | null;
  historical: { reason: string; actualCheckInDate: string | null; actualCheckOutDate: string | null } | null;
}
export const sources = ['Airbnb', 'Booking.com', 'direct'] as const;
export const statuses = ['pending', 'confirmed', 'checkedIn', 'checkedOut', 'cancelled', 'noShow'] as const;
export const payments = ['unpaid', 'partiallyPaid', 'paid', 'partiallyRefunded', 'refunded'] as const;
