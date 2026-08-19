import { Routes } from '@angular/router';
export const payslipRoutes: Routes = [
  { path: '', loadComponent: () => import('./pages/payslip-list/payslip-list.component').then(m => m.PayslipListComponent) }
];
