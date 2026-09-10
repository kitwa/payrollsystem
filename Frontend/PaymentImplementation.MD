I want you to implement a complete subscription, pricing, employee-limit, payment and company-management system in my Payroll SA application.

IMPORTANT:
Do not just explain what needs to be done.

Inspect the existing application architecture, database models, authentication/authorization, company/tenant structure, employee functionality and Super Admin functionality first.

Then implement the required functionality following the existing architecture and coding conventions.

==================================================
APPLICATION CONTEXT
==================================================

This is a South African payroll SaaS application.

The pricing model is based on the number of employees.

The customer gets their FIRST MONTH FREE with ALL FEATURES INCLUDED.

The free month is NOT a limited-feature trial.

However, during the free month the company is limited to a maximum of 5 employees.

After the free month, the company needs an active paid subscription.

==================================================
PRICING PLANS
==================================================

Implement the following pricing tiers:

PLAN 1
-------
Name:
Small Business

Employees:
1–5

Price:
R49/month

Maximum employees:
5


PLAN 2
-------
Name:
Growing Business

Employees:
6–10

Price:
R89/month

Maximum employees:
10


PLAN 3
-------
Name:
Growing Team

Employees:
11–20

Price:
R169/month

Maximum employees:
20


PLAN 4
-------
Name:
Business

Employees:
21–30

Price:
R259/month

Maximum employees:
30


PLAN 5
-------
Name:
Business Plus

Employees:
31–40

Price:
R349/month

Maximum employees:
40


PLAN 6
-------
Name:
Business Plus

Employees:
41–50

Price:
R449/month

Maximum employees:
50


PLAN 7
-------
Name:
Growing Company

Employees:
51–75

Price:
R599/month

Maximum employees:
75


PLAN 8
-------
Name:
Growing Company

Employees:
76–100

Price:
R799/month

Maximum employees:
100


PLAN 9
-------
Name:
Large Business

Employees:
101–150

Price:
R999/month

Maximum employees:
150


PLAN 10
--------
Name:
Large Business

Employees:
151–200

Price:
R1,299/month

Maximum employees:
200


PLAN 11
--------
Name:
Enterprise

Employees:
201–300

Price:
R1,599/month

Maximum employees:
300


PLAN 12
--------
Name:
Enterprise

Employees:
301–500

Price:
R1,999/month

Maximum employees:
500


PLAN 13
--------
Name:
Enterprise

Employees:
500+

Price:
Contact us

For the initial implementation, make the pricing plans configurable rather than hard-coded throughout the application.

Create a Plan/Pricing configuration structure in the database or appropriate configuration layer.

Do NOT duplicate employee limits and prices in multiple places.

There must be a single source of truth for pricing.

==================================================
FREE MONTH
==================================================

Every newly registered company gets:

Subscription status:
FREE_TRIAL

Trial duration:
1 month

Maximum employees:
5

Features:
ALL FEATURES ENABLED

Price:
R0

IMPORTANT:

The free trial must allow the customer to use the actual application.

Do NOT create a separate restricted "trial version".

The only restriction during the free month is the employee limit of 5.

Store the trial start date and trial end date.

For example:

TrialStartDate
TrialEndDate

Do not calculate the trial period only from frontend data.

The backend must be the source of truth.

Use server-side date/time.

==================================================
SUBSCRIPTION MODEL
==================================================

Create an appropriate subscription model/entity if one does not already exist.

For example:

CompanySubscription

Possible fields:

Id
CompanyId
PlanId
Status
TrialStartDate
TrialEndDate
CurrentPeriodStart
CurrentPeriodEnd
Price
Currency
PaymentProvider
PaymentCustomerReference
PaymentSubscriptionReference
PaymentMethodReference
CreatedAt
UpdatedAt
CancelledAt

Use appropriate names based on the existing project conventions.

Do not blindly create these exact fields if the application already has equivalent structures.

Statuses should support at least:

FREE_TRIAL
ACTIVE
PAST_DUE
CANCELLED
EXPIRED
SUSPENDED

Use an enum rather than magic strings where appropriate.

==================================================
EMPLOYEE LIMIT ENFORCEMENT
==================================================

This is extremely important.

The employee limit must be enforced on the BACKEND.

Do NOT rely only on Angular/frontend validation.

