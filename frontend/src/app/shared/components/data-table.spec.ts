import { TestBed } from '@angular/core/testing';
import { provideTransloco } from '@jsverse/transloco';
import { UiTranslationLoader } from '../../core/config/ui-translations';
import { DataTable } from './data-table';
import { formatBusinessDate, formatMoney } from '../utils/formatters';

describe('shared table foundation', () => {
  beforeEach(() => TestBed.configureTestingModule({
    providers: [provideTransloco({ config: { defaultLang: 'en' }, loader: UiTranslationLoader })],
  }));

  it('emits sort intent without reordering the supplied server rows', () => {
    const fixture = TestBed.createComponent(DataTable<{ id: number; name: string }>);
    fixture.componentRef.setInput('columns', [{ key: 'name', label: 'Name', sortable: true, value: (row: { name: string }) => row.name }]);
    fixture.componentRef.setInput('rows', [{ id: 2, name: 'Second' }, { id: 1, name: 'First' }]);
    fixture.componentRef.setInput('trackRow', (row: { id: number }) => row.id);
    const emitted: unknown[] = [];
    fixture.componentInstance.sortChanged.subscribe(value => emitted.push(value));
    fixture.detectChanges();
    fixture.nativeElement.querySelector('th button').click();
    expect(emitted).toEqual([{ key: 'name', direction: 'asc' }]);
    expect(fixture.nativeElement.querySelector('tbody tr').textContent).toContain('Second');
  });

  it('formats MAD and handles Casablanca calendar boundaries', () => {
    expect(formatMoney(14000.25, 'fr-MA')).toContain('MAD');
    expect(formatMoney(null, 'ar-MA')).toBeNull();
    expect(formatBusinessDate('2026-11-25T23:30:00Z', 'en-GB')).toContain('26');
  });
});
