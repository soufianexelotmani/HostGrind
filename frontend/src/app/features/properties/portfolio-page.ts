import { HttpErrorResponse } from '@angular/common/http';
import { inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { PortfolioApi } from './portfolio-api';
import { PortfolioContext } from './portfolio-context';

export abstract class PortfolioPage {
  protected readonly ctx = inject(PortfolioContext);
  protected readonly api = inject(PortfolioApi);
  protected readonly route = inject(ActivatedRoute);
  protected readonly router = inject(Router);
  protected readonly ready = signal(false);
  protected readonly busy = signal(false);
  protected readonly problem = signal('');
  protected readonly message = signal('');
  protected get organization(): string { return this.ctx.organization(); }
  protected get query() { return { organization: this.organization }; }

  protected async start(work: () => Promise<void>): Promise<void> {
    await this.run(async () => {
      await this.ctx.initialize();
      this.ready.set(true);
      if (this.organization) await work();
    });
  }

  protected async run(work: () => Promise<void>, success = false): Promise<void> {
    if (this.busy()) return;
    this.busy.set(true);
    this.problem.set('');
    this.message.set('');
    try {
      await work();
      if (success && !this.message()) this.message.set('success');
    } catch (error) {
      this.problem.set(error instanceof HttpErrorResponse
        ? ({ 400: 'invalid', 404: 'missing', 409: 'conflict' } as Record<number, string>)[error.status] ?? 'error'
        : 'error');
    } finally { this.busy.set(false); }
  }

  protected setOrganization(event: Event, reload: () => Promise<void>): void {
    const organization = (event.target as HTMLSelectElement).value;
    this.ctx.organization.set(organization);
    void this.run(reload);
  }
}
