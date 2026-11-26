import { Component, inject } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { TranslocoDirective } from '@jsverse/transloco';
import { LucideDynamicIcon, LucideShieldCheck as ShieldCheck } from '@lucide/angular';
import { ButtonModule } from 'primeng/button';
import { WorkspaceRole } from '../../core/config/navigation';
import { EmptyState, PageHeader } from '../../shared/components/page-elements';

@Component({
  selector: 'hg-dashboard',
  imports: [TranslocoDirective, RouterLink, EmptyState, PageHeader, LucideDynamicIcon, ButtonModule],
  template: `
    <div class="dashboard" *transloco="let t; prefix: 'ui'">
      <section class="dashboard-intro">
        <hg-page-header [title]="t(role + 'Title')" [description]="t(role + 'Description')" />
      </section>
      <div class="dashboard-grid">
        <section class="dashboard-panel">
          <h2>{{ t('activity') }}</h2>
          <hg-empty-state [title]="t('empty')" [description]="t('noActivity')" />
        </section>
        <section class="dashboard-panel security-panel">
          <h2>{{ t('quickLinks') }}</h2>
          <div class="empty-icon"><svg [lucideIcon]="shield" [size]="22" aria-hidden="true"></svg></div>
          <p>{{ t('securityHint') }}</p>
          <a pButton routerLink="/security"><span pButtonLabel>{{ t('security') }}</span></a>
        </section>
      </div>
    </div>`,
})
export class Dashboard {
  private readonly roleData = inject(ActivatedRoute).snapshot.data['role'] as WorkspaceRole;
  protected readonly role = this.roleData === 'SuperAdmin' ? 'admin' : this.roleData === 'Manager' ? 'manager' : 'owner';
  protected readonly shield = ShieldCheck;
}
