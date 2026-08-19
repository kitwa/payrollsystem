import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';

@Component({
	selector: 'app-self-service-home',
	standalone: true,
	imports: [CommonModule, RouterLink],
	template: `
		<section class="mb-3">
			<h1 class="h3 mb-1">Employee Self-Service</h1>
			<p class="text-muted mb-0">Access your payslips, leave balance, and personal information updates.</p>
		</section>

		<section class="row g-3 mb-3">
			@for (card of summaryCards; track card.label) {
				<div class="col-12 col-md-4">
					<article class="card border-0 shadow-sm h-100">
						<div class="card-body">
							<p class="text-muted mb-1">{{ card.label }}</p>
							<h2 class="h4 mb-0">{{ card.value }}</h2>
						</div>
					</article>
				</div>
			}
		</section>

		<section class="row g-3">
			<div class="col-12 col-lg-6">
				<article class="card border-0 shadow-sm h-100">
					<div class="card-body">
						<h2 class="h5 mb-3">Quick Actions</h2>
						<div class="d-grid gap-2">
							<a class="btn btn-outline-dark" routerLink="/payslips">View Payslips</a>
							<a class="btn btn-outline-dark" routerLink="/leave/request">Request Leave</a>
							<button class="btn btn-outline-secondary" type="button">Update Banking Details</button>
						</div>
					</div>
				</article>
			</div>

			<div class="col-12 col-lg-6">
				<article class="card border-0 shadow-sm h-100">
					<div class="card-body">
						<h2 class="h5 mb-3">Latest Notifications</h2>
						<ul class="list-group list-group-flush">
							<li class="list-group-item px-0">August payslip is now available.</li>
							<li class="list-group-item px-0">Leave request #LV-01 awaiting manager approval.</li>
							<li class="list-group-item px-0">Tax certificate preview available for review.</li>
						</ul>
					</div>
				</article>
			</div>
		</section>
	`
})
export class SelfServiceHomeComponent {
	readonly summaryCards = [
		{ label: 'Leave Balance', value: '8.5 days' },
		{ label: 'Next Payday', value: '29 Aug 2026' },
		{ label: 'Latest Net Pay', value: 'R 41,520' }
	];
}
