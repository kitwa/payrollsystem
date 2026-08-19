import { Routes } from '@angular/router';
export const reportRoutes: Routes = [
  { path: '', loadComponent: () => import('./pages/report-selector/report-selector.component').then(m => m.ReportSelectorComponent) }
];
