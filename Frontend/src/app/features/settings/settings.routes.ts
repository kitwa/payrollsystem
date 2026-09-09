import { Routes } from '@angular/router';
export const settingsRoutes: Routes = [
  { path: '', loadComponent: () => import('./pages/general-settings/general-settings.component').then(m => m.GeneralSettingsComponent) },
  { path: 'users', loadComponent: () => import('./pages/users/users.component').then(m => m.UsersComponent) },
  { path: 'employee-deductions', loadComponent: () => import('./pages/employee-deductions/employee-deductions.component').then(m => m.EmployeeDeductionsComponent) },
  { path: 'leave-types', loadComponent: () => import('./pages/leave-types/leave-types.component').then(m => m.LeaveTypesComponent) },
  { path: 'departments', loadComponent: () => import('./pages/departments/departments.component').then(m => m.DepartmentsComponent) },
  { path: 'tax-tables', loadComponent: () => import('./pages/tax-tables/tax-tables.component').then(m => m.TaxTablesComponent) }
];
