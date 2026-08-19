import { Routes } from '@angular/router';
export const settingsRoutes: Routes = [
  { path: '', loadComponent: () => import('./pages/general-settings/general-settings.component').then(m => m.GeneralSettingsComponent) },
  { path: 'leave-types', loadComponent: () => import('./pages/leave-types/leave-types.component').then(m => m.LeaveTypesComponent) },
  { path: 'tax-tables', loadComponent: () => import('./pages/tax-tables/tax-tables.component').then(m => m.TaxTablesComponent) }
];
