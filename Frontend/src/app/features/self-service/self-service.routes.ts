import { Routes } from '@angular/router';
export const selfServiceRoutes: Routes = [
  { path: '', loadComponent: () => import('./pages/self-service-home/self-service-home.component').then(m => m.SelfServiceHomeComponent) }
];
