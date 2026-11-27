import { LucideActivity as Activity, LucideBanknote as Banknote, LucideBell as Bell, LucideBuilding2 as Building2, LucideCalendarDays as CalendarDays, LucideChartNoAxesCombined as ChartNoAxesCombined, LucideClipboardCheck as ClipboardCheck, LucideClipboardList as ClipboardList, LucideFileText as FileText, LucideHouse as House, LucideKeyRound as KeyRound, LucideLayoutDashboard as LayoutDashboard, LucideListTodo as ListTodo, LucideReceipt as Receipt, LucideSettings as Settings, LucideShieldCheck as ShieldCheck, LucideSparkles as Sparkles, LucideUsers as Users, LucideWallet as Wallet, LucideWrench as Wrench } from '@lucide/angular';
import { CurrentUser } from '../auth/auth.service';
import { Language } from './preferences';
import { navigationText, UiKey } from './ui-translations';

export type WorkspaceRole = 'SuperAdmin' | 'Manager' | 'Owner';
import type { LucideIcon } from '@lucide/angular';
export interface NavigationItem {
  readonly label: UiKey;
  readonly icon: LucideIcon;
  readonly route?: string;
}

const menus: Record<WorkspaceRole, readonly NavigationItem[]> = {
  SuperAdmin: [
    { label: 'dashboard', icon: LayoutDashboard, route: '/admin' },
    { label: 'organizations', icon: Building2 }, { label: 'users', icon: Users },
    { label: 'managers', icon: KeyRound }, { label: 'owners', icon: Users, route: '/admin/owners' },
    { label: 'properties', icon: House, route: '/admin/properties' }, { label: 'globalFinance', icon: Wallet },
    { label: 'audit', icon: ShieldCheck }, { label: 'health', icon: Activity },
    { label: 'reports', icon: ChartNoAxesCombined }, { label: 'globalSettings', icon: Settings },
  ],
  Manager: [
    { label: 'dashboard', icon: LayoutDashboard, route: '/manager' },
    { label: 'properties', icon: Building2, route: '/manager/properties' }, { label: 'owners', icon: Users, route: '/manager/owners' },
    { label: 'guests', icon: Users, route: '/manager/guests' },
    { label: 'reservations', icon: ClipboardList, route: '/manager/reservations' }, { label: 'calendar', icon: CalendarDays },
    { label: 'finance', icon: Wallet }, { label: 'expenses', icon: Receipt },
    { label: 'payouts', icon: Banknote }, { label: 'cleaning', icon: Sparkles },
    { label: 'maintenance', icon: Wrench }, { label: 'tasks', icon: ListTodo },
    { label: 'requests', icon: Bell }, { label: 'reports', icon: FileText },
    { label: 'settings', icon: Settings, route: '/security' },
  ],
  Owner: [
    { label: 'dashboard', icon: LayoutDashboard, route: '/owner' },
    { label: 'myProperties', icon: House, route: '/owner/properties' }, { label: 'reservations', icon: ClipboardList },
    { label: 'calendar', icon: CalendarDays }, { label: 'myRevenue', icon: Wallet },
    { label: 'myExpenses', icon: Receipt }, { label: 'myPayouts', icon: Banknote },
    { label: 'interventions', icon: ClipboardCheck }, { label: 'documents', icon: FileText },
    { label: 'myRequests', icon: Bell }, { label: 'myReports', icon: ChartNoAxesCombined },
    { label: 'profile', icon: Settings, route: '/security' },
  ],
};

export function authorizedRoles(user: CurrentUser | null): WorkspaceRole[] {
  if (!user) return [];
  const scopes = new Set(user.memberships.map(member => member.role));
  return [
    ...(user.superAdmin ? ['SuperAdmin' as const] : []),
    ...(['Manager', 'Owner'] as const).filter(role => scopes.has(role)),
  ];
}

export function navigationFor(role: WorkspaceRole, user: CurrentUser | null): readonly NavigationItem[] {
  return authorizedRoles(user).includes(role) ? menus[role] : [];
}

export interface NavigationSearch {
  find(items: readonly NavigationItem[], language: Language, query: string): readonly NavigationItem[];
}

function normalize(value: string): string {
  return value.normalize('NFD').replace(/\p{Diacritic}/gu, '').toLowerCase().trim();
}

export const navigationSearch: NavigationSearch = {
  find: (items, language, query) => items.filter(item =>
    item.route && normalize(navigationText(language, item.label)).includes(normalize(query))),
};
