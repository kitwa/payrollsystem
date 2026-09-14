import { CommonModule } from '@angular/common';
import { Component, ElementRef, HostListener, computed, inject, signal } from '@angular/core';
import { NavigationEnd, Router, RouterLink, RouterOutlet } from '@angular/router';
import { filter } from 'rxjs/operators';
import { AuthService } from './core/auth/auth.service';
import { SeoService } from './core/seo/seo.service';
import { InstallPromptComponent } from './shared/components/install-prompt/install-prompt.component';
import { SupportChatbotComponent } from './shared/components/support-chatbot/support-chatbot.component';
import { SupportTicketService } from './features/support/services/support-ticket.service';

type NavItem = {
  label: string;
  path: string;
  icon: string;
  roles?: string[];
};

@Component({
  selector: 'app-root',
  imports: [CommonModule, RouterOutlet, RouterLink, InstallPromptComponent, SupportChatbotComponent],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss'
})
export class AppComponent {
  private readonly router = inject(Router);
  private readonly auth = inject(AuthService);
  private readonly seo = inject(SeoService);
  private readonly elementRef = inject(ElementRef<HTMLElement>);
  private readonly supportTicketService = inject(SupportTicketService);

  readonly appName = 'Payroll SA';
  readonly currentPath = signal(this.normalizedPath(this.router.url));
  readonly mobileMenuOpen = signal(false);
  readonly settingsOpen = signal(false);
  readonly accountOpen = signal(false);
  readonly unreadTicketCount = signal(0);

  readonly user = this.auth.currentUser;
  readonly isLoggedIn = this.auth.isLoggedIn;
  readonly homeRoute = computed(() => (this.isLoggedIn() ? this.auth.getDefaultRoute() : '/'));

  readonly mainNav: NavItem[] = [
    { label: 'Dashboard', path: '/dashboard', icon: 'bi-grid-1x2' },
    { label: 'Employees', path: '/employees', icon: 'bi-people', roles: ['PayrollManager', 'Admin', 'SuperAdmin'] },
    { label: 'Payroll', path: '/payroll', icon: 'bi-cash-stack', roles: ['PayrollManager', 'Admin', 'SuperAdmin'] },
    { label: 'Leave', path: '/leave', icon: 'bi-calendar2-week' },
    { label: 'Payslips', path: '/payslips', icon: 'bi-receipt' },
    { label: 'Reports', path: '/reports', icon: 'bi-bar-chart', roles: ['PayrollManager', 'Admin', 'SuperAdmin'] },
    { label: 'Tax Certificates', path: '/tax-certificates', icon: 'bi-file-earmark-text', roles: ['PayrollManager', 'Admin', 'SuperAdmin'] },
    { label: 'Support', path: '/support/tickets', icon: 'bi-life-preserver', roles: ['Admin', 'SuperAdmin'] },
    { label: 'Self Service', path: '/self-service', icon: 'bi-person-bounding-box', roles: ['Employee'] }
  ];

  readonly settingsNav: NavItem[] = [
    { label: 'Management', path: '/management', icon: 'bi-speedometer2', roles: ['SuperAdmin'] },
    { label: 'Billing', path: '/billing', icon: 'bi-credit-card', roles: ['Admin', 'SuperAdmin'] },
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
    const publicAuthPaths = ['/', '/login', '/register', '/reset-password', '/forgot-password'];
    return this.isLoggedIn() && !publicAuthPaths.includes(path);
  });

  constructor() {
    this.router.events.pipe(filter(event => event instanceof NavigationEnd)).subscribe(() => {
      this.currentPath.set(this.normalizedPath(this.router.url));
      this.mobileMenuOpen.set(false);
      this.settingsOpen.set(false);
      this.accountOpen.set(false);
      if (!this.isPublicPath(this.currentPath())) {
        this.seo.noIndex();
      }
      this.refreshUnreadTicketCount();
    });
    this.refreshUnreadTicketCount();
  }

  private refreshUnreadTicketCount(): void {
    const companyId = this.auth.companyId();
    if (!companyId || !this.auth.isInRole('Admin')) {
      this.unreadTicketCount.set(0);
      return;
    }
    this.supportTicketService.getUnreadCount(companyId).subscribe({
      next: count => this.unreadTicketCount.set(count),
      error: () => this.unreadTicketCount.set(0)
    });
  }

  private isPublicPath(path: string): boolean {
    return path === '/' || path === '/features' || path === '/pricing' || path === '/payroll-software-south-africa'
      || ['/payroll/paye', '/payroll/uif', '/payroll/sdl'].includes(path) || path.startsWith('/features/') || path === '/blog' || path.startsWith('/blog/')
      || path.startsWith('/legal/') || path === '/privacy-policy' || path === '/terms' || path === '/contact';
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
    this.accountOpen.set(false);
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

  closeAccount(): void {
    this.accountOpen.set(false);
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    if (this.accountOpen() && !this.elementRef.nativeElement.querySelector('.user-menu-wrapper')?.contains(event.target as Node)) {
      this.accountOpen.set(false);
    }
  }

  logout(): void {
    this.auth.logout();
  }

  private normalizedPath(url: string): string {
    return url.split('?')[0].split('#')[0] || '/';
  }
}
