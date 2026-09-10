import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { SupportTicketService } from '../../services/support-ticket.service';
import { SupportTicketType } from '../../models/support-ticket.models';

@Component({
  selector: 'app-ticket-new',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  template: `
    <section class="d-flex justify-content-between align-items-start flex-wrap gap-2 mb-3">
      <div><p class="text-muted mb-1">Support</p><h1 class="h3 mb-1">New Support Ticket</h1><p class="text-muted mb-0">Tell us what you need help with.</p></div>
      <a class="btn btn-outline-secondary" routerLink="/support/tickets">Back to Tickets</a>
    </section>
    @if (error()) { <div class="alert alert-danger">{{ error() }}</div> }
    <section class="card border-0 shadow-sm"><div class="card-body">
      <form class="row g-3" [formGroup]="form" (ngSubmit)="submit()">
        <div class="col-12 col-md-8"><label class="form-label">Subject</label><input class="form-control" formControlName="subject" maxlength="200"></div>
        <div class="col-12 col-md-4"><label class="form-label">Ticket type</label><select class="form-select" formControlName="type">@for (type of types; track type.value) { <option [ngValue]="type.value">{{ type.label }}</option> }</select></div>
        <div class="col-12"><label class="form-label">Description</label><textarea class="form-control" rows="8" formControlName="description" placeholder="Describe the issue or question"></textarea></div>
        <div class="col-12 d-flex justify-content-end"><button class="btn btn-dark" type="submit" [disabled]="form.invalid || submitting()">{{ submitting() ? 'Submitting…' : 'Submit Ticket' }}</button></div>
      </form>
    </div></section>
  `
})
export class TicketNewComponent {
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);
  private readonly ticketService = inject(SupportTicketService);
  readonly submitting = signal(false);
  readonly error = signal('');
  readonly types = [
    { value: SupportTicketType.TechnicalIssue, label: 'Technical Issue' }, { value: SupportTicketType.BugReport, label: 'Bug Report' }, { value: SupportTicketType.FeatureRequest, label: 'Feature Request' }, { value: SupportTicketType.PayrollQuestion, label: 'Payroll Question' }, { value: SupportTicketType.Other, label: 'Other' }
  ];
  readonly form = this.fb.group({ subject: ['', [Validators.required, Validators.maxLength(200)]], type: [SupportTicketType.TechnicalIssue, Validators.required], description: ['', [Validators.required, Validators.maxLength(10000)]] });

  submit(): void {
    if (this.form.invalid || this.submitting()) return;
    this.submitting.set(true);
    const value = this.form.getRawValue();
    this.ticketService.create({ subject: value.subject!.trim(), type: value.type!, description: value.description!.trim() }).subscribe({
      next: ticket => this.router.navigate(['/support/tickets', ticket.id]),
      error: response => { this.error.set(response.error?.errors?.[0] ?? 'Unable to submit support ticket.'); this.submitting.set(false); }
    });
  }
}
