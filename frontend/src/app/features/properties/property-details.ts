import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { ConfirmationDialog, ErrorState, LoadingSpinner, StatusBadge } from '../../shared/components/feedback';
import { CurrencyDisplay, FilePicker, SearchInput } from '../../shared/components/form-elements';
import { PageHeader } from '../../shared/components/page-elements';
import { formatBusinessDate } from '../../shared/utils/formatters';
import { Contract, FileGroup, Inventory, Listing, Option, PrivateFile, PropertyView, Season } from './portfolio-api';
import { PortfolioContext } from './portfolio-context';
import { PortfolioPage } from './portfolio-page';
import { OPERATING_STATES, PROPERTY_STATES } from './property-list';
import { PrivatePhoto } from './private-photo';

const tabs = ['overview', 'photos', 'contracts', 'inventory', 'pricing', 'documents', 'history'] as const;
type Tab = typeof tabs[number];

@Component({
  selector: 'hg-property-details', providers: [PortfolioContext],
  imports: [RouterLink, ReactiveFormsModule, ConfirmationDialog, ErrorState, LoadingSpinner, StatusBadge,
    CurrencyDisplay, FilePicker, SearchInput, PageHeader, PrivatePhoto],
  templateUrl: './property-details.html',
  styleUrl: './portfolio-page.scss',
})
export class PropertyDetails extends PortfolioPage {
  protected readonly id = this.route.snapshot.paramMap.get('id') ?? '';
  protected readonly data = signal<PropertyView | null>(null);
  protected readonly amenities = signal<Option[]>([]);
  protected readonly selectedAmenities = signal<ReadonlySet<string>>(new Set());
  protected readonly tab = signal<Tab>('overview');
  protected readonly tabs = tabs;
  protected readonly days = ['sun', 'mon', 'tue', 'wed', 'thu', 'fri', 'sat'];
  protected readonly kinds = ['weekend', 'seasonal', 'custom'];
  protected readonly channels = ['airbnb', 'booking', 'direct'];
  protected readonly states = PROPERTY_STATES;
  protected readonly operations = OPERATING_STATES;
  protected readonly inventoryQuery = signal('');
  protected readonly confirmOpen = signal(false);
  protected readonly confirmLabel = signal('remove');
  protected readonly uploading = signal(false);
  private queued: (() => void) | null = null;

  private readonly fb = inject(FormBuilder).nonNullable;
  protected readonly contractForm = this.fb.group({ startDate: ['', Validators.required], endDate: [''], rate: [''], documentId: [''] });
  protected readonly endForm = this.fb.group({ contractId: ['', Validators.required], endDate: ['', Validators.required] });
  protected readonly rateForm = this.fb.group({ effectiveFrom: ['', Validators.required], rate: [20, Validators.required] });
  protected readonly inventoryForm = this.fb.group({ name: ['', Validators.required], quantity: [1, Validators.required], condition: [''] });
  protected readonly seasonForm = this.fb.group({ startDate: ['', Validators.required], endDate: ['', Validators.required],
    nightlyRate: [0, Validators.required], priority: [3, Validators.required] });
  protected readonly listingForm = this.fb.group({ source: [0], url: [''], externalReference: [''] });
  protected readonly daysMask = signal(127);
  protected readonly inventoryEditing = signal<Inventory | null>(null);
  protected readonly seasonEditing = signal<Season | null>(null);

  constructor() {
    super();
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

  protected createContract(): void {
    if (this.contractForm.invalid) return this.invalid();
    const form = this.contractForm.getRawValue();
    this.write('contracts', { startDate: form.startDate, endDate: form.endDate || null,
      rate: form.rate === '' ? null : Number(form.rate), documentId: form.documentId || null }, 'post', () => this.contractForm.reset());
  }
  protected endContract(): void {
    const form = this.endForm.getRawValue();
    const contract = this.data()?.contracts.find(item => item.id === form.contractId);
    if (this.endForm.invalid || !contract) return this.invalid();
    this.write(`contracts/${contract.id}/end`, { endDate: form.endDate, version: contract.version }, 'put', () => this.endForm.reset());
  }
  protected createRate(): void {
    if (this.rateForm.invalid) return this.invalid();
    this.write('commission-rates', this.rateForm.getRawValue(), 'post', () => this.rateForm.reset());
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
    return items.filter(row => row.name.toLocaleLowerCase(this.ctx.locale).includes(term));
  }
  protected editInventory(row: Inventory): void {
    this.inventoryEditing.set(row);
    this.inventoryForm.setValue({ name: row.name, quantity: row.quantity, condition: row.condition ?? '' });
  }
  protected saveInventory(): void {
    if (this.inventoryForm.invalid) return this.invalid();
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
    if (this.seasonForm.invalid || !this.daysMask()) return this.invalid();
    const row = this.seasonEditing();
    this.write(row ? 'seasonal-rates/' + row.id : 'seasonal-rates',
      { ...this.seasonForm.getRawValue(), daysOfWeek: this.daysMask(), version: row?.version }, row ? 'put' : 'post',
      () => { this.seasonEditing.set(null); this.daysMask.set(127); this.seasonForm.reset(); });
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
    if (!files.length) return;
    void this.run(async () => {
      this.uploading.set(true);
      let failed = 0;
      try {
        for (const file of files) {
          try { await firstValueFrom(this.api.upload(this.organization, this.id, group, file)); }
          catch { failed++; }
        }
        await this.load();
        if (failed) this.problem.set(failed === files.length ? 'error' : 'partialUpload');
        else this.message.set('success');
      } finally { this.uploading.set(false); }
    });
  }
  protected download(file: PrivateFile): void {
    void this.run(async () => {
      try {
        const blob = await firstValueFrom(this.api.blob(this.organization, this.id, 'documents', file.id));
        const url = URL.createObjectURL(blob);
        const anchor = document.createElement('a');
        anchor.href = url;
        anchor.download = file.name;
        anchor.click();
        setTimeout(() => URL.revokeObjectURL(url), 5000);
      } catch { this.problem.set('fileUnavailable'); }
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
    this.selectedAmenities.set(new Set(data.amenities));
  }
}
