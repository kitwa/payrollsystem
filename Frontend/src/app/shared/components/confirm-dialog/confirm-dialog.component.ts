import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';

export interface ConfirmOptions {
	title: string;
	message: string;
	confirmLabel?: string;
	cancelLabel?: string;
	tone?: 'default' | 'danger';
}

@Component({
	selector: 'app-confirm-dialog',
	standalone: true,
	imports: [CommonModule],
	template: `
		@if (open()) {
			<div class="confirm-backdrop" (click)="cancel()">
				<div class="confirm-card" (click)="$event.stopPropagation()">
					<h2 class="h5 mb-2">{{ options()?.title }}</h2>
					<p class="text-muted mb-4">{{ options()?.message }}</p>
					<div class="d-flex justify-content-end gap-2">
						<button type="button" class="btn btn-outline-secondary" (click)="cancel()">
							{{ options()?.cancelLabel ?? 'Cancel' }}
						</button>
						<button
							type="button"
							class="btn"
							[class.btn-dark]="options()?.tone !== 'danger'"
							[class.btn-danger]="options()?.tone === 'danger'"
							(click)="confirm()"
						>
							{{ options()?.confirmLabel ?? 'Confirm' }}
						</button>
					</div>
				</div>
			</div>
		}
	`,
	styles: [
		`
			.confirm-backdrop {
				position: fixed;
				inset: 0;
				background: rgba(15, 23, 42, 0.45);
				display: grid;
				place-items: center;
				z-index: 1050;
				padding: 1rem;
			}

			.confirm-card {
				background: #fff;
				border-radius: 1rem;
				padding: 1.5rem;
				width: min(28rem, 100%);
				box-shadow: 0 20px 45px rgba(15, 23, 42, 0.25);
			}
		`
	]
})
export class ConfirmDialogComponent {
	readonly open = signal(false);
	readonly options = signal<ConfirmOptions | null>(null);
	private resolveFn: ((confirmed: boolean) => void) | null = null;

	show(options: ConfirmOptions): Promise<boolean> {
		this.options.set(options);
		this.open.set(true);
		return new Promise(resolve => {
			this.resolveFn = resolve;
		});
	}

	confirm(): void {
		this.close(true);
	}

	cancel(): void {
		this.close(false);
	}

	private close(result: boolean): void {
		this.open.set(false);
		this.resolveFn?.(result);
		this.resolveFn = null;
	}
}
