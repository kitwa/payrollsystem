import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-landing',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './landing.component.html',
  styleUrl: './landing.component.scss'
})
export class LandingComponent {
  /** Used in the footer copyright line. */
  currentYear = new Date().getFullYear();

  /**
   * Displayed monthly price on the pricing card, shown after the free month.
   * TODO: replace with your actual monthly price, e.g. 'R149.00'.
   */
  monthlyPrice = 'RXX.XX';
}