import { Component, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../../core/auth/auth.service';
import { DashboardService } from '../services/dashboard.service';
import { DashboardSummary } from '../models/dashboard.models';

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
					<a class="action-card" routerLink="/leave/request">
						<i class="bi bi-calendar-plus"></i>
						<span>Capture Leave</span>
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

	readonly summary = signal<DashboardSummary | null>(null);

	constructor() {
		const companyId = this.auth.companyId();
		if (companyId) {
			this.dashboardService.getSummary(companyId).subscribe(summary => this.summary.set(summary));
		}
	}

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
