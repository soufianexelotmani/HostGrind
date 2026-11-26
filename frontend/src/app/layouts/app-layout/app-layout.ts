import { DOCUMENT } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { ReactiveFormsModule, FormControl } from '@angular/forms';
import { NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { TranslocoDirective, TranslocoPipe } from '@jsverse/transloco';
import { LucideBell as Bell, LucideChevronDown as ChevronDown, LucideEllipsis as Ellipsis, LucideLogOut as LogOut, LucideDynamicIcon, LucidePanelLeft as PanelLeft, LucideSearch as Search, LucideShield as Shield } from '@lucide/angular';
import { MessageService } from 'primeng/api';
import { Toast } from 'primeng/toast';
import { filter, firstValueFrom, map, startWith } from 'rxjs';
import { AuthService } from '../../core/auth/auth.service';
import { BRANDING } from '../../core/config/branding';
import { authorizedRoles, navigationFor, navigationSearch, WorkspaceRole } from '../../core/config/navigation';
import { Preferences } from '../../core/config/preferences';
import { navigationText } from '../../core/config/ui-translations';
import { Modal } from '../../shared/components/modal';
import { Avatar, Breadcrumbs } from '../../shared/components/page-elements';
import { PreferenceControls } from '../../shared/components/preference-controls';

@Component({
  selector: 'hg-app-layout',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, TranslocoDirective, TranslocoPipe, LucideDynamicIcon,
    Modal, PreferenceControls, ReactiveFormsModule, Avatar, Breadcrumbs, Toast],
  templateUrl: './app-layout.html',
  styleUrl: './app-layout.scss',
  host: { '(document:keydown)': 'shortcut($event)' },
})
export class AppLayout {
  protected readonly auth = inject(AuthService);
  protected readonly prefs = inject(Preferences);
  protected readonly brand = inject(BRANDING);
  private readonly messages = inject(MessageService);
  private readonly doc = inject(DOCUMENT);
  private readonly router = inject(Router);
  protected readonly path = toSignal(this.router.events.pipe(
    filter((event): event is NavigationEnd => event instanceof NavigationEnd),
    map(event => event.urlAfterRedirects.split(/[?#]/)[0])), { initialValue: this.router.url });
  protected readonly roles = computed(() => authorizedRoles(this.auth.user()));
  protected readonly role = computed<WorkspaceRole>(() => {
    const section = this.path().split('/')[1];
    const selected = section === 'admin' ? 'SuperAdmin' : section === 'owner' ? 'Owner' : section === 'manager' ? 'Manager' : undefined;
    return selected && this.roles().includes(selected) ? selected : this.roles()[0] ?? 'Owner';
  });
  protected readonly items = computed(() => navigationFor(this.role(), this.auth.user()));
  protected readonly crumbs = computed(() => {
    const lang = this.prefs.language();
    const entry = this.items().find(item => item.label !== 'dashboard' && item.route &&
      (this.path() === item.route || this.path().startsWith(item.route + '/'))) ?? this.items()[0];
    return [
      { label: navigationText(lang, 'workspace') },
      this.path() === '/security'
        ? { label: navigationText(lang, 'security') }
        : { label: navigationText(lang, entry?.label ?? 'dashboard') },
    ];
  });
  protected readonly query = new FormControl('', { nonNullable: true });
  private readonly queryValue = toSignal(this.query.valueChanges.pipe(startWith('')), { initialValue: '' });
  protected readonly results = computed(() => navigationSearch.find(this.items(), this.prefs.language(), this.queryValue()));
  protected readonly searchOpen = signal(false);
  protected readonly drawerOpen = signal(false);
  protected readonly noticesOpen = signal(false);
  protected readonly signingOut = signal(false);
  protected readonly searchIcon = Search;
  protected readonly panelIcon = PanelLeft;
  protected readonly bellIcon = Bell;
  protected readonly menuIcon = Ellipsis;
  protected readonly chevronIcon = ChevronDown;
  protected readonly shieldIcon = Shield;
  protected readonly logoutIcon = LogOut;

  protected shortcut(event: KeyboardEvent): void {
    const menu = this.doc.querySelector<HTMLDetailsElement>('.profile-menu[open]');
    if (event.key === 'Escape' && menu) {
      menu.open = false;
      menu.querySelector('summary')?.focus();
    }
    if (event.key.toLowerCase() === 'k' && (event.ctrlKey || event.metaKey)) {
      event.preventDefault();
      this.openSearch();
    }
  }

  protected openSearch(): void { this.query.setValue(''); this.searchOpen.set(true); }

  protected skip(event: Event): void {
    event.preventDefault();
    this.doc.getElementById('main-content')?.focus();
  }

  protected async signOut(): Promise<void> {
    if (this.signingOut()) return;
    this.signingOut.set(true);
    try {
      await firstValueFrom(this.auth.logout());
      await this.router.navigateByUrl('/login');
    } catch {
      this.messages.add({ severity: 'error', summary: navigationText(this.prefs.language(), 'error') });
    } finally { this.signingOut.set(false); }
  }
}
