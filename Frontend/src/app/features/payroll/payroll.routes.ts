import { Routes } from '@angular/router';
export const payrollRoutes: Routes = [
  { path: '', loadComponent: () => import('./pages/payroll-list/payroll-list.component').then(m => m.PayrollListComponent) },
  { path: ':id', loadComponent: () => import('./pages/payroll-detail/payroll-detail.component').then(m => m.PayrollDetailComponent) }
];
