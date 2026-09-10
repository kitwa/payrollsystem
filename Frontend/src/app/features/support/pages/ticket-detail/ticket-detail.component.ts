import { Component, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { AuthService } from '../../../../core/auth/auth.service';
import { SupportTicketService } from '../../services/support-ticket.service';
import { SupportTicketDetail, SupportTicketStatus, SupportTicketType } from '../../models/support-ticket.models';

@Component({
  selector: 'app-ticket-detail',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <section class="d-flex justify-content-between align-items-start flex-wrap gap-2 mb-3">
      <div><p class="text-muted mb-1">Support Ticket</p><h1 class="h3 mb-1">{{ ticket()?.ticketNumber ?? 'Ticket' }}</h1><p class="text-muted mb-0">{{ ticket()?.subject }}</p></div>
      <a class="btn btn-outline-secondary" routerLink="/support/tickets">Back to Tickets</a>
    </section>
    @if (error()) { <div class="alert alert-danger">{{ error() }}</div> }
    @if (message()) { <div class="alert alert-success">{{ message() }}</div> }
    @if (ticket(); as item) {
      <section class="row g-3">
        <div class="col-12 col-lg-8"><article class="card border-0 shadow-sm"><div class="card-body">
          <div class="d-flex justify-content-between align-items-start gap-2 mb-3"><h2 class="h5 mb-0">{{ item.subject }}</h2><span class="badge bg-secondary">{{ statusName(item.status) }}</span></div>
          <p class="text-muted small mb-3">{{ typeName(item.type) }} · Submitted {{ item.createdAt | date:'yyyy-MM-dd HH:mm' }} by {{ item.createdByName }}</p>
          <p class="ticket-description">{{ item.description }}</p>
        </div></article></div>
        <div class="col-12 col-lg-4"><article class="card border-0 shadow-sm"><div class="card-body">
          <h2 class="h6 text-muted">Ticket details</h2>
          <dl class="row small mb-3"><dt class="col-5">Company</dt><dd class="col-7">{{ item.companyName }}</dd><dt class="col-5">Type</dt><dd class="col-7">{{ typeName(item.type) }}</dd><dt class="col-5">Updated</dt><dd class="col-7">{{ item.modifiedAt ? (item.modifiedAt | date:'yyyy-MM-dd HH:mm') : '—' }}</dd><dt class="col-5">Closed</dt><dd class="col-7">{{ item.closedAt ? (item.closedAt | date:'yyyy-MM-dd HH:mm') : '—' }}</dd></dl>
          @if (isSuperAdmin()) { <label class="form-label">Status</label><select class="form-select mb-3" [value]="item.status" (change)="changeStatus($event)" [disabled]="busy()">@for (status of statuses; track status.value) { <option [value]="status.value">{{ status.label }}</option> }</select> }
          @if (canClose()) { <button class="btn btn-outline-danger w-100" type="button" [disabled]="busy()" (click)="close()">Close Ticket</button> }
        </div></article></div>
      </section>
    }
  `,
  styles: [`.ticket-description { white-space: pre-wrap; line-height: 1.7; }`]
})
export class TicketDetailComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly auth = inject(AuthService);
  private readonly ticketService = inject(SupportTicketService);
  readonly ticket = signal<SupportTicketDetail | null>(null);
  readonly error = signal('');
  readonly message = signal('');
  readonly busy = signal(false);
  readonly isSuperAdmin = computed(() => this.auth.isInRole('SuperAdmin'));
  readonly statuses = [
    { value: 0, label: 'Open' }, { value: 1, label: 'In Review' }, { value: 2, label: 'In Progress' }, { value: 3, label: 'Resolved' }, { value: 4, label: 'Closed' }
  ];
  private readonly ticketId = this.route.snapshot.paramMap.get('id') ?? '';

  constructor() { if (this.ticketId) this.load(); }

  canClose(): boolean {
    const item = this.ticket();
    return !!item && !this.isSuperAdmin() && item.createdByUserId === this.auth.currentUser()?.userId && item.status !== SupportTicketStatus.Closed;
  }

  close(): void {
    this.busy.set(true); this.reset();
    this.ticketService.close(this.ticketId).subscribe({ next: item => { this.ticket.set(item); this.message.set('Ticket closed.'); this.busy.set(false); }, error: response => this.fail(response, 'Unable to close ticket.') });
  }

  changeStatus(event: Event): void {
    const status = Number((event.target as HTMLSelectElement).value) as SupportTicketStatus;
    this.busy.set(true); this.reset();
    this.ticketService.updateStatus(this.ticketId, status).subscribe({ next: item => { this.ticket.set(item); this.message.set('Ticket status updated.'); this.busy.set(false); }, error: response => this.fail(response, 'Unable to update ticket status.') });
  }

  typeName(type: SupportTicketType): string { return ['Technical Issue', 'Bug Report', 'Feature Request', 'Payroll Question', 'Other'][type] ?? 'Other'; }
  statusName(status: SupportTicketStatus): string { return ['Open', 'In Review', 'In Progress', 'Resolved', 'Closed'][status] ?? 'Unknown'; }
  private load(): void { this.ticketService.get(this.ticketId).subscribe({ next: item => this.ticket.set(item), error: response => this.fail(response, 'Unable to load ticket.') }); }
  private reset(): void { this.error.set(''); this.message.set(''); }
  private fail(response: { error?: { errors?: string[] } }, fallback: string): void { this.error.set(response.error?.errors?.[0] ?? fallback); this.busy.set(false); }
}
