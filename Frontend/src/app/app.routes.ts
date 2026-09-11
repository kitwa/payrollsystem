import { Routes } from '@angular/router';
import { authGuard, roleGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  { path: '', loadComponent: () => import('./features/landing/pages/landing/landing.component').then(m => m.LandingComponent) },
  { path: 'login', loadComponent: () => import('./core/auth/login/login.component').then(m => m.LoginComponent) },
  { path: 'register', loadComponent: () => import('./core/auth/register/register.component').then(m => m.RegisterComponent) },
  { path: 'features', loadComponent: () => import('./features/public-seo/pages/public-seo-page.component').then(m => m.PublicSeoPageComponent), data: { seoKey: 'features' } },
  { path: 'pricing', loadComponent: () => import('./features/public-seo/pages/public-seo-page.component').then(m => m.PublicSeoPageComponent), data: { seoKey: 'pricing' } },
  { path: 'payroll-software-south-africa', loadComponent: () => import('./features/public-seo/pages/public-seo-page.component').then(m => m.PublicSeoPageComponent), data: { seoKey: 'payroll' } },
  { path: 'payroll/paye', loadComponent: () => import('./features/public-seo/pages/public-seo-page.component').then(m => m.PublicSeoPageComponent), data: { seoKey: 'paye' } },
  { path: 'payroll/uif', loadComponent: () => import('./features/public-seo/pages/public-seo-page.component').then(m => m.PublicSeoPageComponent), data: { seoKey: 'uif' } },
  { path: 'payroll/sdl', loadComponent: () => import('./features/public-seo/pages/public-seo-page.component').then(m => m.PublicSeoPageComponent), data: { seoKey: 'sdl' } },
  { path: 'features/payslips', loadComponent: () => import('./features/public-seo/pages/public-seo-page.component').then(m => m.PublicSeoPageComponent), data: { seoKey: 'payslips' } },
  { path: 'features/leave-management', loadComponent: () => import('./features/public-seo/pages/public-seo-page.component').then(m => m.PublicSeoPageComponent), data: { seoKey: 'leave' } },
  { path: 'features/employee-management', loadComponent: () => import('./features/public-seo/pages/public-seo-page.component').then(m => m.PublicSeoPageComponent), data: { seoKey: 'employees' } },
  { path: 'legal/privacy', loadComponent: () => import('./features/public-seo/pages/public-seo-page.component').then(m => m.PublicSeoPageComponent), data: { seoKey: 'privacy' } },
  { path: 'privacy-policy', redirectTo: 'legal/privacy', pathMatch: 'full' },
  { path: 'legal/terms', loadComponent: () => import('./features/public-seo/pages/public-seo-page.component').then(m => m.PublicSeoPageComponent), data: { seoKey: 'terms' } },
  { path: 'terms', redirectTo: 'legal/terms', pathMatch: 'full' },
  { path: 'contact', loadComponent: () => import('./features/public-seo/pages/public-seo-page.component').then(m => m.PublicSeoPageComponent), data: { seoKey: 'contact' } },
  { path: 'about', loadComponent: () => import('./features/public-seo/pages/public-seo-page.component').then(m => m.PublicSeoPageComponent), data: { seoKey: 'about' } },
  { path: 'legal/popia', loadComponent: () => import('./features/public-seo/pages/public-seo-page.component').then(m => m.PublicSeoPageComponent), data: { seoKey: 'popia' } },
  { path: 'legal/cookies', loadComponent: () => import('./features/public-seo/pages/public-seo-page.component').then(m => m.PublicSeoPageComponent), data: { seoKey: 'cookies' } },
  { path: 'legal/refunds', loadComponent: () => import('./features/public-seo/pages/public-seo-page.component').then(m => m.PublicSeoPageComponent), data: { seoKey: 'refunds' } },
  { path: 'blog', loadComponent: () => import('./features/public-seo/pages/public-seo-page.component').then(m => m.PublicSeoPageComponent), data: { seoKey: 'blog' } },
  { path: 'blog/:slug', loadComponent: () => import('./features/public-seo/pages/public-seo-page.component').then(m => m.PublicSeoPageComponent) },

  {
    path: 'dashboard',
    canActivate: [authGuard, roleGuard(['PayrollManager', 'Admin', 'SuperAdmin'])],
    loadComponent: () => import('./features/dashboard/pages/dashboard.component').then(m => m.DashboardComponent)
  },
  {
    path: 'employees',
    loadChildren: () => import('./features/employees/employees.routes').then(m => m.employeeRoutes)
  },
  {
    path: 'payroll',
    canActivate: [authGuard, roleGuard(['PayrollManager', 'Admin', 'SuperAdmin'])],
    loadChildren: () => import('./features/payroll/payroll.routes').then(m => m.payrollRoutes)
  },
  {
    path: 'leave',
    canActivate: [authGuard],
    loadChildren: () => import('./features/leave/leave.routes').then(m => m.leaveRoutes)
  },
  {
    path: 'payslips',
    canActivate: [authGuard],
    loadChildren: () => import('./features/payslips/payslips.routes').then(m => m.payslipRoutes)
  },
  {
    path: 'reports',
    canActivate: [authGuard, roleGuard(['PayrollManager', 'Admin', 'SuperAdmin'])],
    loadChildren: () => import('./features/reports/reports.routes').then(m => m.reportRoutes)
  },
  {
    path: 'tax-certificates',
    canActivate: [authGuard, roleGuard(['PayrollManager', 'Admin', 'SuperAdmin'])],
    loadComponent: () => import('./features/tax-certificates/pages/tax-certificates.component').then(m => m.TaxCertificatesComponent)
  },
  {
    path: 'settings',
    canActivate: [authGuard, roleGuard(['PayrollManager', 'Admin', 'SuperAdmin'])],
    loadChildren: () => import('./features/settings/settings.routes').then(m => m.settingsRoutes)
  },
  {
    path: 'billing',
    canActivate: [authGuard, roleGuard(['Admin', 'SuperAdmin'])],
    loadComponent: () => import('./features/billing/pages/billing/billing.component').then(m => m.BillingComponent)
  },
  {
    path: 'management',
    canActivate: [authGuard, roleGuard(['SuperAdmin'])],
    loadComponent: () => import('./features/management/pages/management.component').then(m => m.ManagementComponent)
  },
  {
    path: 'support/tickets',
    canActivate: [authGuard, roleGuard(['Admin', 'SuperAdmin'])],
    loadChildren: () => import('./features/support/support.routes').then(m => m.supportRoutes)
  },
  {
    path: 'self-service',
    canActivate: [authGuard, roleGuard(['Employee'])],
    loadChildren: () => import('./features/self-service/self-service.routes').then(m => m.selfServiceRoutes)
  },
  { path: '**', redirectTo: '' }
];
