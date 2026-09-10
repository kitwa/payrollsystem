import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { SeoPage, SeoService } from '../../../core/seo/seo.service';

type PageContent = SeoPage & { eyebrow: string; intro: string; sections: { title: string; body: string }[]; links: { label: string; path: string }[] };

const pages: Record<string, PageContent> = {
  blog: {
    title: 'South African Payroll Guides | Payroll SA Blog',
    description: 'Practical South African payroll guides covering PAYE, UIF, SDL, payslips, leave, payroll compliance and small business payroll.',
    path: '/blog', eyebrow: 'Payroll SA blog', intro: 'Useful payroll reading for South African employers, payroll administrators and growing businesses.',
    sections: [{ title: 'Start with the payroll fundamentals', body: 'Explore practical explanations of PAYE, UIF, SDL, payroll processing, payslips and employee leave. Each guide links to the relevant Payroll SA product page where useful.' }, { title: 'Topics we are building', body: 'Upcoming guides cover payroll software for small businesses, payroll compliance, cloud payroll compared with Excel, employee self-service, POPIA and keeping payroll information secure.' }], links: [{ label: 'How to calculate PAYE', path: '/blog/how-to-calculate-paye-in-south-africa' }, { label: 'What is UIF?', path: '/blog/what-is-uif' }, { label: 'Payroll software for small businesses', path: '/blog/payroll-software-for-small-businesses-in-south-africa' }, { label: 'Payroll software features', path: '/features' }], breadcrumbs: [{ name: 'Home', path: '/' }, { name: 'Blog', path: '/blog' }]
  },
  features: {
    title: 'Payroll Software Features | PAYE, UIF, SDL & Payslips | Payroll SA',
    description: 'Explore payroll software features for South African businesses, including PAYE, UIF, SDL, payslips, leave and employee management.',
    path: '/features', eyebrow: 'Payroll software features', intro: 'Payroll SA brings the everyday work of South African payroll into one clear online system.',
    sections: [
      { title: 'Payroll processing for South African businesses', body: 'Calculate salaries, earnings and deductions while keeping payroll history organised for each pay period.' },
      { title: 'PAYE, UIF and SDL support', body: 'Capture the payroll inputs used for employee tax, UIF and Skills Development Levy calculations. Review the results before completing a payroll run.' },
      { title: 'Payslips, leave and employees', body: 'Manage employee records, produce payslips, track leave and give employees access to their own payroll information.' }
    ], links: [{ label: 'Online payroll system', path: '/payroll' }, { label: 'Payslip software', path: '/features/payslips' }, { label: 'Leave management', path: '/features/leave-management' }], breadcrumbs: [{ name: 'Home', path: '/' }, { name: 'Features', path: '/features' }], faqs: [{ question: 'Can small businesses use Payroll SA?', answer: 'Yes. Payroll SA includes plans designed for small South African teams and scales as employee numbers grow.' }, { question: 'Can employees access payslips?', answer: 'Employees can use the self-service area to access payslips and relevant leave information when enabled for their account.' }]
  },
  pricing: {
    title: 'Payroll Software Pricing South Africa | Payroll SA',
    description: 'See transparent payroll software pricing for South African businesses, from small teams to larger payroll departments.',
    path: '/pricing', eyebrow: 'Payroll software pricing South Africa', intro: 'Choose a payroll plan based on the number of employees you manage, with a free first month and clear monthly tiers.',
    sections: [{ title: 'Plans that follow your team size', body: 'Payroll SA pricing is organised around employee count so smaller businesses can start simply and move to a larger tier when their payroll grows.' }, { title: 'What is included', body: 'Plans support payroll processing, employee records, payslips, leave workflows and payroll reporting. Review the plan details before upgrading.' }], links: [{ label: 'View all payroll features', path: '/features' }, { label: 'Register your company', path: '/register' }], breadcrumbs: [{ name: 'Home', path: '/' }, { name: 'Pricing', path: '/pricing' }]
  },
  payroll: {
    title: 'Online Payroll System South Africa | Payroll SA',
    description: 'Run payroll online for South African businesses with salary calculations, deductions, PAYE, UIF, SDL, payslips and payroll history.',
    path: '/payroll', eyebrow: 'Online payroll system South Africa', intro: 'Process payroll with a practical workflow for salaries, deductions, payslips and reporting.',
    sections: [{ title: 'A clear payroll workflow', body: 'Set up employees, select a payroll period, review earnings and deductions, then generate payroll records and payslips.' }, { title: 'Built around South African payroll', body: 'Payroll SA includes support for PAYE, UIF and SDL inputs, with payroll history that helps your team review previous periods.' }, { title: 'Useful records for your team', body: 'Keep employee data, payroll results and payslips together so payroll administrators spend less time searching across spreadsheets.' }], links: [{ label: 'Learn about PAYE', path: '/payroll/paye' }, { label: 'Learn about UIF', path: '/payroll/uif' }, { label: 'See pricing', path: '/pricing' }], breadcrumbs: [{ name: 'Home', path: '/' }, { name: 'Payroll', path: '/payroll' }], faqs: [{ question: 'Is this an official SARS calculator?', answer: 'No. Payroll SA is payroll management software and should be reviewed alongside current SARS guidance and professional advice.' }]
  },
  paye: {
    title: 'PAYE Calculator and Payroll Software South Africa | Payroll SA',
    description: 'Understand PAYE in South Africa and see how payroll software can help employers manage employee tax calculations and payroll records.',
    path: '/payroll/paye', eyebrow: 'PAYE payroll software', intro: 'PAYE is the tax employers deduct from remuneration and pay to SARS on behalf of employees.',
    sections: [{ title: 'How PAYE fits into payroll', body: 'PAYE depends on taxable remuneration, the applicable tax year and employee information. Payroll software helps keep the inputs and calculated deductions together for review.' }, { title: 'Use current guidance', body: 'Tax thresholds, rebates and rates can change. Payroll SA is not an official SARS calculator, so employers should validate payroll settings against current SARS guidance.' }], links: [{ label: 'Run payroll online', path: '/payroll' }, { label: 'Learn about UIF', path: '/payroll/uif' }, { label: 'Start with Payroll SA', path: '/register' }], breadcrumbs: [{ name: 'Home', path: '/' }, { name: 'Payroll', path: '/payroll' }, { name: 'PAYE', path: '/payroll/paye' }], faqs: [{ question: 'What is PAYE?', answer: 'PAYE is Pay-As-You-Earn tax deducted by an employer from employee remuneration and paid to SARS.' }]
  },
  uif: {
    title: 'UIF Payroll South Africa | UIF Payroll Software | Payroll SA',
    description: 'Learn how UIF fits into South African payroll and manage UIF-related payroll inputs with Payroll SA software.',
    path: '/payroll/uif', eyebrow: 'UIF payroll South Africa', intro: 'UIF contributions help support eligible employees during periods such as unemployment, illness or parental leave.',
    sections: [{ title: 'UIF in the payroll process', body: 'Employers need accurate employee and remuneration information when calculating and recording UIF contributions. Payroll software reduces repeated manual work.' }, { title: 'Review contribution settings', body: 'Contribution ceilings and rules can change. Keep settings current and review outputs against official guidance before submission.' }], links: [{ label: 'Online payroll system', path: '/payroll' }, { label: 'Learn about SDL', path: '/payroll/sdl' }, { label: 'View pricing', path: '/pricing' }], breadcrumbs: [{ name: 'Home', path: '/' }, { name: 'Payroll', path: '/payroll' }, { name: 'UIF', path: '/payroll/uif' }], faqs: [{ question: 'Does Payroll SA submit UIF automatically?', answer: 'Payroll SA helps manage payroll calculations and records; employers remain responsible for reviewing and submitting statutory information through the appropriate channels.' }]
  },
  sdl: {
    title: 'SDL Payroll South Africa | Skills Development Levy | Payroll SA',
    description: 'Understand Skills Development Levy in South Africa and manage SDL payroll inputs with Payroll SA.',
    path: '/payroll/sdl', eyebrow: 'SDL payroll South Africa', intro: 'The Skills Development Levy supports skills development and forms part of payroll administration for qualifying employers.',
    sections: [{ title: 'Where SDL appears in payroll', body: 'SDL is an employer payroll cost that needs to be considered with the company payroll configuration and employee remuneration.' }, { title: 'Make payroll reviewable', body: 'A consistent payroll system helps teams review SDL-related calculations alongside PAYE, UIF, earnings and deductions.' }], links: [{ label: 'Explore payroll features', path: '/features' }, { label: 'Learn about PAYE', path: '/payroll/paye' }, { label: 'Register', path: '/register' }], breadcrumbs: [{ name: 'Home', path: '/' }, { name: 'Payroll', path: '/payroll' }, { name: 'SDL', path: '/payroll/sdl' }]
  },
  payslips: {
    title: 'Payslip Software South Africa | Online Payslips | Payroll SA',
    description: 'Generate and manage employee payslips online with payroll software designed for South African businesses.',
    path: '/features/payslips', eyebrow: 'Payslip software South Africa', intro: 'Give payroll teams and employees a dependable record of each completed payroll period.',
    sections: [{ title: 'Clear employee payslips', body: 'Create payslips from payroll results with earnings, deductions and net pay presented in one employee-facing record.' }, { title: 'Less manual distribution', body: 'Keep payslips available in the application so employees can access their records without relying on repeated email attachments.' }], links: [{ label: 'Run payroll online', path: '/payroll' }, { label: 'Employee self-service', path: '/features/employee-management' }, { label: 'View plans', path: '/pricing' }], breadcrumbs: [{ name: 'Home', path: '/' }, { name: 'Features', path: '/features' }, { name: 'Payslips', path: '/features/payslips' }]
  },
  leave: {
    title: 'Leave Management Software South Africa | Payroll SA',
    description: 'Manage employee leave requests, leave types and balances with online payroll software for South African businesses.',
    path: '/features/leave-management', eyebrow: 'Leave management software South Africa', intro: 'Bring leave requests, approvals and balances into the same system as your employee records and payroll.',
    sections: [{ title: 'A consistent leave process', body: 'Configure leave types, capture requests and keep approval decisions visible to the people responsible for payroll.' }, { title: 'Connect leave and payroll', body: 'Accurate leave balances and approved leave information help payroll teams prepare each pay period with fewer manual checks.' }], links: [{ label: 'Employee management', path: '/features/employee-management' }, { label: 'Payroll features', path: '/features' }, { label: 'Start free', path: '/register' }], breadcrumbs: [{ name: 'Home', path: '/' }, { name: 'Features', path: '/features' }, { name: 'Leave management', path: '/features/leave-management' }]
  },
  employees: {
    title: 'Employee Management Software South Africa | Payroll SA',
    description: 'Manage employee records, departments, payroll details and self-service access with South African employee payroll software.',
    path: '/features/employee-management', eyebrow: 'Employee payroll management', intro: 'Keep the employee information your payroll team uses in one controlled, searchable workspace.',
    sections: [{ title: 'Employee records built for payroll', body: 'Store employment details, departments, salary information and payroll-related data with company-level access controls.' }, { title: 'Support employee self-service', body: 'Give employees appropriate access to payslips and leave information while administrators retain control of payroll data.' }], links: [{ label: 'Payslip software', path: '/features/payslips' }, { label: 'Leave management', path: '/features/leave-management' }, { label: 'View pricing', path: '/pricing' }], breadcrumbs: [{ name: 'Home', path: '/' }, { name: 'Features', path: '/features' }, { name: 'Employee management', path: '/features/employee-management' }]
  },
  privacy: {
    title: 'Privacy Policy | Payroll SA', description: 'Read how Payroll SA handles account, employee and payroll information.', path: '/legal/privacy', eyebrow: 'Legal', intro: 'Payroll SA is designed to handle payroll information with care and controlled access.', sections: [{ title: 'Information we handle', body: 'We may process account, company, employee, payroll and support information needed to provide the service. Do not place sensitive information in public support messages.' }, { title: 'How information is protected', body: 'Access is controlled through authentication, role authorization, tenant isolation, logging and secure deployment practices. Configure production secrets outside source control.' }, { title: 'Your responsibilities', body: 'Keep user credentials private, review access regularly and contact support if you suspect unauthorized access.' }], links: [{ label: 'Contact support', path: '/contact' }], breadcrumbs: [{ name: 'Home', path: '/' }, { name: 'Privacy', path: '/legal/privacy' }]
  },
  terms: {
    title: 'Terms of Service | Payroll SA', description: 'Read the Payroll SA terms for using the online payroll platform.', path: '/legal/terms', eyebrow: 'Legal', intro: 'These terms describe the basic conditions for using Payroll SA.', sections: [{ title: 'Use of the service', body: 'Use the service lawfully and provide accurate company and user information. You are responsible for reviewing payroll outputs before statutory submission.' }, { title: 'Payroll and tax responsibility', body: 'Payroll SA provides software tools and does not replace professional tax, accounting or legal advice. Keep rates and settings current.' }, { title: 'Account security', body: 'Protect credentials and notify Payroll SA promptly about suspected unauthorized access.' }], links: [{ label: 'Privacy policy', path: '/legal/privacy' }, { label: 'Contact support', path: '/contact' }], breadcrumbs: [{ name: 'Home', path: '/' }, { name: 'Terms', path: '/legal/terms' }]
  },
  contact: {
    title: 'Contact Payroll SA | South African Payroll Support', description: 'Contact Payroll SA about payroll software, plans, support and sales questions.', path: '/contact', eyebrow: 'Contact Payroll SA', intro: 'Have a question about payroll software, plans or implementation? Send the team a support ticket.', sections: [{ title: 'Sales and plan questions', body: 'Ask about employee tiers, the best plan for your business or how Payroll SA supports your payroll workflow.' }, { title: 'Product support', body: 'Existing customers can submit a support ticket with the relevant context so the team can respond efficiently.' }], links: [{ label: 'Create a support ticket', path: '/support/tickets/new' }, { label: 'View pricing', path: '/pricing' }], breadcrumbs: [{ name: 'Home', path: '/' }, { name: 'Contact', path: '/contact' }]
  }
};

