import { Routes } from '@angular/router';

export const supportRoutes: Routes = [
  { path: '', loadComponent: () => import('./pages/ticket-list/ticket-list.component').then(m => m.TicketListComponent) },
  { path: 'new', loadComponent: () => import('./pages/ticket-new/ticket-new.component').then(m => m.TicketNewComponent) },
  { path: ':id', loadComponent: () => import('./pages/ticket-detail/ticket-detail.component').then(m => m.TicketDetailComponent) }
];
