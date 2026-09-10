import { Component, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { SettingsService } from '../../services/settings.service';
import { TaxYearDetail } from '../../models/settings.models';
import { PaginationComponent } from '../../../../shared/components/pagination/pagination.component';

@Component({
	selector: 'app-tax-tables',
	standalone: true,
	imports: [CommonModule, PaginationComponent],
	template: `
		<section class="d-flex justify-content-between align-items-end flex-wrap gap-2 mb-3">
			<div>
				<h1 class="h3 mb-1">Tax Tables</h1>
				<p class="text-muted mb-0">Maintain PAYE bracket definitions and effective tax year updates.</p>
			</div>
			<div>
				<label class="form-label">Tax Year</label>
				<input class="form-control" [value]="taxYear()?.year ?? ''" readonly>
			</div>
		</section>

		<section class="card border-0 shadow-sm">
			<div class="card-body">
				<div class="table-responsive">
					<table class="table align-middle mb-0">
						<thead>
							<tr><th>Taxable Income From</th><th>Taxable Income To</th><th>Base Tax</th><th>Rate</th></tr>
						</thead>
						<tbody>
							@for (bracket of pagedTaxTables(); track bracket.id) {
								<tr>
									<td>R {{ bracket.incomeFrom | number:'1.0-0' }}</td>
									<td>R {{ bracket.incomeTo | number:'1.0-0' }}</td>
									<td>R {{ bracket.baseTax | number:'1.0-0' }}</td>
									<td>{{ bracket.marginalRate }}%</td>
								</tr>
							} @empty {
								<tr><td colspan="4" class="text-center text-muted py-4">No tax tables loaded.</td></tr>
							}
						</tbody>
					</table>
				</div>
				<app-pagination [page]="pageNumber()" [pageSize]="pageSize" [total]="taxYear()?.taxTables?.length ?? 0" (pageChange)="pageNumber.set($event)"></app-pagination>
			</div>
		</section>
	`
})
export class TaxTablesComponent {
	private readonly settingsService = inject(SettingsService);

	readonly taxYear = signal<TaxYearDetail | null>(null);
	readonly pageNumber = signal(1);
	readonly pageSize = 20;
	readonly pagedTaxTables = computed(() => (this.taxYear()?.taxTables ?? []).slice((this.pageNumber() - 1) * this.pageSize, this.pageNumber() * this.pageSize));

	constructor() {
		this.settingsService.getActiveTaxYear().subscribe(year => this.taxYear.set(year));
	}
}