pages['about'] = { title: 'About Payroll SA | South African Payroll Software', description: 'Learn about Payroll SA, a payroll software platform for South African businesses.', path: '/about', eyebrow: 'About Payroll SA', intro: 'Payroll SA helps South African businesses organise employee information, payroll processing, payslips and leave in one online workspace.', sections: [{ title: 'Designed for practical payroll work', body: 'The product focuses on clear workflows for payroll administrators and employees, from employee setup through payroll history and payslips.' }, { title: 'South African context', body: 'The platform includes payroll concepts such as PAYE, UIF and SDL. Always review current statutory guidance and seek professional advice for complex cases.' }], links: [{ label: 'Explore features', path: '/features' }, { label: 'Contact Payroll SA', path: '/contact' }], breadcrumbs: [{ name: 'Home', path: '/' }, { name: 'About', path: '/about' }] };
pages['popia'] = { title: 'POPIA and Payroll Information | Payroll SA', description: 'Read practical information about protecting employee payroll information in line with responsible POPIA practices.', path: '/legal/popia', eyebrow: 'POPIA information', intro: 'Payroll data can include personal and financial information, so access, retention and secure handling matter.', sections: [{ title: 'Collect what is needed', body: 'Use employee and payroll information for clear, legitimate business purposes and avoid collecting unrelated information.' }, { title: 'Control access', body: 'Use roles, tenant boundaries, strong credentials and regular access reviews to limit payroll information to people who need it.' }, { title: 'Plan retention and incidents', body: 'Define retention practices, protect backups and maintain an incident response process. Obtain professional privacy advice for your specific obligations.' }], links: [{ label: 'Privacy policy', path: '/legal/privacy' }, { label: 'Security overview', path: '/contact' }], breadcrumbs: [{ name: 'Home', path: '/' }, { name: 'Legal', path: '/legal/popia' }] };
pages['cookies'] = { title: 'Cookie Policy | Payroll SA', description: 'Read how Payroll SA may use cookies and browser storage for essential application functions.', path: '/legal/cookies', eyebrow: 'Legal', intro: 'Payroll SA may use browser storage and essential cookies to keep the application working and remember authentication state.', sections: [{ title: 'Essential storage', body: 'Authentication and application preferences may require browser storage. Do not use shared or public devices for payroll administration.' }, { title: 'Optional analytics', body: 'Analytics is optional and should only be enabled through production configuration. Sensitive payroll and employee information must never be sent to analytics systems.' }], links: [{ label: 'Privacy policy', path: '/legal/privacy' }, { label: 'Contact support', path: '/contact' }], breadcrumbs: [{ name: 'Home', path: '/' }, { name: 'Cookies', path: '/legal/cookies' }] };
pages['refunds'] = { title: 'Refund Policy | Payroll SA', description: 'Read the Payroll SA refund and subscription support information.', path: '/legal/refunds', eyebrow: 'Legal', intro: 'Subscription and payment questions are handled through the support process so account and billing context can be reviewed.', sections: [{ title: 'Contact support first', body: 'Submit a support ticket with your company and subscription context. Do not include full card numbers, CVV values or banking credentials.' }, { title: 'Plan and billing review', body: 'Billing outcomes depend on the selected plan, payment provider and applicable agreement. Contact Payroll SA for the current terms that apply to your account.' }], links: [{ label: 'Contact support', path: '/contact' }, { label: 'View pricing', path: '/pricing' }], breadcrumbs: [{ name: 'Home', path: '/' }, { name: 'Refunds', path: '/legal/refunds' }] };

