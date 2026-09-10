import { CommonModule } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { NavigationEnd, Router, RouterLink, RouterOutlet } from '@angular/router';
import { filter } from 'rxjs/operators';
import { AuthService } from './core/auth/auth.service';
import { InstallPromptComponent } from './shared/components/install-prompt/install-prompt.component';

type NavItem = {
  label: string;
  path: string;
  icon: string;
  roles?: string[];
};

@Component({
  selector: 'app-root',
  imports: [CommonModule, RouterOutlet, RouterLink, InstallPromptComponent],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss'
})
export class AppComponent {
  private readonly router = inject(Router);
  private readonly auth = inject(AuthService);

  readonly appName = 'Payroll SA';
  readonly currentPath = signal(this.normalizedPath(this.router.url));
  readonly mobileMenuOpen = signal(false);
  readonly settingsOpen = signal(false);
  readonly accountOpen = signal(false);

  readonly user = this.auth.currentUser;
  readonly isLoggedIn = this.auth.isLoggedIn;

  readonly mainNav: NavItem[] = [
    { label: 'Dashboard', path: '/dashboard', icon: 'bi-grid-1x2' },
    { label: 'Employees', path: '/employees', icon: 'bi-people', roles: ['PayrollManager', 'Admin', 'SuperAdmin'] },
    { label: 'Payroll', path: '/payroll', icon: 'bi-cash-stack', roles: ['PayrollManager', 'Admin', 'SuperAdmin'] },
    { label: 'Leave', path: '/leave', icon: 'bi-calendar2-week' },
    { label: 'Payslips', path: '/payslips', icon: 'bi-receipt' },
    { label: 'Reports', path: '/reports', icon: 'bi-bar-chart', roles: ['PayrollManager', 'Admin', 'SuperAdmin'] },
    { label: 'Support', path: '/support/tickets', icon: 'bi-life-preserver', roles: ['Admin', 'SuperAdmin'] },
    { label: 'Self Service', path: '/self-service', icon: 'bi-person-bounding-box', roles: ['Employee'] }
  ];

  readonly settingsNav: NavItem[] = [
    { label: 'Management', path: '/management', icon: 'bi-speedometer2', roles: ['SuperAdmin'] },
    { label: 'General', path: '/settings', icon: 'bi-sliders', roles: ['Admin', 'SuperAdmin'] },
    { label: 'Users', path: '/settings/users', icon: 'bi-person-gear', roles: ['Admin', 'SuperAdmin'] },
    { label: 'Payroll Items', path: '/settings/payroll-items', icon: 'bi-wallet2', roles: ['PayrollManager', 'Admin', 'SuperAdmin'] },
    { label: 'Employee Deductions', path: '/settings/employee-deductions', icon: 'bi-dash-circle', roles: ['PayrollManager', 'Admin', 'SuperAdmin'] },
    { label: 'Employee Bonuses', path: '/settings/employee-bonuses', icon: 'bi-plus-circle', roles: ['PayrollManager', 'Admin', 'SuperAdmin'] },
    { label: 'Departments', path: '/settings/departments', icon: 'bi-diagram-3', roles: ['Admin', 'SuperAdmin'] },
    { label: 'Leave Types', path: '/settings/leave-types', icon: 'bi-list-check', roles: ['Admin', 'SuperAdmin'] },
    { label: 'Tax Tables', path: '/settings/tax-tables', icon: 'bi-calculator', roles: ['Admin', 'SuperAdmin'] }
  ];

  readonly hasSettingsAccess = computed(() => this.settingsNav.some(item => this.isAllowed(item)));

  readonly showShell = computed(() => {
    const path = this.currentPath();
    return !['/', '/login', '/register'].includes(path);
  });

  constructor() {
    this.router.events.pipe(filter(event => event instanceof NavigationEnd)).subscribe(() => {
      this.currentPath.set(this.normalizedPath(this.router.url));
      this.mobileMenuOpen.set(false);
      this.settingsOpen.set(false);
      this.accountOpen.set(false);
    });
  }

  isAllowed(item: NavItem): boolean {
    if (!item.roles?.length) {
      return true;
    }

    return item.roles.some(role => this.auth.isInRole(role));
  }

  isRouteActive(path: string): boolean {
    const current = this.currentPath();
    if (path === '/settings') {
      return current === path;
    }
    return current === path || current.startsWith(`${path}/`);
  }

  toggleMobileMenu(): void {
    this.mobileMenuOpen.update(open => !open);
  }

  closeMobileMenu(): void {
    this.mobileMenuOpen.set(false);
  }

  toggleSettings(): void {
    this.settingsOpen.update(open => !open);
    this.accountOpen.set(false);
  }

  toggleAccount(): void {
    this.accountOpen.update(open => !open);
    this.settingsOpen.set(false);
  }

  logout(): void {
    this.auth.logout();
  }

  private normalizedPath(url: string): string {
    return url.split('?')[0].split('#')[0] || '/';
  }
}
