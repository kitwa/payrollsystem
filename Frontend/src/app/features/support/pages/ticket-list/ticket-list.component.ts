import { Component, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../../../core/auth/auth.service';
import { SettingsService } from '../../../settings/services/settings.service';
import { Company } from '../../../settings/models/settings.models';
import { PaginationComponent } from '../../../../shared/components/pagination/pagination.component';
import { SupportTicketService } from '../../services/support-ticket.service';
import { SupportTicket, SupportTicketStatus, SupportTicketType } from '../../models/support-ticket.models';

@Component({
  selector: 'app-ticket-list',
  standalone: true,
  imports: [CommonModule, RouterLink, PaginationComponent],
  template: `
    <section class="d-flex justify-content-between align-items-start flex-wrap gap-2 mb-3">
      <div>
        <p class="text-muted mb-1">Support</p>
        <h1 class="h3 mb-1">Support Tickets</h1>
        <p class="text-muted mb-0">{{ isSuperAdmin() ? 'Manage support requests across all companies.' : 'Track support requests for your company.' }}</p>
        @if (contactEmail()) {
          <p class="text-muted small mb-0">Need immediate help? Email <a [href]="'mailto:' + contactEmail()">{{ contactEmail() }}</a>.</p>
        }
      </div>
      @if (!isSuperAdmin()) { <a class="btn btn-dark" routerLink="/support/tickets/new"><i class="bi bi-plus-lg me-2"></i>New Ticket</a> }
    </section>

    @if (error()) { <div class="alert alert-danger">{{ error() }}</div> }

    <section class="card border-0 shadow-sm">
      <div class="card-body">
        @if (isSuperAdmin()) {
          <div class="row g-2 mb-3">
            <div class="col-12 col-md-3">
              <label class="form-label">Company</label>
              <select class="form-select" [value]="companyId()" (change)="setCompany($event)">
                <option value="">All companies</option>
                @for (company of companies(); track company.id) { <option [value]="company.id">{{ company.name }}</option> }
              </select>
            </div>
            <div class="col-12 col-md-3">
              <label class="form-label">Status</label>
              <select class="form-select" [value]="statusFilter()" (change)="setStatus($event)">
                <option value="">All statuses</option>
                @for (status of statuses; track status.value) { <option [value]="status.value">{{ status.label }}</option> }
              </select>
            </div>
            <div class="col-12 col-md-3">
              <label class="form-label">Ticket type</label>
              <select class="form-select" [value]="typeFilter()" (change)="setType($event)">
                <option value="">All types</option>
                @for (type of types; track type.value) { <option [value]="type.value">{{ type.label }}</option> }
              </select>
            </div>
            <div class="col-12 col-md-3">
              <label class="form-label">Search</label>
              <input class="form-control" [value]="search()" (input)="setSearch($event)" placeholder="Number or subject">
            </div>
          </div>
        }

        <div class="table-responsive">
          <table class="table align-middle mb-0">
            <thead><tr><th>Ticket</th><th>Subject</th>@if (isSuperAdmin()) { <th>Company</th><th>Submitted by</th> }<th>Type</th><th>Status</th><th>Created</th><th class="text-end">Action</th></tr></thead>
            <tbody>
              @for (ticket of tickets(); track ticket.id) {
                <tr>
                  <td class="fw-semibold">{{ ticket.ticketNumber }}</td>
                  <td>{{ ticket.subject }}</td>
                  @if (isSuperAdmin()) { <td>{{ ticket.companyName }}</td><td>{{ ticket.createdByName }}</td> }
                  <td>{{ typeName(ticket.type) }}</td>
                  <td><span class="badge" [class.bg-secondary]="ticket.status === 0 || ticket.status === 4" [class.bg-info]="ticket.status === 1" [class.bg-warning]="ticket.status === 2" [class.bg-success]="ticket.status === 3">{{ statusName(ticket.status) }}</span></td>
                  <td>{{ ticket.createdAt | date:'yyyy-MM-dd HH:mm' }}</td>
                  <td class="text-end"><a class="btn btn-sm btn-outline-secondary" [routerLink]="['/support/tickets', ticket.id]">View</a></td>
                </tr>
              } @empty {
                <tr><td [attr.colspan]="isSuperAdmin() ? 8 : 6" class="text-center text-muted py-4">No support tickets found.</td></tr>
              }
            </tbody>
          </table>
        </div>
        <app-pagination [page]="pageNumber()" [pageSize]="pageSize" [total]="totalCount()" (pageChange)="loadPage($event)"></app-pagination>
      </div>
    </section>
  `
})
export class TicketListComponent {
  private readonly auth = inject(AuthService);
  private readonly settingsService = inject(SettingsService);
  private readonly ticketService = inject(SupportTicketService);

  readonly tickets = signal<SupportTicket[]>([]);
  readonly companies = signal<Company[]>([]);
  readonly companyId = signal('');
  readonly statusFilter = signal('');
  readonly typeFilter = signal('');
  readonly search = signal('');
  readonly pageNumber = signal(1);
  readonly totalCount = signal(0);
  readonly pageSize = 20;
  readonly error = signal('');
  readonly contactEmail = signal('');
  readonly isSuperAdmin = computed(() => this.auth.isInRole('SuperAdmin'));

  readonly statuses = [
    { value: 0, label: 'Open' }, { value: 1, label: 'In Review' }, { value: 2, label: 'In Progress' }, { value: 3, label: 'Resolved' }, { value: 4, label: 'Closed' }
  ];
  readonly types = [
    { value: 0, label: 'Technical Issue' }, { value: 1, label: 'Bug Report' }, { value: 2, label: 'Feature Request' }, { value: 3, label: 'Payroll Question' }, { value: 4, label: 'Other' }
  ];

  constructor() {
    this.ticketService.getContact().subscribe({ next: contact => this.contactEmail.set(contact.email), error: () => {} });
    if (this.isSuperAdmin()) {
      this.settingsService.getCompanies().subscribe({ next: companies => this.companies.set(companies), error: response => this.showError(response, 'Unable to load companies.') });
    }
    this.loadPage(1);
  }

  loadPage(page: number): void {
    this.error.set('');
    this.ticketService.getAll({
      companyId: this.companyId() || undefined,
      status: this.statusFilter() === '' ? undefined : Number(this.statusFilter()) as SupportTicketStatus,
      type: this.typeFilter() === '' ? undefined : Number(this.typeFilter()) as SupportTicketType,
      search: this.search(), pageNumber: page, pageSize: this.pageSize
    }).subscribe({
      next: result => { this.tickets.set(result.items); this.pageNumber.set(result.pageNumber); this.totalCount.set(result.totalCount); },
      error: response => this.showError(response, 'Unable to load support tickets.')
    });
  }

  setCompany(event: Event): void { this.companyId.set((event.target as HTMLSelectElement).value); this.loadPage(1); }
  setStatus(event: Event): void { this.statusFilter.set((event.target as HTMLSelectElement).value); this.loadPage(1); }
  setType(event: Event): void { this.typeFilter.set((event.target as HTMLSelectElement).value); this.loadPage(1); }
  setSearch(event: Event): void { this.search.set((event.target as HTMLInputElement).value); this.loadPage(1); }

  typeName(type: SupportTicketType): string { return ['Technical Issue', 'Bug Report', 'Feature Request', 'Payroll Question', 'Other'][type] ?? 'Other'; }
  statusName(status: SupportTicketStatus): string { return ['Open', 'In Review', 'In Progress', 'Resolved', 'Closed'][status] ?? 'Unknown'; }
  private showError(response: { error?: { errors?: string[] } }, fallback: string): void { this.error.set(response.error?.errors?.[0] ?? fallback); }
}
