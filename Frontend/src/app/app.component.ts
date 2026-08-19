import { CommonModule } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { filter } from 'rxjs/operators';
import { AuthService } from './core/auth/auth.service';

type NavItem = {
  label: string;
  path: string;
  icon: string;
  roles?: string[];
};

@Component({
  selector: 'app-root',
  imports: [CommonModule, RouterOutlet, RouterLink, RouterLinkActive],
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

  readonly user = this.auth.currentUser;
  readonly isLoggedIn = this.auth.isLoggedIn;

  readonly mainNav: NavItem[] = [
    { label: 'Dashboard', path: '/dashboard', icon: 'bi-grid-1x2' },
    { label: 'Employees', path: '/employees', icon: 'bi-people', roles: ['PayrollManager', 'Admin', 'SuperAdmin'] },
    { label: 'Payroll', path: '/payroll', icon: 'bi-cash-stack', roles: ['PayrollManager', 'Admin', 'SuperAdmin'] },
    { label: 'Leave', path: '/leave', icon: 'bi-calendar2-week' },
    { label: 'Payslips', path: '/payslips', icon: 'bi-receipt' },
    { label: 'Reports', path: '/reports', icon: 'bi-bar-chart', roles: ['PayrollManager', 'Admin', 'SuperAdmin'] },
    { label: 'Self Service', path: '/self-service', icon: 'bi-person-bounding-box', roles: ['Employee'] }
  ];

  readonly settingsNav: NavItem[] = [
    { label: 'General', path: '/settings', icon: 'bi-sliders', roles: ['Admin', 'SuperAdmin'] },
    { label: 'Leave Types', path: '/settings/leave-types', icon: 'bi-list-check', roles: ['Admin', 'SuperAdmin'] },
    { label: 'Tax Tables', path: '/settings/tax-tables', icon: 'bi-calculator', roles: ['Admin', 'SuperAdmin'] }
  ];

  readonly showShell = computed(() => {
    const path = this.currentPath();
    return !['/', '/login', '/register'].includes(path);
  });

  constructor() {
    this.router.events.pipe(filter(event => event instanceof NavigationEnd)).subscribe(() => {
      this.currentPath.set(this.normalizedPath(this.router.url));
      this.mobileMenuOpen.set(false);
      this.settingsOpen.set(false);
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
    return current === path || current.startsWith(`${path}/`);
  }

  toggleMobileMenu(): void {
    this.mobileMenuOpen.update(open => !open);
  }

  toggleSettings(): void {
    this.settingsOpen.update(open => !open);
  }

  logout(): void {
    this.auth.logout();
  }

  private normalizedPath(url: string): string {
    return url.split('?')[0].split('#')[0] || '/';
  }
}
