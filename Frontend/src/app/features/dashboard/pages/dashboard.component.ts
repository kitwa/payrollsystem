import { Component, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../../core/auth/auth.service';
import { DashboardService } from '../services/dashboard.service';
import { DashboardSummary } from '../models/dashboard.models';
import { EmployeeService, DepartmentService } from '../../employees/services/employee.service';
import { UserService } from '../../settings/services/user.service';
import { EmployeeList, Department } from '../../employees/models/employee.models';
import { ManagedUser } from '../../settings/models/user.models';
import { BillingService } from '../../billing/services/billing.service';
import { SubscriptionSummaryDto } from '../../billing/models/billing.models';

@Component({
	selector: 'app-dashboard',
	standalone: true,
	imports: [CommonModule, RouterLink],
	template: `
		<section class="page-head">
			<div>
				<p class="eyebrow mb-2">Operations Snapshot</p>
				<h1 class="mb-1">Payroll Dashboard</h1>
				<p class="text-muted mb-0">Monitor payroll operations, compliance, and pending actions in one place.</p>
			</div>
			<a class="btn btn-dark" routerLink="/payroll">Run Payroll</a>
		</section>

		@if (billingBanner(); as banner) {
			<div class="alert" [class.alert-warning]="banner.tone === 'warning'" [class.alert-danger]="banner.tone === 'danger'">
				{{ banner.message }}
				@if (canManageBilling()) {
					<a routerLink="/billing" class="alert-link ms-1">Manage billing</a>
				}
			</div>
		}

		<section class="kpi-grid">
			@for (card of kpis(); track card.label) {
				<article class="kpi-card">
					<div class="kpi-card__top">
						<span>{{ card.label }}</span>
						<i class="bi" [class]="card.icon"></i>
					</div>
					<strong>{{ card.value }}</strong>
					<small [class]="card.deltaClass">{{ card.delta }}</small>
				</article>
			}
		</section>

		<section class="content-grid">
			<article class="panel">
				<div class="panel__head">
					<h2>Quick Actions</h2>
				</div>
				<div class="action-grid">
					<a class="action-card" routerLink="/employees/new">
						<i class="bi bi-person-plus"></i>
						<span>Add Employee</span>
					</a>
					<a class="action-card" routerLink="/settings/departments">
						<i class="bi bi-diagram-3"></i>
						<span>Add Department</span>
					</a>
					<a class="action-card" routerLink="/settings/users">
						<i class="bi me-2 bi-person-gear"></i>
						<span>Add User</span>
					</a>
					<a class="action-card" routerLink="/reports">
						<i class="bi bi-file-earmark-bar-graph"></i>
						<span>Generate Reports</span>
					</a>
				</div>
			</article>

			<article class="panel">
				<div class="panel__head">
					<h2>Payroll Timeline</h2>
				</div>
				<ul class="timeline">
					@for (item of timeline(); track item.title) {
						<li>
							<span class="dot"></span>
							<div>
								<p class="mb-0 fw-semibold">{{ item.title }}</p>
								<small class="text-muted">{{ item.when }}</small>
							</div>
						</li>
					}
				</ul>
			</article>
		</section>

		<section class="directory-grid">
			<article class="panel directory-panel">
				<div class="panel__head"><div><h2>Employees</h2><p class="text-muted mb-0">{{ employeeTotal() }} employee records</p></div><a routerLink="/employees" class="btn btn-sm btn-outline-primary">View all</a></div>
				<div class="directory-list">
					@for (employee of employees().slice(0, 6); track employee.id) {
						<a class="directory-row" [routerLink]="['/employees', employee.id]"><span class="directory-avatar">{{ employee.firstName.charAt(0) }}{{ employee.lastName.charAt(0) }}</span><span><strong>{{ employee.firstName }} {{ employee.lastName }}</strong><small>{{ employee.jobTitle || 'Employee' }}{{ employee.department ? ' · ' + employee.department : '' }}</small></span><i class="bi bi-chevron-right"></i></a>
					} @empty { <p class="text-muted mb-0">No employees found.</p> }
				</div>
			</article>
			<article class="panel directory-panel">
				<div class="panel__head"><div><h2>Users</h2><p class="text-muted mb-0">{{ users().length }} user accounts</p></div><a routerLink="/settings/users" class="btn btn-sm btn-outline-primary">Manage</a></div>
				<div class="directory-list">
					@for (user of users().slice(0, 6); track user.userId) { <div class="directory-row"><span class="directory-avatar directory-avatar--blue">{{ user.firstName.charAt(0) }}{{ user.lastName.charAt(0) }}</span><span><strong>{{ user.firstName }} {{ user.lastName }}</strong><small>{{ user.roles[0] || 'User' }} · {{ user.isActive ? 'Active' : 'Disabled' }}</small></span><i class="bi bi-person-check"></i></div> } @empty { <p class="text-muted mb-0">No user accounts found.</p> }
				</div>
			</article>
			<article class="panel directory-panel">
				<div class="panel__head"><div><h2>Departments</h2><p class="text-muted mb-0">{{ departments().length }} departments</p></div><a routerLink="/settings/departments" class="btn btn-sm btn-outline-primary">Manage</a></div>
				<div class="department-list">
					@for (department of departments(); track department.id) { <a class="department-row" routerLink="/settings/departments"><span><i class="bi bi-diagram-3 me-2"></i>{{ department.name }}</span><i class="bi bi-chevron-right"></i></a> } @empty { <p class="text-muted mb-0">No departments found.</p> }
				</div>
			</article>
		</section>
	`,
	styles: [
		`
			.page-head {
				display: flex;
				justify-content: space-between;
				gap: 1rem;
				flex-wrap: wrap;
				margin-bottom: 1rem;
			}

			.page-head > a {
				padding: 0.5rem 0.85rem;
				font-size: 0.88rem;
				white-space: nowrap;
			}

			.eyebrow {
				letter-spacing: 0.08em;
				text-transform: uppercase;
				font-weight: 700;
				color: #0f766e;
				font-size: 0.78rem;
			}

			.kpi-grid {
				display: grid;
				grid-template-columns: repeat(4, minmax(0, 1fr));
				gap: 0.8rem;
			}

			.kpi-card,
			.panel {
				background: #fff;
				border: 1px solid #e7e5e4;
				border-radius: 1rem;
				padding: 1rem;
			}

			.kpi-card__top {
				display: flex;
				justify-content: space-between;
				color: #52525b;
				font-size: 0.87rem;
			}

			.kpi-card strong {
				display: block;
				margin-top: 0.35rem;
				font-size: 1.55rem;
			}

			.kpi-card small {
				font-weight: 600;
			}

			.text-success {
				color: #15803d;
			}

			.text-warning {
				color: #a16207;
			}

			.content-grid {
				margin-top: 1rem;
				display: grid;
				grid-template-columns: 1.2fr 1fr;
				gap: 0.9rem;
			}

			.directory-grid {
				margin-top: 1rem;
				display: grid;
				grid-template-columns: repeat(3, minmax(0, 1fr));
				gap: 0.9rem;
			}

			.directory-panel { min-width: 0; }
			.directory-panel .panel__head { display: flex; justify-content: space-between; align-items: flex-start; gap: .75rem; }
			.directory-panel .panel__head h2 { margin-bottom: .2rem; }
			.directory-list, .department-list { display: grid; gap: .45rem; }
			.directory-row, .department-row { display: flex; align-items: center; gap: .65rem; min-width: 0; padding: .55rem; border-radius: .6rem; color: #212529; text-decoration: none; }
			.directory-row:hover, .department-row:hover { background: #f1f6fc; }
			.directory-row > span:nth-child(2) { min-width: 0; flex: 1; }
			.directory-row strong, .directory-row small { display: block; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
			.directory-row small { color: #6c757d; font-size: .78rem; }
			.directory-row > i, .department-row > i { color: #8a99aa; }
			.directory-avatar { display: grid; place-items: center; flex: 0 0 2rem; width: 2rem; height: 2rem; border-radius: 50%; background: #dbeafe; color: #0a58ca; font-size: .72rem; font-weight: 700; }
			.directory-avatar--blue { background: #e7f1ff; color: #0d6efd; }
			.department-row { justify-content: space-between; }

			.panel__head h2 {
				margin: 0 0 0.7rem;
				font-size: 1.1rem;
			}

			.action-grid {
				display: grid;
				grid-template-columns: repeat(2, minmax(0, 1fr));
				gap: 0.7rem;
			}

			.action-card {
				text-decoration: none;
				color: #1f2937;
				border: 1px solid #e7e5e4;
				border-radius: 0.8rem;
				padding: 0.85rem;
				display: flex;
				align-items: center;
				gap: 0.55rem;
				font-weight: 600;
			}

			.action-card:hover {
				border-color: #cbd5e1;
				background: #f8fafc;
			}

			.timeline {
				list-style: none;
				margin: 0;
				padding: 0;
				display: grid;
				gap: 0.9rem;
			}

			.timeline li {
				display: grid;
				grid-template-columns: 0.9rem 1fr;
				gap: 0.6rem;
			}

			.dot {
				width: 0.75rem;
				height: 0.75rem;
				border-radius: 50%;
				margin-top: 0.25rem;
				background: #0ea5e9;
			}

			@media (max-width: 992px) {
				.kpi-grid {
					grid-template-columns: repeat(2, minmax(0, 1fr));
				}

				.content-grid {
					grid-template-columns: 1fr;
				}

				.directory-grid { grid-template-columns: 1fr; }
			}

			@media (max-width: 560px) {
				.kpi-grid,
				.action-grid {
					grid-template-columns: 1fr;
				}
			}
		`
	]
})
export class DashboardComponent {
	private readonly auth = inject(AuthService);
	private readonly dashboardService = inject(DashboardService);
	private readonly billingService = inject(BillingService);

	readonly subscription = signal<SubscriptionSummaryDto | null>(null);
	readonly summary = signal<DashboardSummary | null>(null);
	readonly employees = signal<EmployeeList[]>([]);
	readonly employeeTotal = signal(0);
	readonly users = signal<ManagedUser[]>([]);
	readonly departments = signal<Department[]>([]);
	private readonly employeeService = inject(EmployeeService);
	private readonly userService = inject(UserService);
	private readonly departmentService = inject(DepartmentService);

	constructor() {
		const companyId = this.auth.companyId();
		if (companyId) {
			this.dashboardService.getSummary(companyId).subscribe(summary => this.summary.set(summary));
			this.employeeService.getAll(companyId, 1, 100).subscribe(page => { this.employees.set(page.items); this.employeeTotal.set(page.totalCount); });
			this.userService.getAll(companyId).subscribe(users => this.users.set(users));
			this.departmentService.getAll(companyId).subscribe(departments => this.departments.set(departments));
			this.billingService.getMine().subscribe(sub => this.subscription.set(sub));
		}
	}

	canManageBilling(): boolean {
		return this.auth.isInRole('Admin') || this.auth.isInRole('SuperAdmin');
	}

	readonly billingBanner = computed(() => {
		const sub = this.subscription();
		if (!sub) return null;

		if (!sub.canAddEmployee) {
			return { tone: 'danger' as const, message: `You've reached your plan's employee limit (${sub.maxEmployees}).` };
		}

		if (sub.status === 'FreeTrial' && sub.trialEndDate) {
			const daysLeft = Math.ceil((new Date(sub.trialEndDate).getTime() - Date.now()) / (1000 * 60 * 60 * 24));
			if (daysLeft <= 7) {
				return { tone: 'warning' as const, message: daysLeft <= 0 ? 'Your free month has ended.' : `Your free month ends in ${daysLeft} day${daysLeft === 1 ? '' : 's'}.` };
			}
		}

		if (sub.status === 'PastDue' || sub.status === 'Expired' || sub.status === 'Suspended') {
			return { tone: 'danger' as const, message: 'Your subscription is inactive.' };
		}

		return null;
	});

	readonly kpis = computed(() => {
		const s = this.summary();
		return [
			{ label: 'Net Payroll (Current Period)', value: `R ${(s?.currentPeriodTotalNet ?? 0).toLocaleString('en-ZA', { maximumFractionDigits: 0 })}`, delta: s?.currentPeriodStatus ?? 'No period yet', icon: 'bi-currency-exchange', deltaClass: 'text-success' },
			{ label: 'Employees Paid', value: `${s?.currentPeriodEmployeeCount ?? 0}`, delta: `${s?.activeEmployees ?? 0} active employees`, icon: 'bi-people-fill', deltaClass: 'text-warning' },
			{ label: 'Leave Pending', value: `${s?.pendingLeaveRequests ?? 0}`, delta: `${s?.onLeaveEmployees ?? 0} employees on leave`, icon: 'bi-calendar-check', deltaClass: 'text-warning' },
			{ label: 'YTD Net Pay', value: `R ${(s?.ytdNet ?? 0).toLocaleString('en-ZA', { maximumFractionDigits: 0 })}`, delta: 'Year to date', icon: 'bi-shield-check', deltaClass: 'text-success' }
		];
	});

	readonly timeline = computed(() => {
		const s = this.summary();
		if (!s?.currentPeriodId) return [];
		return [
			{ title: `Current payroll period status: ${s.currentPeriodStatus}`, when: `${s.currentPeriodEmployeeCount} employees` }
		];
	});
}
