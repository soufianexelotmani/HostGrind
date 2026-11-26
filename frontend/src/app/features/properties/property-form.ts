import { Component, inject, signal } from '@angular/core';
import { FormBuilder, FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { ErrorState, LoadingSpinner } from '../../shared/components/feedback';
import { PageHeader } from '../../shared/components/page-elements';
import { Option, Owner, Page, PropertyView } from './portfolio-api';
import { PortfolioContext } from './portfolio-context';
import { PortfolioPage } from './portfolio-page';

const decimal = (min: number, max: number) => [Validators.min(min), Validators.max(max)];
export function timeForApi(time: string): string { return time.length === 5 ? time + ':00' : time; }

@Component({
  selector: 'hg-property-form', providers: [PortfolioContext],
  imports: [RouterLink, ReactiveFormsModule, PageHeader, LoadingSpinner, ErrorState],
  templateUrl: './property-form.html',
  styleUrl: './portfolio-page.scss',
})
export class PropertyForm extends PortfolioPage {
  protected readonly id = this.route.snapshot.paramMap.get('id') ?? '';
  protected readonly types = signal<Option[]>([]);
  protected readonly owners = signal<Owner[]>([]);
  protected readonly ownerSearch = new FormControl('', { nonNullable: true });
  private version: string | null = null;
  protected readonly form = inject(FormBuilder).nonNullable.group({
    ownerId: ['', Validators.required], typeId: ['', Validators.required], name: ['', [Validators.required, Validators.maxLength(200)]],
    description: ['', Validators.maxLength(4000)], address: ['', [Validators.required, Validators.maxLength(500)]],
    city: ['', [Validators.required, Validators.maxLength(120)]],
    latitude: new FormControl<number | null>(null, decimal(-90, 90)),
    longitude: new FormControl<number | null>(null, decimal(-180, 180)),
    maxGuests: [2, [Validators.required, ...decimal(1, 32767)]],
    bedrooms: [1, [Validators.required, ...decimal(0, 32767)]],
    bathrooms: [1, [Validators.required, ...decimal(0, 32767)]],
    baseNightlyRate: [0, [Validators.required, Validators.min(0)]],
    checkInTime: ['15:00', Validators.required], checkOutTime: ['11:00', Validators.required],
  });

  constructor() {
    super();
    if (this.id) this.form.controls.ownerId.disable();
    void this.start(async () => {
      this.types.set(await firstValueFrom(this.api.get<Option[]>(this.organization, 'properties/types')));
      await this.loadOwners();
      if (!this.id) return;
      const property = await firstValueFrom(this.api.get<PropertyView>(this.organization, 'properties/' + this.id));
      const row = property.summary;
      if (!this.owners().some(owner => owner.id === row.ownerId)) {
        const owner = await firstValueFrom(this.api.get<{ owner: Owner }>(this.organization, 'owners/' + row.ownerId));
        this.owners.update(items => [...items, owner.owner]);
      }
      this.version = property.version;
      this.form.setValue({ ownerId: row.ownerId, typeId: row.typeId, name: row.name, city: row.city,
        description: property.description ?? '', address: property.address, latitude: property.latitude, longitude: property.longitude,
        maxGuests: property.maxGuests, bedrooms: property.bedrooms, bathrooms: property.bathrooms,
        baseNightlyRate: row.baseNightlyRate, checkInTime: property.checkInTime.slice(0, 5), checkOutTime: property.checkOutTime.slice(0, 5) });
    });
  }

  protected searchOwners(): void { void this.run(() => this.loadOwners()); }
  protected save(): void {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    void this.run(async () => {
      const values = this.form.getRawValue();
      const body = { ...values, version: this.version, checkInTime: timeForApi(values.checkInTime),
        checkOutTime: timeForApi(values.checkOutTime) };
      let id = this.id;
      if (id) await firstValueFrom(this.api.put(this.organization, 'properties/' + id, body));
      else id = (await firstValueFrom(this.api.post<{ id: string }>(this.organization, 'properties', body))).id;
      await this.router.navigate([this.ctx.link('properties', id)], { queryParams: this.query });
    });
  }
  private async loadOwners(): Promise<void> {
    const keep = this.owners().find(owner => owner.id === this.form.controls.ownerId.value);
    const response = await firstValueFrom(this.api.get<Page<Owner>>(this.organization, 'owners', {
      size: 100, archived: false, search: this.ownerSearch.value,
    }));
    this.owners.set(keep && !response.items.some(owner => owner.id === keep.id) ? [keep, ...response.items] : response.items);
  }
}
