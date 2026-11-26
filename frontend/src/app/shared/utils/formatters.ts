export const BUSINESS_TIME_ZONE = 'Africa/Casablanca';

export function formatMoney(amount: number | null, locale: string): string | null {
  return amount === null || !Number.isFinite(amount) ? null : new Intl.NumberFormat(locale, {
    style: 'currency', currency: 'MAD', currencyDisplay: 'code',
  }).format(amount);
}

export function formatBusinessDate(instant: string | Date, locale: string): string {
  return new Intl.DateTimeFormat(locale, { dateStyle: 'medium', timeZone: BUSINESS_TIME_ZONE })
    .format(new Date(instant));
}

export function formatNumber(value: number, locale: string): string {
  return new Intl.NumberFormat(locale).format(value);
}
