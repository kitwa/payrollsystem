import { Routes } from '@angular/router';
import { authGuard, roleGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  { path: '', loadComponent: () => import('./features/landing/pages/landing/landing.component').then(m => m.LandingComponent) },
  { path: 'login', loadComponent: () => import('./core/auth/login/login.component').then(m => m.LoginComponent) },
  { path: 'register', loadComponent: () => import('./core/auth/register/register.component').then(m => m.RegisterComponent) },

  {
    path: 'dashboard',
    canActivate: [authGuard],
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
    path: 'settings',
    canActivate: [authGuard, roleGuard(['Admin', 'SuperAdmin'])],
    loadChildren: () => import('./features/settings/settings.routes').then(m => m.settingsRoutes)
  },
  {
    path: 'management',
    canActivate: [authGuard, roleGuard(['SuperAdmin'])],
    loadComponent: () => import('./features/management/pages/management.component').then(m => m.ManagementComponent)
  },
  {
    path: 'self-service',
    canActivate: [authGuard, roleGuard(['Employee'])],
    loadChildren: () => import('./features/self-service/self-service.routes').then(m => m.selfServiceRoutes)
  },
  { path: '**', redirectTo: '' }
];
