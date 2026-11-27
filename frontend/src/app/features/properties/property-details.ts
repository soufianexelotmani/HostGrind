import { HttpErrorResponse, HttpEventType } from '@angular/common/http';
import { Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { TranslocoPipe } from '@jsverse/transloco';
import { filter, firstValueFrom, tap } from 'rxjs';
import { ConfirmationDialog, ErrorState, LoadingSpinner, StatusBadge } from '../../shared/components/feedback';
import { CurrencyDisplay, FilePicker, SearchInput } from '../../shared/components/form-elements';
import { PageHeader } from '../../shared/components/page-elements';
import { formatBusinessDate } from '../../shared/utils/formatters';
import { Contract, FileGroup, Inventory, Listing, NightlyQuote, Option, PrivateFile, PropertyView, Season } from './portfolio-api';
import { PortfolioContext } from './portfolio-context';
import { PortfolioPage } from './portfolio-page';
import { OPERATING_STATES, PROPERTY_STATES } from './property-list';
import { PrivatePhoto } from './private-photo';

const ownerTabs = ['overview', 'photos', 'contracts', 'inventory', 'pricing', 'documents'] as const;
type Tab = typeof ownerTabs[number] | 'history';
const percentValidators = [Validators.min(0), Validators.max(100), Validators.pattern(/^\d+(\.\d{1,2})?$/)];
const moneyValidators = [Validators.required, Validators.min(0), Validators.pattern(/^\d{1,16}(\.\d{1,2})?$/)];

@Component({
  selector: 'hg-property-details', providers: [PortfolioContext],
  imports: [RouterLink, ReactiveFormsModule, ConfirmationDialog, ErrorState, LoadingSpinner, StatusBadge,
    CurrencyDisplay, FilePicker, SearchInput, PageHeader, PrivatePhoto, TranslocoPipe],
  templateUrl: './property-details.html',
  styleUrl: './portfolio-page.scss',
})
export class PropertyDetails extends PortfolioPage {
  protected readonly id = this.route.snapshot.paramMap.get('id') ?? '';
  protected readonly data = signal<PropertyView | null>(null);
  protected readonly amenities = signal<Option[]>([]);
  protected readonly selectedAmenities = signal<ReadonlySet<string>>(new Set());
  protected readonly tab = signal<Tab>('overview');
  protected readonly tabs: readonly Tab[] = this.ctx.manage ? [...ownerTabs, 'history'] : ownerTabs;
  protected readonly days = ['sun', 'mon', 'tue', 'wed', 'thu', 'fri', 'sat'];
  protected readonly kinds = ['weekend', 'seasonal', 'custom'];
  protected readonly channels = ['airbnb', 'booking', 'direct'];
  protected readonly states = PROPERTY_STATES;
  protected readonly operations = OPERATING_STATES;
  protected readonly inventoryQuery = signal('');
  protected readonly confirmOpen = signal(false);
  protected readonly confirmLabel = signal('remove');
  protected readonly uploadProgress = signal<{ current: number; count: number; percent: number | null } | null>(null);
  private readonly destroy = inject(DestroyRef);
  private readonly downloads = new Map<string, ReturnType<typeof setTimeout>>();
  private queued: (() => void) | null = null;

  private readonly fb = inject(FormBuilder).nonNullable;
  protected readonly contractEditing = signal<Contract | null>(null);
  protected readonly contractForm = this.fb.group({
    startDate: ['', Validators.required], endDate: [''], rate: new FormControl<number | null>(null, percentValidators), documentId: [''],
  });
  protected readonly endForm = this.fb.group({ contractId: ['', Validators.required], endDate: ['', Validators.required] });
  protected readonly rateForm = this.fb.group({ effectiveFrom: ['', Validators.required], rate: [20, [Validators.required, ...percentValidators]] });
  protected readonly inventoryForm = this.fb.group({
    name: ['', [Validators.required, Validators.maxLength(200)]],
    quantity: [1, [Validators.required, Validators.min(0), Validators.max(2147483647), Validators.pattern(/^\d+$/)]],
    condition: ['', Validators.maxLength(120)], notes: ['', Validators.maxLength(1000)],
  });
  protected readonly baseRateForm = this.fb.group({ nightlyRate: [0, moneyValidators] });
  protected readonly previewForm = this.fb.group({ start: ['', Validators.required], end: ['', Validators.required] });
  protected readonly quotes = signal<NightlyQuote[] | null>(null);
  protected readonly seasonForm = this.fb.group({ startDate: ['', Validators.required], endDate: ['', Validators.required],
    nightlyRate: [0, moneyValidators], priority: [3, Validators.required] });
  protected readonly listingForm = this.fb.group({ source: [0], url: [''], externalReference: [''] });
  protected readonly daysMask = signal(127);
  protected readonly inventoryEditing = signal<Inventory | null>(null);
  protected readonly seasonEditing = signal<Season | null>(null);

  constructor() {
    super();
    this.destroy.onDestroy(() => {
      for (const [url, timer] of this.downloads) {
        clearTimeout(timer);
        URL.revokeObjectURL(url);
      }
      this.downloads.clear();
    });
    void this.start(async () => {
      await this.load();
      this.amenities.set(await firstValueFrom(this.api.get<Option[]>(this.organization, 'properties/amenities')));
    });
  }
  protected get editable(): boolean { return this.ctx.manage && this.data()?.summary.status !== 2; }
  protected date(value: string): string { return formatBusinessDate(value, this.ctx.locale); }
  protected labelAction(value: string): string { return this.ctx.label('action_' + value); }

  protected selectTab(key: Tab): void { this.tab.set(key); }
  protected keys(event: KeyboardEvent): void {
    const rtl = document.documentElement.dir === 'rtl';
    const direction = event.key === 'ArrowRight' ? (rtl ? -1 : 1) : event.key === 'ArrowLeft' ? (rtl ? 1 : -1) : 0;
    const index = this.tabs.indexOf(this.tab());
    const target = event.key === 'Home' ? 0 : event.key === 'End' ? this.tabs.length - 1 :
      direction ? (index + direction + this.tabs.length) % this.tabs.length : index;
    if (target === index) return;
    event.preventDefault();
    const tab = this.tabs[target];
    this.tab.set(tab);
    (event.currentTarget as HTMLElement).querySelector<HTMLElement>('#tab-' + tab)?.focus();
  }

  protected status(status: number): void {
    this.ask(status === 2 ? 'archive' : status === 0 ? 'reactivate' : 'deactivate',
      () => this.write('status', { version: this.data()?.version, status }, 'put'));
  }

  protected editContract(contract: Contract): void {
    this.contractEditing.set(contract);
    this.contractForm.setValue({
      startDate: contract.startDate, endDate: contract.endDate ?? '', rate: contract.rate, documentId: contract.documentId ?? '',
    });
    if (contract.documentId) this.contractForm.controls.documentId.disable();
    else this.contractForm.controls.documentId.enable();
  }

  protected clearContract(): void {
    this.contractEditing.set(null);
    this.contractForm.controls.documentId.enable();
    this.contractForm.reset();
  }

  protected saveContract(): void {
    const form = this.contractForm.getRawValue();
    const contract = this.contractEditing();
    if (this.contractForm.invalid || form.endDate && form.endDate <= form.startDate ||
        contract && form.startDate <= (this.data()?.businessDate ?? '')) return this.invalid();
    this.write(contract ? 'contracts/' + contract.id : 'contracts', {
      startDate: form.startDate, endDate: form.endDate || null, rate: form.rate,
      documentId: form.documentId || null, version: contract?.version,
    }, contract ? 'put' : 'post', () => this.clearContract());
  }
  protected endContract(): void {
    const form = this.endForm.getRawValue();
    const contract = this.data()?.contracts.find(item => item.id === form.contractId);
    if (this.endForm.invalid || !contract || form.endDate <= contract.startDate ||
        form.endDate <= (this.data()?.businessDate ?? '')) return this.invalid();
    this.ask('confirmContractEnd', () =>
      this.write(`contracts/${contract.id}/end`, { endDate: form.endDate, version: contract.version }, 'put', () => this.endForm.reset()));
  }
  protected archiveContract(contract: Contract): void {
    this.ask('confirmContractArchive', () => this.write(`contracts/${contract.id}/archive`, { version: contract.version }, 'post'));
  }

  protected createRate(): void {
    const form = this.rateForm.getRawValue();
    if (this.rateForm.invalid || form.effectiveFrom < (this.data()?.businessDate ?? '')) return this.invalid();
    this.ask('commissionWarning', () => this.write('commission-rates', form, 'post', () => this.rateForm.reset()));
  }

  protected toggleAmenity(id: string, event: Event): void {
    const checked = (event.target as HTMLInputElement).checked;
    this.selectedAmenities.update(previous => {
      const copy = new Set(previous);
      if (checked) copy.add(id); else copy.delete(id);
      return copy;
    });
  }
  protected saveAmenities(): void {
    this.write('amenities', { ids: [...this.selectedAmenities()], version: this.data()?.version }, 'put');
  }
  protected inventoryRows(items: Inventory[]): Inventory[] {
    const term = this.inventoryQuery().trim().toLocaleLowerCase(this.ctx.locale);
    return items.filter(row => [row.name, row.condition, row.notes]
      .some(value => value?.toLocaleLowerCase(this.ctx.locale).includes(term)));
  }
  protected editInventory(row: Inventory): void {
    this.inventoryEditing.set(row);
    this.inventoryForm.setValue({ name: row.name, quantity: row.quantity, condition: row.condition ?? '', notes: row.notes ?? '' });
  }
  protected saveInventory(): void {
    if (this.inventoryForm.invalid || !this.inventoryForm.controls.name.value.trim()) return this.invalid();
    const row = this.inventoryEditing();
    this.write(row ? 'inventory/' + row.id : 'inventory',
      { ...this.inventoryForm.getRawValue(), version: row?.version }, row ? 'put' : 'post',
      () => { this.inventoryEditing.set(null); this.inventoryForm.reset(); });
  }
  protected removeInventory(row: Inventory): void {
    this.ask('remove', () => this.write('inventory/' + row.id, { version: row.version }, 'delete'));
  }

  protected setDays(index: number, event: Event): void {
    const checked = (event.target as HTMLInputElement).checked;
    this.daysMask.update(value => checked ? value | (1 << index) : value & ~(1 << index));
  }
  protected checkedDay(index: number): boolean { return (this.daysMask() & (1 << index)) !== 0; }
  protected choosePriority(event: Event): void { this.daysMask.set((event.target as HTMLSelectElement).selectedIndex === 0 ? 65 : 127); }
  protected editSeason(rate: Season): void {
    this.seasonEditing.set(rate);
    this.daysMask.set(rate.daysOfWeek);
    this.seasonForm.setValue({ startDate: rate.startDate, endDate: rate.endDate, nightlyRate: rate.nightlyRate, priority: rate.priority });
  }
  protected saveSeason(): void {
    const { startDate, endDate } = this.seasonForm.getRawValue();
    if (this.seasonForm.invalid || !this.daysMask() || endDate <= startDate ||
        startDate < (this.data()?.businessDate ?? '')) return this.invalid();
    const row = this.seasonEditing();
    this.write(row ? 'seasonal-rates/' + row.id : 'seasonal-rates',
      { ...this.seasonForm.getRawValue(), daysOfWeek: this.daysMask(), version: row?.version }, row ? 'put' : 'post',
      () => { this.seasonEditing.set(null); this.daysMask.set(127); this.seasonForm.reset(); });
  }

  protected saveBaseRate(): void {
    if (this.baseRateForm.invalid) return this.invalid();
    const body = { ...this.baseRateForm.getRawValue(), version: this.data()?.version };
    this.ask('confirmBaseRate', () => this.write('base-rate', body, 'put'));
  }

  protected previewRates(): void {
    const range = this.previewForm.getRawValue();
    if (this.previewForm.invalid || range.end <= range.start ||
        Date.parse(range.end) - Date.parse(range.start) > 366 * 86400000) return this.invalid();
    void this.run(async () => {
      this.quotes.set(null);
      this.quotes.set(await firstValueFrom(this.api.get<NightlyQuote[]>(this.organization,
        `properties/${this.id}/pricing-preview`, range)));
    });
  }
  protected loadListing(event: Event): void {
    const source = (event.target as HTMLSelectElement).selectedIndex;
    const listing = this.data()?.listings.find(row => row.source === source);
    this.listingForm.setValue({ source, url: listing?.url ?? '', externalReference: listing?.externalReference ?? '' });
  }
  protected editListing(row: Listing): void {
    this.listingForm.setValue({ source: row.source, url: row.url ?? '', externalReference: row.externalReference ?? '' });
  }
  protected saveListing(): void {
    const values = this.listingForm.getRawValue();
    const current = this.data()?.listings.find(row => row.source === values.source);
    this.write('listings/' + values.source, { url: values.url, externalReference: values.externalReference, version: current?.version }, 'put');
  }

  protected upload(group: FileGroup, files: readonly File[]): void {
    if (!this.editable || !files.length) return;
    void this.run(async () => {
      let failed = 0;
      let failureLabel = 'error';
      try {
        for (const [index, file] of files.entries()) {
          if (this.destroy.destroyed) return;
          const progress = { current: index + 1, count: files.length, percent: null };
          this.uploadProgress.set(progress);
          try {
            await firstValueFrom(this.api.upload(this.organization, this.id, group, file).pipe(
              tap(event => {
                if (event.type === HttpEventType.UploadProgress && event.total)
                  this.uploadProgress.set({ ...progress, percent: Math.floor(100 * event.loaded / event.total) });
              }),
              filter(event => event.type === HttpEventType.Response),
              takeUntilDestroyed(this.destroy),
            ));
          } catch (error) {
            failed++;
            failureLabel = error instanceof HttpErrorResponse && [413, 415, 422].includes(error.status)
              ? 'invalidFiles' : 'error';
          }
        }
        if (this.destroy.destroyed) return;
        await this.load();
        if (failed) this.problem.set(failed === files.length ? failureLabel : 'partialUpload');
        else this.message.set('success');
      } finally { this.uploadProgress.set(null); }
    });
  }
  protected download(file: PrivateFile): void {
    void this.run(async () => {
      try {
        const blob = await firstValueFrom(this.api.blob(this.organization, this.id, 'documents', file.id)
          .pipe(takeUntilDestroyed(this.destroy)));
        const url = URL.createObjectURL(blob);
        const anchor = document.createElement('a');
        anchor.href = url;
        anchor.download = file.name;
        anchor.click();
        this.downloads.set(url, setTimeout(() => {
          URL.revokeObjectURL(url);
          this.downloads.delete(url);
        }, 5000));
      } catch { if (!this.destroy.destroyed) this.problem.set('fileUnavailable'); }
    });
  }
  protected contractFile(contract: Contract): void {
    const file = this.data()?.documents.find(row => row.id === contract.documentId);
    if (file) this.download(file);
  }
  protected removeFile(group: FileGroup, file: PrivateFile): void {
    this.ask('remove', () => this.write(group + '/' + file.id, {}, 'delete'));
  }
  protected orderPhoto(id: string, cover: boolean): void {
    const photoIds = this.data()?.photos.map(file => file.id) ?? [];
    const index = photoIds.indexOf(id);
    if (index <= 0) return;
    photoIds.splice(index, 1);
    photoIds.splice(cover ? 0 : index - 1, 0, id);
    this.write('photos/order', { ids: photoIds, version: this.data()?.version }, 'put');
  }

  protected confirmed(): void { this.queued?.(); this.queued = null; }
  private ask(label: string, action: () => void): void {
    this.confirmLabel.set(label); this.queued = action; this.confirmOpen.set(true);
  }
  private invalid(): void { this.problem.set('invalid'); }
  private write(path: string, body: object, method: 'post' | 'put' | 'delete', after?: () => void): void {
    void this.run(async () => {
      const suffix = `properties/${this.id}/${path}`;
      if (method === 'post') await firstValueFrom(this.api.post(this.organization, suffix, body));
      else if (method === 'put') await firstValueFrom(this.api.put(this.organization, suffix, body));
      else await firstValueFrom(this.api.remove(this.organization, suffix, body));
      await this.load();
      after?.();
    }, true);
  }
  private async load(): Promise<void> {
    const data = await firstValueFrom(this.api.get<PropertyView>(this.organization, 'properties/' + this.id));
    this.data.set(data);
    this.baseRateForm.reset({ nightlyRate: data.summary.baseNightlyRate });
    this.quotes.set(null);
    this.selectedAmenities.set(new Set(data.amenities));
  }
}
