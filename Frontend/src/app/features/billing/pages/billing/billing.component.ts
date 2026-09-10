import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { BillingService } from '../../services/billing.service';
import { PlanDto, SubscriptionSummaryDto } from '../../models/billing.models';

@Component({
  selector: 'app-billing',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <section class="mb-3">
      <p class="text-uppercase small fw-bold text-muted mb-1">Billing</p>
      <h1 class="h3 mb-1">Subscription &amp; Plan</h1>
      <p class="text-muted mb-0">Manage your plan, see how many employees you can add, and upgrade as your team grows.</p>
    </section>

    @if (error()) { <div class="alert alert-danger">{{ error() }}</div> }
    @if (message()) { <div class="alert alert-success">{{ message() }}</div> }

    @if (subscription(); as sub) {
      <section class="card border-0 shadow-sm mb-4">
        <div class="card-body">
          <div class="d-flex justify-content-between align-items-start flex-wrap gap-3">
            <div>
              <span class="badge mb-2" [class.bg-success]="sub.status === 'FreeTrial' || sub.status === 'Active'"
                    [class.bg-danger]="sub.status === 'Expired' || sub.status === 'Cancelled' || sub.status === 'PastDue' || sub.status === 'Suspended'">
                {{ statusLabel(sub.status) }}
              </span>
              <h2 class="h4 mb-1">{{ sub.planName }}</h2>
              <p class="text-muted mb-0">
                {{ sub.currentEmployeeCount }} of {{ sub.maxEmployees ?? 'unlimited' }} employees used
              </p>
              @if (sub.status === 'FreeTrial' && sub.trialEndDate) {
                <p class="text-muted small mb-0">Your free month ends on {{ sub.trialEndDate | date:'longDate' }}.</p>
              }
              @if (sub.currentPeriodEnd) {
                <p class="text-muted small mb-0">Current period ends on {{ sub.currentPeriodEnd | date:'longDate' }}.</p>
              }
            </div>
            <div class="text-end">
              <div class="h4 mb-0">{{ sub.monthlyPrice != null ? ('R ' + sub.monthlyPrice) : 'Contact Sales' }}</div>
              @if (sub.monthlyPrice != null) { <small class="text-muted">per month</small> }
            </div>
          </div>

          @if (!sub.canAddEmployee) {
            <div class="alert alert-warning mt-3 mb-0">
              You've reached your plan's employee limit.
              @if (sub.recommendedPlanCode) {
                Upgrade to keep adding employees.
              }
            </div>
          }

          @if (sub.status !== 'Cancelled' && sub.planCode !== 'FREE_TRIAL') {
            <div class="mt-3">
              <button class="btn btn-outline-danger btn-sm" type="button" (click)="cancelSubscription()">Cancel Subscription</button>
            </div>
          }
        </div>
      </section>
    }

    <section>
      <h2 class="h5 mb-3">Available Plans</h2>
      <div class="plan-grid">
        @for (plan of plans(); track plan.code) {
          <article class="plan-card" [class.plan-card--current]="subscription()?.planCode === plan.code"
                    [class.plan-card--recommended]="subscription()?.recommendedPlanCode === plan.code">
            @if (subscription()?.recommendedPlanCode === plan.code) {
              <span class="plan-card__ribbon">Recommended</span>
            }
            <h3 class="h6 mb-1">{{ plan.name }}</h3>
            <p class="text-muted small mb-2">
              {{ plan.minEmployees }}{{ plan.maxEmployees ? ' - ' + plan.maxEmployees : '+' }} employees
            </p>
            <div class="h4 mb-3">{{ plan.monthlyPrice != null ? ('R ' + plan.monthlyPrice) : 'Contact Sales' }}</div>
            @if (subscription()?.planCode === plan.code) {
              <button class="btn btn-outline-secondary btn-sm w-100" type="button" disabled>Current Plan</button>
            } @else if (plan.isContactSales) {
              <a class="btn btn-dark btn-sm w-100" routerLink="/support/tickets/new">Contact Sales</a>
            } @else {
              <button class="btn btn-dark btn-sm w-100" type="button" [disabled]="upgrading()" (click)="upgrade(plan.code)">
                {{ upgrading() === plan.code ? 'Upgrading...' : 'Choose Plan' }}
              </button>
            }
          </article>
        }
      </div>
    </section>
  `,
  styles: [`
    .plan-grid { display:grid; grid-template-columns:repeat(auto-fill, minmax(220px, 1fr)); gap:1rem; }
    .plan-card { position:relative; background:#fff; border:1px solid #e7e5e4; border-radius:1rem; padding:1.25rem; display:flex; flex-direction:column; }
    .plan-card--current { border-color:#0d6efd; }
    .plan-card--recommended { border-color:#198754; }
    .plan-card__ribbon { position:absolute; top:.6rem; right:.6rem; background:#198754; color:#fff; font-size:.7rem; padding:.15rem .5rem; border-radius:1rem; }
  `]
})
export class BillingComponent {
  private readonly billingService = inject(BillingService);

  readonly subscription = signal<SubscriptionSummaryDto | null>(null);
  readonly plans = signal<PlanDto[]>([]);
  readonly error = signal('');
  readonly message = signal('');
  readonly upgrading = signal<string | null>(null);

  constructor() {
    this.load();
  }

  statusLabel(status: string): string {
    switch (status) {
      case 'FreeTrial': return 'Free Trial';
      case 'PastDue': return 'Past Due';
      default: return status;
    }
  }

  upgrade(planCode: string): void {
    this.error.set('');
    this.message.set('');
    this.upgrading.set(planCode);
    this.billingService.upgrade({ planCode }).subscribe({
      next: sub => {
        this.subscription.set(sub);
        this.message.set('Your plan has been updated.');
        this.upgrading.set(null);
      },
      error: response => {
        this.error.set(response.error?.errors?.[0] ?? 'Unable to change plan.');
        this.upgrading.set(null);
      }
    });
  }

  cancelSubscription(): void {
    this.error.set('');
    this.message.set('');
    this.billingService.cancel().subscribe({
      next: () => {
        this.message.set('Your subscription has been cancelled.');
        this.load();
      },
      error: response => this.error.set(response.error?.errors?.[0] ?? 'Unable to cancel subscription.')
    });
  }

  private load(): void {
    this.billingService.getMine().subscribe({
      next: sub => this.subscription.set(sub),
      error: response => this.error.set(response.error?.errors?.[0] ?? 'Unable to load subscription.')
    });
    this.billingService.getPlans().subscribe({
      next: plans => this.plans.set(plans),
      error: () => {}
    });
  }
}
