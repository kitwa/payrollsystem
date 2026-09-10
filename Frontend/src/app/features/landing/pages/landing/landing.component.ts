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
  currentYear = new Date().getFullYear();

  pricingPlans = [
    { range: '1–5 employees', price: 'R49', description: 'A simple starting point for small teams.', highlight: 'Best entry price' },
    { range: '6–10 employees', price: 'R89', description: 'Keep growing without a complicated plan.' },
    { range: '11–20 employees', price: 'R169', description: 'A practical fit for growing businesses.', highlight: 'Great for growing teams' },
    { range: '21–30 employees', price: 'R259', description: 'More room for an expanding payroll.' },
    { range: '31–40 employees', price: 'R349', description: 'Straightforward payroll for a busy team.' },
    { range: '41–50 employees', price: 'R449', description: 'Keep payroll organized as your team grows.' },
    { range: '51–75 employees', price: 'R599', description: 'Support a larger team with clear pricing.' },
    { range: '76–100 employees', price: 'R799', description: 'A reliable plan for established teams.' },
    { range: '101–150 employees', price: 'R999', description: 'Payroll that scales with your company.' },
    { range: '151–200 employees', price: 'R1,299', description: 'More employees, still one clear monthly price.' },
    { range: '201–300 employees', price: 'R1,599', description: 'Built for larger payroll teams.' },
    { range: '301–500 employees', price: 'R1,999', description: 'A transparent plan for substantial teams.' },
    { range: '500+ employees', price: 'Contact us', description: 'Let’s discuss the right fit for your company.', contact: true }
  ];
}