import { Routes } from '@angular/router';
import { authGuard, roleGuard } from '../../core/guards/auth.guard';

export const employeeRoutes: Routes = [
  {
    path: '',
    canActivate: [authGuard, roleGuard(['PayrollManager', 'Admin', 'SuperAdmin'])],
    children: [
      { path: '', loadComponent: () => import('./pages/employee-list/employee-list.component').then(m => m.EmployeeListComponent) },
      { path: 'new', loadComponent: () => import('./pages/employee-form/employee-form.component').then(m => m.EmployeeFormComponent) },
      { path: ':id', loadComponent: () => import('./pages/employee-detail/employee-detail.component').then(m => m.EmployeeDetailComponent) },
      { path: ':id/edit', loadComponent: () => import('./pages/employee-form/employee-form.component').then(m => m.EmployeeFormComponent) }
    ]
  }
];
