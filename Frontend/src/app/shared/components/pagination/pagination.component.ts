import { Component, EventEmitter, Input, Output } from '@angular/core';

@Component({
  selector: 'app-pagination',
  standalone: true,
  template: `
    @if (total > 0) {
      <div class="d-flex justify-content-between align-items-center gap-3 mt-3 flex-wrap">
        <small class="text-muted">Showing {{ rangeStart }}–{{ rangeEnd }} of {{ total }}</small>
        <div class="d-flex align-items-center gap-2">
          <button class="btn btn-sm btn-outline-secondary" type="button" [disabled]="page <= 1" (click)="pageChange.emit(page - 1)">Previous</button>
          <span class="small text-muted">Page {{ page }} of {{ totalPages }}</span>
          <button class="btn btn-sm btn-outline-secondary" type="button" [disabled]="page >= totalPages" (click)="pageChange.emit(page + 1)">Next</button>
        </div>
      </div>
    }
  `
})
export class PaginationComponent {
  @Input() page = 1;
  @Input() pageSize = 20;
  @Input() total = 0;
  @Output() pageChange = new EventEmitter<number>();

  get totalPages(): number { return Math.max(1, Math.ceil(this.total / this.pageSize)); }
  get rangeStart(): number { return this.total === 0 ? 0 : (this.page - 1) * this.pageSize + 1; }
  get rangeEnd(): number { return Math.min(this.page * this.pageSize, this.total); }
}