For example:

FREE_TRIAL:
Maximum 5 employees

R49:
Maximum 5 employees

R89:
Maximum 10 employees

R169:
Maximum 20 employees

etc.

Before creating an employee, the backend must:

1. Identify the company.
2. Retrieve its current subscription.
3. Determine the current plan.
4. Count the company's employees.
5. Compare the current employee count with the plan maximum.
6. If the company has reached the limit, reject the creation.
7. Return a clear response telling the frontend that an upgrade is required.

Example:

Current plan:
1–5 employees

Current employees:
5

Attempting to add:
1 employee

Result:

"Your current plan allows a maximum of 5 employees. Please upgrade your plan to add more employees."

HTTP 409 Conflict would be appropriate if consistent with the existing API conventions.

Do not allow users to bypass the limit through API calls.

==================================================
FRONTEND EMPLOYEE LIMIT
==================================================

The Angular application should also provide a good user experience.

When the company reaches its employee limit:

Show a clear message such as:

"You've reached your 5 employee limit."

Then:

"Upgrade your plan to add more employees."

Button:

"Upgrade Plan"

The button should navigate to the subscription/billing page.

However, remember:

Frontend validation is only for UX.

The backend remains the authoritative enforcement mechanism.

==================================================
CURRENT PLAN DISPLAY
==================================================

Add a Subscription/Billing area to the company dashboard.

The customer should immediately see:

CURRENT PLAN

Free Trial

or:

Small Business

1–5 employees

R49/month

For example:

----------------------------------
Your Plan

FREE TRIAL

5 / 5 employees

First month FREE

All features included

Trial ends:
25 September 2026

[ Upgrade Plan ]
----------------------------------

Once the company becomes paid:

----------------------------------
Your Plan

SMALL BUSINESS

1–5 employees

R49 / month

Employees:
4 / 5

Next billing date:
25 October 2026

[ Upgrade Plan ]
----------------------------------

Make the employee usage very clear.

For example:

Employees
4 / 5

Use a Bootstrap progress bar if appropriate.

==================================================
PLAN UPGRADE
==================================================

Implement the ability for a company administrator to upgrade their plan.

The upgrade flow should:

1. Show current plan.
2. Show available plans.
3. Show current employee count.
4. Highlight the recommended/required next plan.
5. Show the monthly price.
6. Allow the administrator to select a plan.
7. Redirect to payment if payment is required.
8. Update the subscription only after successful payment confirmation.

Example:

Current:

5 employees
Free Trial

Customer adds another employee.

System says:

"You have reached your 5 employee limit."

Available upgrade:

6–10 employees
R89/month

[ Upgrade to R89 ]

==================================================
AUTOMATIC PLAN RECOMMENDATION
==================================================

The system should determine the minimum plan required based on employee count.

For example:

1–5 → R49
6–10 → R89
11–20 → R169
21–30 → R259
etc.

Create a reusable service such as:

SubscriptionService
PlanService
PricingService

or use the application's existing architecture.

Do not duplicate this logic in Angular.

The backend should expose the current plan and available plans through an API.

==================================================
IMPORTANT: WHAT HAPPENS WHEN EMPLOYEES INCREASE
==================================================

If a company has:

5 employees
R49 plan

and tries to add employee number 6:

DO NOT automatically charge the customer without their knowledge unless the payment provider and subscription model explicitly support this behavior and it has been clearly communicated.

Instead:

Block the employee creation.

Show:

"Your current plan supports up to 5 employees."

"Upgrade to the 6–10 employee plan for R89/month."

[ Upgrade Plan ]

After successful upgrade/payment, employee creation can continue.

This keeps pricing transparent.

==================================================
WHAT HAPPENS WHEN EMPLOYEES DECREASE
==================================================

Do NOT automatically downgrade the customer simply because they remove employees.

For example:

Company is paying R169 for 11–20 employees.

They reduce to 9 employees.

Do not immediately downgrade them to R89.

Instead, allow the customer to choose to downgrade.

If they select downgrade:

Explain when the lower pricing will take effect.

Prefer applying downgrades at the next billing period rather than unexpectedly changing the current subscription.

This avoids billing surprises.

==================================================
PAYMENT GATEWAY
==================================================

Implement the application so that a payment gateway can be integrated cleanly.

