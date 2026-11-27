import { addDates, businessToday, overlaps, validIsoDate } from './calendar-models';

describe('calendar business dates', () => {
  it('advances the 30-day window across year and leap-month boundaries', () => {
    expect(addDates('2031-12-15', 30)).toBe('2032-01-14');
    expect(addDates('2032-02-28', 1)).toBe('2032-02-29');
    expect(addDates('2032-02-29', 1)).toBe('2032-03-01');
    expect(validIsoDate('2031-02-29')).toBe(false);
  });

  it('uses Casablanca rather than the browser time zone for the initial date', () => {
    expect(businessToday(new Date('2026-11-26T23:30:00Z'))).toBe('2026-11-27');
  });

  it('keeps same-day turnover available and catches cases spanning the visible range', () => {
    const held = { startDate: '2032-02-27', endDate: '2032-03-01' };
    expect(overlaps(held, { startDate: '2032-02-28', endDate: '2032-02-29' })).toBe(true);
    expect(overlaps(held, { startDate: '2032-03-01', endDate: '2032-03-02' })).toBe(false);
    expect(overlaps(held, { startDate: '2032-02-26', endDate: '2032-02-27' })).toBe(false);
  });
});
