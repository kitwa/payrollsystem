import { Routes } from '@angular/router';
import { roleGuard } from '../../core/guards/auth.guard';
export const settingsRoutes: Routes = [
  { path: '', canActivate: [roleGuard(['Admin', 'SuperAdmin'])], loadComponent: () => import('./pages/general-settings/general-settings.component').then(m => m.GeneralSettingsComponent) },
  { path: 'users', canActivate: [roleGuard(['Admin', 'SuperAdmin'])], loadComponent: () => import('./pages/users/users.component').then(m => m.UsersComponent) },
  { path: 'payroll-items', canActivate: [roleGuard(['PayrollManager', 'Admin', 'SuperAdmin'])], loadComponent: () => import('./pages/payroll-items/payroll-items.component').then(m => m.PayrollItemsComponent) },
  { path: 'employee-deductions', canActivate: [roleGuard(['PayrollManager', 'Admin', 'SuperAdmin'])], loadComponent: () => import('./pages/employee-deductions/employee-deductions.component').then(m => m.EmployeeDeductionsComponent) },
  { path: 'employee-bonuses', canActivate: [roleGuard(['PayrollManager', 'Admin', 'SuperAdmin'])], loadComponent: () => import('./pages/employee-bonuses/employee-bonuses.component').then(m => m.EmployeeBonusesComponent) },
  { path: 'leave-types', canActivate: [roleGuard(['Admin', 'SuperAdmin'])], loadComponent: () => import('./pages/leave-types/leave-types.component').then(m => m.LeaveTypesComponent) },
  { path: 'departments', canActivate: [roleGuard(['Admin', 'SuperAdmin'])], loadComponent: () => import('./pages/departments/departments.component').then(m => m.DepartmentsComponent) },
  { path: 'tax-tables', canActivate: [roleGuard(['Admin', 'SuperAdmin'])], loadComponent: () => import('./pages/tax-tables/tax-tables.component').then(m => m.TaxTablesComponent) }
];
