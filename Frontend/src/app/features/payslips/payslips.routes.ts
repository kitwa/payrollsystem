import { Routes } from '@angular/router';
export const payslipRoutes: Routes = [
  { path: '', loadComponent: () => import('./pages/payslip-list/payslip-list.component').then(m => m.PayslipListComponent) },
  { path: 'period/:periodId', loadComponent: () => import('./pages/payslip-period/payslip-period.component').then(m => m.PayslipPeriodComponent) },
  { path: ':id', loadComponent: () => import('./pages/payslip-detail/payslip-detail.component').then(m => m.PayslipDetailComponent) }
];
