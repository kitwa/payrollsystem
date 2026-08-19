import { Routes } from '@angular/router';
export const leaveRoutes: Routes = [
  { path: '', loadComponent: () => import('./pages/leave-list/leave-list.component').then(m => m.LeaveListComponent) },
  { path: 'request', loadComponent: () => import('./pages/leave-request/leave-request.component').then(m => m.LeaveRequestComponent) }
];