The architecture must NOT tightly couple Payroll SA business logic to one payment provider.

Create an abstraction/interface such as:

IPaymentGateway

or equivalent according to the application's architecture.

It should support operations such as:

CreateCustomer
CreateSubscription
UpdateSubscription
CancelSubscription
GetSubscription
CreateCheckoutSession
HandleWebhook

Use the appropriate implementation for the payment provider once the provider is configured.

If the project already has a payment provider selected, use it.

If no provider has been selected yet, implement the abstraction and a clean placeholder/mock implementation rather than inventing credentials or pretending that payment is live.

==================================================
PAYMENT SECURITY
==================================================

VERY IMPORTANT:

DO NOT STORE:

- Full bank account numbers
- CVV
- Card numbers
- Card security codes
- Online banking passwords
- PINs
- Payment credentials
- Payment authentication secrets

Do not build our own card/payment form that stores sensitive payment information.

Payment information should be handled by the payment provider.

Store only non-sensitive references returned by the provider.

For example:

PaymentProvider
PaymentCustomerReference
PaymentSubscriptionReference
PaymentMethodReference
PaymentStatus
LastFourDigits (only if the provider safely returns this and it is useful)
CardBrand (if applicable)
BankName (if applicable and non-sensitive)
MandateReference (if applicable)
CreatedAt

Use provider tokens/references rather than raw payment details.

==================================================
BANK ACCOUNT INFORMATION
==================================================

I want to store some information about the company's payment/bank account, but NOT sensitive banking credentials.

Create an appropriate CompanyPaymentProfile or BillingProfile.

Potential safe information:

CompanyId
AccountHolderName
BankName
AccountType
AccountLast4
PaymentProvider
PaymentCustomerReference
PaymentMethodReference
MandateReference
CreatedAt
UpdatedAt

Only store the minimum necessary information.

DO NOT store:

Full bank account number
Internet banking username
Internet banking password
PIN
CVV
Card number
Security codes

If the payment gateway provides a token/reference, store that reference instead.

Clearly separate:

Company banking/business information

from:

Payment provider sensitive information.

==================================================
PAYMENT WEBHOOKS
==================================================

Payment status must NOT be trusted from the Angular frontend.

Implement a webhook endpoint for the payment provider.

The webhook should update subscription status after receiving trusted payment events.

Handle appropriate events such as:

Payment successful
Payment failed
Subscription activated
Subscription renewed
Subscription cancelled
Subscription expired

Use the actual event names from the selected provider if one is already configured.

Webhook processing must be:

- authenticated/verified
- idempotent
- safe against duplicate events
- logged appropriately

Never activate a paid subscription merely because the frontend says payment succeeded.

==================================================
SUBSCRIPTION EXPIRY
==================================================

When the free month expires:

If there is no active paid subscription:

The company should no longer be allowed to continue running payroll.

However, do not delete the company or employee data.

Set subscription status to:

EXPIRED

Show a clear message:

"Your free month has ended."

"Choose a plan to continue using Payroll SA."

[ Choose a Plan ]

Allow access to appropriate account/billing areas so they can subscribe.

Do not silently delete their data.

==================================================
PAID SUBSCRIPTION FAILURE
==================================================

If a payment fails:

Do not immediately delete the company.

Set the subscription to an appropriate state such as:

PAST_DUE

Show:

"We couldn't process your latest payment."

"Please update your payment method to continue using Payroll SA."

Provide:

[ Update Payment ]

Use a reasonable grace period if appropriate.

Do not hard-code an arbitrary grace period without making it configurable.

==================================================
COMPANY DISABLE / ENABLE
==================================================

I also want the SUPER ADMIN to be able to disable a company.

Add this capability to the Super Admin area.

The Super Admin should see:

Company
Status
Plan
Employee Count
Subscription Status
Trial End
Created Date
Actions

Actions:

View
Disable
Enable

When disabling a company, show a confirmation dialog.

Example:

"Disable Company?"

"Are you sure you want to disable this company? Users from this company will no longer be able to use the Payroll SA application."

Require explicit confirmation.

Do not delete any data.

==================================================
DISABLED COMPANY BEHAVIOUR
==================================================

When a company is disabled:

Company.IsActive = false

or use the application's existing status model.