const articles: Record<string, PageContent> = {
  'how-to-calculate-paye-in-south-africa': { title: 'How to Calculate PAYE in South Africa | Payroll SA', description: 'A practical guide to understanding PAYE inputs, tax years and payroll review in South Africa.', path: '/blog/how-to-calculate-paye-in-south-africa', eyebrow: 'Payroll guide', intro: 'PAYE calculations depend on employee remuneration, tax-year rules and accurate payroll information.', sections: [{ title: 'Start with accurate payroll inputs', body: 'Capture remuneration, taxable benefits, deductions and employee information before calculating PAYE. Errors in source data produce errors in payroll.' }, { title: 'Use current tax-year guidance', body: 'Tax brackets, rebates and thresholds can change. Compare your payroll configuration with current SARS guidance and have unusual cases reviewed by a qualified adviser.' }, { title: 'Keep a review trail', body: 'A payroll system should make it possible to review the period, understand deductions and retain the resulting payslip and payroll history.' }], links: [{ label: 'PAYE payroll software', path: '/payroll/paye' }, { label: 'Online payroll system', path: '/payroll' }, { label: 'View pricing', path: '/pricing' }], breadcrumbs: [{ name: 'Home', path: '/' }, { name: 'Blog', path: '/blog' }, { name: 'How to calculate PAYE', path: '/blog/how-to-calculate-paye-in-south-africa' }], type: 'article', published: '2026-09-10' },
  'what-is-uif': { title: 'What Is UIF and How Does UIF Work? | Payroll SA', description: 'Understand UIF contributions and the role of accurate payroll records for South African employers.', path: '/blog/what-is-uif', eyebrow: 'Payroll guide', intro: 'UIF is part of the statutory payroll landscape for eligible South African employers and employees.', sections: [{ title: 'Why accurate records matter', body: 'Employee status, remuneration and contribution settings should be reviewed consistently so payroll records remain reliable.' }, { title: 'Review before submission', body: 'Use current official guidance for contribution rules and review payroll outputs before submitting statutory information.' }], links: [{ label: 'UIF payroll software', path: '/payroll/uif' }, { label: 'Payroll features', path: '/features' }, { label: 'Register', path: '/register' }], breadcrumbs: [{ name: 'Home', path: '/' }, { name: 'Blog', path: '/blog' }, { name: 'What is UIF?', path: '/blog/what-is-uif' }], type: 'article', published: '2026-09-10' },
  'payroll-software-for-small-businesses-in-south-africa': { title: 'Payroll Software for Small Businesses in South Africa | Payroll SA', description: 'How small businesses can choose practical payroll software for employees, payslips, leave and payroll compliance.', path: '/blog/payroll-software-for-small-businesses-in-south-africa', eyebrow: 'Small business payroll', intro: 'Small businesses need payroll tools that reduce manual repetition without making everyday work harder.', sections: [{ title: 'Look for a complete workflow', body: 'Employee records, payroll processing, payslips, leave and reports should work together instead of being maintained in disconnected spreadsheets.' }, { title: 'Choose transparent pricing', body: 'Employee-based pricing can make costs easier to understand as a small team grows. Review limits, support and included features before choosing a plan.' }], links: [{ label: 'Small business payroll pricing', path: '/pricing' }, { label: 'Payroll features', path: '/features' }, { label: 'Start your workspace', path: '/register' }], breadcrumbs: [{ name: 'Home', path: '/' }, { name: 'Blog', path: '/blog' }, { name: 'Payroll software for small businesses', path: '/blog/payroll-software-for-small-businesses-in-south-africa' }], type: 'article', published: '2026-09-10' }
};

