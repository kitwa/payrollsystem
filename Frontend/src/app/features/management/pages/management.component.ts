import { Component, ViewChild, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../../core/auth/auth.service';
import { SettingsService } from '../../settings/services/settings.service';
import { Company } from '../../settings/models/settings.models';
import { ManagementService } from '../services/management.service';
import { AdminCompanyService } from '../services/admin-company.service';
import { CompanyManagementSummary, ManagementAudit, AdminCompanyDto } from '../models/management.models';
import { ConfirmDialogComponent } from '../../../shared/components/confirm-dialog/confirm-dialog.component';

@Component({
  selector: 'app-management',
  standalone: true,
  imports: [CommonModule, ConfirmDialogComponent],
  template: `
    <section class="mb-3">
      <p class="text-uppercase small fw-bold text-muted mb-1">Platform Management</p>
      <h1 class="h3 mb-1">Company Overview</h1>
      <p class="text-muted mb-0">Monitor tenant activity, payroll delivery, and access history.</p>
    </section>

    @if (error()) { <div class="alert alert-danger">{{ error() }}</div> }
    @if (adminError()) { <div class="alert alert-danger">{{ adminError() }}</div> }

    <section class="card border-0 shadow-sm mb-3">
      <div class="card-body">
        <div class="d-flex justify-content-between align-items-center mb-3">
          <div><h2 class="h5 mb-1">Companies</h2><p class="text-muted small mb-0">Subscription status and account access for every tenant.</p></div>
          <button class="btn btn-sm btn-outline-secondary" type="button" (click)="loadAdminCompanies()">Refresh</button>
        </div>
        <div class="table-responsive">
          <table class="table align-middle mb-0">
            <thead><tr><th>Company</th><th>Plan</th><th>Status</th><th>Employees</th><th>Account</th><th></th></tr></thead>
            <tbody>
              @for (item of adminCompanies(); track item.id) {
                <tr>
                  <td>{{ item.name }}</td>
                  <td>{{ item.planName }}</td>
                  <td><span class="badge" [class.bg-success]="item.subscriptionStatus === 'FreeTrial' || item.subscriptionStatus === 'Active'" [class.bg-danger]="item.subscriptionStatus === 'Expired' || item.subscriptionStatus === 'Cancelled' || item.subscriptionStatus === 'PastDue' || item.subscriptionStatus === 'Suspended'">{{ item.subscriptionStatus }}</span></td>
                  <td>{{ item.employeeCount }}{{ item.maxEmployees ? ' / ' + item.maxEmployees : '' }}</td>
                  <td><span class="badge" [class.bg-success]="item.isActive" [class.bg-secondary]="!item.isActive">{{ item.isActive ? 'Active' : 'Disabled' }}</span></td>
                  <td class="text-end">
                    @if (item.isActive) {
                      <button class="btn btn-sm btn-outline-danger" type="button" (click)="disableCompany(item)">Disable</button>
                    } @else {
                      <button class="btn btn-sm btn-outline-success" type="button" (click)="enableCompany(item)">Enable</button>
                    }
                  </td>
                </tr>
              } @empty {
                <tr><td colspan="6" class="text-center text-muted py-4">No companies found.</td></tr>
              }
            </tbody>
          </table>
        </div>
      </div>
    </section>

    <section class="card border-0 shadow-sm mb-3">
      <div class="card-body">
        <label class="form-label">Company</label>
        <select class="form-select" [value]="selectedCompanyId() ?? ''" (change)="selectCompany($event)">
          <option value="">Select company</option>
          @for (company of companies(); track company.id) {
            <option [value]="company.id">{{ company.name }}</option>
          }
        </select>
      </div>
    </section>

    <app-confirm-dialog #confirmDialog></app-confirm-dialog>

    @if (summary(); as data) {
      <section class="kpi-grid mb-3">
        <article class="kpi-card"><span>Users</span><strong>{{ data.userCount }}</strong><small>{{ data.activeUserCount }} active</small></article>
        <article class="kpi-card"><span>Employees</span><strong>{{ data.employeeCount }}</strong><small>{{ data.activeEmployeeCount }} active</small></article>
        <article class="kpi-card"><span>Payslips Generated</span><strong>{{ data.payslipCount }}</strong><small>{{ data.emailedPayslipCount }} emailed</small></article>
        <article class="kpi-card"><span>Latest Net Payroll</span><strong>R {{ data.latestPayrollNet | number:'1.0-0' }}</strong><small>{{ data.latestPayrollDate | date:'MMMM yyyy' }}</small></article>
      </section>

      <section class="card border-0 shadow-sm">
        <div class="card-body">
          <div class="d-flex justify-content-between align-items-center mb-3">
            <div><h2 class="h5 mb-1">Access and Activity History</h2><p class="text-muted small mb-0">Who used the system, when, from which IP, and what endpoint was called.</p></div>
            <button class="btn btn-sm btn-outline-secondary" type="button" (click)="reloadAudit()">Refresh</button>
          </div>
          <div class="table-responsive">
            <table class="table align-middle mb-0">
              <thead><tr><th>When</th><th>User</th><th>IP Address</th><th>Action</th><th>Status</th></tr></thead>
              <tbody>
                @for (item of audit(); track item.id) {
                  <tr>
                    <td>{{ item.occurredAt | date:'yyyy-MM-dd HH:mm:ss' }}</td>
                    <td>{{ item.userEmail || 'Anonymous' }}</td>
                    <td><code>{{ item.ipAddress || 'Unavailable' }}</code></td>
                    <td><span class="fw-semibold">{{ item.httpMethod }}</span> {{ item.path }}</td>
                    <td><span class="badge" [class.bg-success]="item.statusCode < 400" [class.bg-danger]="item.statusCode >= 400">{{ item.statusCode }}</span></td>
                  </tr>
                } @empty {
                  <tr><td colspan="5" class="text-center text-muted py-4">No activity recorded for this company yet.</td></tr>
                }
              </tbody>
            </table>
          </div>

          @if (totalCount() > 0) {
            <div class="d-flex justify-content-between align-items-center mt-3">
              <small class="text-muted">
                Showing {{ rangeStart() }}–{{ rangeEnd() }} of {{ totalCount() }}
              </small>
              <div class="d-flex gap-2">
                <button class="btn btn-sm btn-outline-secondary" type="button" [disabled]="pageNumber() <= 1" (click)="previousPage()">Previous</button>
                <span class="small text-muted align-self-center">Page {{ pageNumber() }} of {{ totalPages() }}</span>
                <button class="btn btn-sm btn-outline-secondary" type="button" [disabled]="pageNumber() >= totalPages()" (click)="nextPage()">Next</button>
              </div>
            </div>
          }
        </div>
      </section>
    }
  `,
  styles: [`
    .kpi-grid { display:grid; grid-template-columns:repeat(4,minmax(0,1fr)); gap:.8rem; }
    .kpi-card { background:#fff; border:1px solid #e7e5e4; border-radius:1rem; padding:1rem; }
    .kpi-card span,.kpi-card small { display:block; color:#71717a; }
    .kpi-card strong { display:block; margin:.35rem 0; font-size:1.5rem; }
    @media (max-width: 800px) { .kpi-grid { grid-template-columns:repeat(2,minmax(0,1fr)); } }
    @media (max-width: 500px) { .kpi-grid { grid-template-columns:1fr; } }
  `]
})
export class ManagementComponent {
  private readonly auth = inject(AuthService);
  private readonly settingsService = inject(SettingsService);
  private readonly managementService = inject(ManagementService);
  private readonly adminCompanyService = inject(AdminCompanyService);

  @ViewChild('confirmDialog') confirmDialog!: ConfirmDialogComponent;

  readonly adminCompanies = signal<AdminCompanyDto[]>([]);
  readonly adminError = signal('');

  readonly companies = signal<Company[]>([]);
  readonly selectedCompanyId = signal<string | null>(null);
  readonly summary = signal<CompanyManagementSummary | null>(null);
  readonly audit = signal<ManagementAudit[]>([]);
  readonly error = signal('');

  readonly pageSize = 20;
  readonly pageNumber = signal(1);
  readonly totalCount = signal(0);
  readonly totalPages = computed(() => Math.max(1, Math.ceil(this.totalCount() / this.pageSize)));
  readonly rangeStart = computed(() => this.totalCount() === 0 ? 0 : (this.pageNumber() - 1) * this.pageSize + 1);
  readonly rangeEnd = computed(() => Math.min(this.pageNumber() * this.pageSize, this.totalCount()));

  constructor() {
    this.loadAdminCompanies();
    this.settingsService.getCompanies().subscribe({
      next: companies => {
        this.companies.set(companies);
        if (companies.length) {
          this.selectedCompanyId.set(companies[0].id);
          this.load();
        }
      },
      error: response => this.showError(response, 'Unable to load companies.')
    });
  }

  selectCompany(event: Event): void {
    this.selectedCompanyId.set((event.target as HTMLSelectElement).value || null);
    this.summary.set(null);
    this.audit.set([]);
    this.pageNumber.set(1);
    this.load();
  }

  loadAdminCompanies(): void {
    this.adminError.set('');
    this.adminCompanyService.getAll().subscribe({
      next: companies => this.adminCompanies.set(companies),
      error: response => this.adminError.set(response.error?.errors?.[0] ?? 'Unable to load companies.')
    });
  }

  async disableCompany(company: AdminCompanyDto): Promise<void> {
    const confirmed = await this.confirmDialog.show({
      title: 'Disable company',
      message: `${company.name} will immediately lose access to Payroll SA. Continue?`,
      confirmLabel: 'Disable',
      tone: 'danger'
    });
    if (!confirmed) return;

    this.adminCompanyService.disable(company.id).subscribe({
      next: () => this.loadAdminCompanies(),
      error: response => this.adminError.set(response.error?.errors?.[0] ?? 'Unable to disable company.')
    });
  }

  async enableCompany(company: AdminCompanyDto): Promise<void> {
    const confirmed = await this.confirmDialog.show({
      title: 'Enable company',
      message: `Restore access to Payroll SA for ${company.name}?`,
      confirmLabel: 'Enable'
    });
    if (!confirmed) return;

    this.adminCompanyService.enable(company.id).subscribe({
      next: () => this.loadAdminCompanies(),
      error: response => this.adminError.set(response.error?.errors?.[0] ?? 'Unable to enable company.')
    });
  }

  reloadAudit(): void {
    const companyId = this.selectedCompanyId();
    if (!companyId) return;
    this.managementService.getAudit(companyId, this.pageNumber(), this.pageSize).subscribe({
      next: page => {
        this.audit.set(page.items);
        this.totalCount.set(page.totalCount);
      },
      error: response => this.showError(response, 'Unable to load activity history.')
    });
  }

  previousPage(): void {
    if (this.pageNumber() <= 1) return;
    this.pageNumber.update(page => page - 1);
    this.reloadAudit();
  }

  nextPage(): void {
    if (this.pageNumber() >= this.totalPages()) return;
    this.pageNumber.update(page => page + 1);
    this.reloadAudit();
  }

  private load(): void {
    const companyId = this.selectedCompanyId();
    if (!companyId) return;
    this.error.set('');
    this.managementService.getSummary(companyId).subscribe({
      next: summary => this.summary.set(summary),
      error: response => this.showError(response, 'Unable to load company summary.')
    });
    this.pageNumber.set(1);
    this.reloadAudit();
  }

  private showError(response: { error?: { errors?: string[] } }, fallback: string): void {
    this.error.set(response.error?.errors?.[0] ?? fallback);
  }
}
