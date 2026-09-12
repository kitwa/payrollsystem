import { Routes } from '@angular/router';
import { roleGuard } from '../../core/guards/auth.guard';

export const leaveRoutes: Routes = [
  { path: '', loadComponent: () => import('./pages/leave-list/leave-list.component').then(m => m.LeaveListComponent) },
  { path: 'request', loadComponent: () => import('./pages/leave-request/leave-request.component').then(m => m.LeaveRequestComponent) },
  {
    path: 'balances',
    canActivate: [roleGuard(['PayrollManager', 'Admin', 'SuperAdmin'])],
    loadComponent: () => import('./pages/leave-balances/leave-balances.component').then(m => m.LeaveBalancesComponent)
  }
];