All company users should be prevented from using normal application functionality.

The backend MUST enforce this.

Do not rely only on hiding buttons in Angular.

For authenticated requests:

If the user's company is disabled:

Return an appropriate response such as:

403 Forbidden

with a clear error code/message.

Example:

"Your company account has been disabled. Please contact support."

The Angular application should display a clear disabled-account page/message.

Do not expose sensitive company information.

==================================================
SUPER ADMIN SECURITY
==================================================

Only users with the existing Super Admin role/permission may:

- Disable company
- Enable company
- View company subscription information across companies
- Modify plans if this is part of the administration functionality

Normal company administrators must NOT be able to disable their own company or other companies.

Enforce authorization in the backend.

Do not rely on Angular route guards alone.

==================================================
AUDIT LOG
==================================================

Important administrative actions should be audited.

At minimum log:

Company disabled
Company enabled
Plan changed
Subscription created
Subscription cancelled
Payment status changed

For each audit event store appropriate information such as:

UserId
CompanyId
Action
Timestamp
Relevant reference/id

Do not store payment secrets in logs.

==================================================
SUPER ADMIN COMPANY LIST
==================================================

Improve the Super Admin company management page.

Add columns such as:

Company Name
Status
Current Plan
Employees
Subscription
Trial End
Next Billing Date
Created
Actions

Use Bootstrap 5.3.3 tables/cards.

Add filters if practical:

All
Active
Disabled
Free Trial
Paid
Expired
Past Due

Keep the interface simple.

==================================================
API DESIGN
==================================================

Create appropriate API endpoints following the existing application's conventions.

Potential endpoints:

GET /api/subscription
GET /api/subscription/plans
POST /api/subscription/checkout
POST /api/subscription/upgrade
POST /api/subscription/cancel
POST /api/payment/webhook

Super Admin:

GET /api/admin/companies
GET /api/admin/companies/{id}
POST /api/admin/companies/{id}/disable
POST /api/admin/companies/{id}/enable

Use the existing API routing conventions instead of blindly copying these exact URLs.

==================================================
DATABASE
==================================================

Inspect the existing EF Core/database structure first.

Create migrations for all required tables/columns.

Do not modify existing production data destructively.

Use proper foreign keys and indexes.

Recommended relationships:

Company
   ↓
CompanySubscription
   ↓
SubscriptionPlan

Company
   ↓
Billing/PaymentProfile

Company
   ↓
Employees

Ensure CompanyId is consistently used for tenant isolation.

==================================================
TENANT SECURITY
==================================================

This is extremely important.

A company administrator must only be able to access:

- their own company
- their own employees
- their own payroll
- their own subscription
- their own billing information

Never allow a CompanyId supplied by the frontend to override the authenticated user's company.

Determine CompanyId from the authenticated user's claims/context whenever possible.

Apply this to:

Employees
Payroll
Subscription
Billing
Reports
etc.

==================================================
ANGULAR UI
==================================================

Add a Billing/Subscription page to the Angular application.

It should contain:

CURRENT PLAN

Free Trial

1–5 employees

R0

First month FREE

All features included

Trial ends:
DATE

Employee usage:

3 / 5 employees

[ Upgrade Plan ]

Then show:

AVAILABLE PLANS

R49
1–5 employees

R89
6–10 employees

R169
11–20 employees

etc.

The current plan should be visually highlighted.

The user should immediately understand:

- What plan they are on
- How many employees they have
- Maximum employees allowed
- How much they pay
- When the trial/billing period ends
- How to upgrade

==================================================
DASHBOARD PLAN INDICATOR
==================================================

Also add a small plan indicator to the main company dashboard/sidebar.

Example:

--------------------------------
YOUR PLAN

Free Trial

3 / 5 employees

[ Upgrade ]
--------------------------------

For paid users:

--------------------------------
YOUR PLAN

R89 / month

8 / 10 employees

[ Manage Plan ]
--------------------------------

This should fit nicely into the existing Bootstrap 5.3.3 dashboard design.

==================================================
UPGRADE EXPERIENCE
==================================================

When the user clicks Upgrade:

Show the plans.

Highlight the plan required to accommodate their current employee count.

Example:

"You currently have 8 employees."

"Your current plan supports 5 employees."

"Your recommended plan is:

6–10 employees
R89/month"

[ Upgrade to R89 ]

If payment is required:

→ Create checkout/payment session
→ Redirect to payment provider
→ Payment provider processes payment
→ Webhook confirms payment
→ Backend activates subscription
→ User returns to Payroll SA
→ Refresh subscription information

Do not activate the subscription based only on the return URL.

==================================================
DESIGN
==================================================

Use Bootstrap 5.3.3.

Keep the design:

- Clean
- Modern
- Professional
- Simple
- Mobile responsive

Use Bootstrap cards, badges, tables, alerts, progress bars and modals where appropriate.

Use Bootstrap Icons.

Example icons:

bi-credit-card
bi-wallet2
bi-person
bi-people
bi-check-circle
bi-arrow-up-circle
bi-shield-check
bi-exclamation-triangle

Use status badges:

FREE TRIAL
ACTIVE
PAST DUE
EXPIRED
DISABLED

Do not make the UI overly complicated.

==================================================
IMPORTANT BUSINESS RULE
==================================================

The backend must have ONE central mechanism to determine:

Current subscription
Current plan
Employee limit
Current employee count
Whether the company can add an employee
Whether payroll functionality is allowed
Whether the subscription is active

Do not duplicate this logic across controllers.

Create a service/domain layer appropriate to the existing architecture.

For example:

ISubscriptionService
IPlanService
IBillingService

Use the existing project's architecture where possible.

==================================================
IMPORTANT: DO NOT BREAK EXISTING FUNCTIONALITY
==================================================

Before implementing:

1. Inspect the existing database models.
2. Inspect Company/tenant implementation.
3. Inspect Employee model.
4. Inspect authentication.
5. Inspect roles.
6. Inspect Super Admin functionality.
7. Inspect Angular routing.
8. Inspect existing dashboard.
9. Inspect existing API structure.
10. Inspect existing EF Core DbContext.

Then integrate the subscription functionality into the existing architecture.

Do not create duplicate Company/User/Employee structures.

Do not replace working authentication.

Do not break existing payroll functionality.

==================================================
TESTING
==================================================

Add tests for the most important business rules.

At minimum test:

1. New company receives FREE_TRIAL.
2. Free trial has maximum 5 employees.
3. Free trial includes all features.
4. Company with 5 employees cannot add employee #6.
5. Company on R89 plan can have up to 10 employees.
6. Company with 10 employees cannot add employee #11 without upgrading.
7. Correct plan is recommended based on employee count.
8. Trial expiry changes subscription to EXPIRED.
9. Active paid subscription allows payroll.
10. Disabled company cannot access normal application functionality.
11. Non-Super Admin cannot disable a company.
12. Super Admin can disable a company.
13. Super Admin can re-enable a company.
14. Payment webhook cannot activate subscription without valid verification.
15. Duplicate webhook events do not create duplicate subscription/payment records.

==================================================
SECURITY
==================================================

Follow secure payment and application practices.

Never log:

- Card numbers
- CVV
- Full bank account numbers
- Payment credentials
- Passwords
- Access tokens
- Payment secrets

Use environment variables/app configuration for:

Payment provider API keys
Webhook secrets
Other sensitive configuration

Never hard-code secrets into source code.

==================================================
DELIVERABLE
==================================================

Implement the complete functionality.

After implementation, provide:

1. Files created
2. Files modified
3. Database migrations created
4. New API endpoints
5. New Angular routes/components
6. New entities/models
7. Subscription/business rules
8. Payment gateway integration/abstraction
9. Super Admin disable/enable functionality
10. Tests added
11. Any configuration/environment variables required

If a payment provider has not yet been selected/configured, do NOT pretend payment is live.

Instead implement a clean payment gateway abstraction and clearly identify the provider-specific part that needs to be configured.

Most importantly:

The system must be secure, multi-tenant, backend-enforced and transparent to customers.

The customer must always know:

"What plan am I on?"
"How many employees can I have?"
"How many employees am I currently using?"
"What am I paying?"
"When does my free month end?"
"How do I upgrade?"
"What happens if payment fails?"

The first month must clearly say:

"FREE FOR 1 MONTH — ALL FEATURES INCLUDED."

No feature restrictions during the free month.

The free month employee limit is 5 employees.