@Component({
  selector: 'app-public-seo-page',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <main class="public-page">
      <nav class="breadcrumbs" aria-label="Breadcrumb">
        @for (crumb of content.breadcrumbs ?? []; track crumb.path; let last = $last) {
          @if (!last) { <a [routerLink]="crumb.path">{{ crumb.name }}</a><span aria-hidden="true">/</span> } @else { <span>{{ crumb.name }}</span> }
        }
      </nav>
      <p class="eyebrow">{{ content.eyebrow }}</p>
      <h1>{{ content.title.split(' | ')[0] }}</h1>
      <p class="lead">{{ content.intro }}</p>
      @for (section of content.sections; track section.title) {
        <section><h2>{{ section.title }}</h2><p>{{ section.body }}</p></section>
      }
      @if (content.faqs?.length) {
        <section><h2>Frequently asked questions</h2>
          @for (faq of content.faqs; track faq.question) { <details><summary>{{ faq.question }}</summary><p>{{ faq.answer }}</p></details> }
        </section>
      }
      <section class="page-links"><h2>Continue exploring</h2><div class="link-grid">@for (link of content.links; track link.path) { <a class="link-card" [routerLink]="link.path">{{ link.label }} <span aria-hidden="true">→</span></a> }</div></section>
      <a class="btn btn-dark" routerLink="/register">Start with Payroll SA</a>
    </main>
  `,
  styles: [`
    .public-page { max-width: 900px; margin: 0 auto; padding: 2rem 1rem 4rem; }
    .breadcrumbs { display:flex; gap:.55rem; font-size:.85rem; margin-bottom:2rem; color:#667085; }
    .breadcrumbs a { color:#0d6efd; text-decoration:none; }
    .eyebrow { color:#0f766e; font-size:.78rem; font-weight:700; letter-spacing:.08em; text-transform:uppercase; }
    h1 { max-width:760px; font-size:clamp(2rem,5vw,3.5rem); line-height:1.08; margin-bottom:1rem; }
    .lead { max-width:720px; font-size:1.2rem; color:#475467; margin-bottom:3rem; }
    section { border-top:1px solid #e4e7ec; padding:1.5rem 0; }
    section h2 { font-size:1.35rem; margin-bottom:.55rem; }
    section p { max-width:760px; color:#475467; line-height:1.7; }
    details { border-bottom:1px solid #e4e7ec; padding:1rem 0; } summary { cursor:pointer; font-weight:700; }
    details p { margin:.75rem 0 0; }
    .link-grid { display:grid; grid-template-columns:repeat(auto-fit,minmax(210px,1fr)); gap:.75rem; }
    .link-card { display:flex; justify-content:space-between; padding:1rem; border:1px solid #d0d5dd; border-radius:.6rem; color:#101828; text-decoration:none; }
    .link-card:hover { border-color:#0d6efd; background:#f8fbff; }
  `]
})
export class PublicSeoPageComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly seo = inject(SeoService);
  readonly content: PageContent;

  constructor() {
    const path = this.route.snapshot.url.map(segment => segment.path).join('/');
    const key = this.route.snapshot.data['seoKey'] ?? (path.startsWith('blog/') ? path.substring(5) : path);
    this.content = pages[key] ?? articles[key] ?? pages['features'];
    this.seo.update(this.content);
  }
}
