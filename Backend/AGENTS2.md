# AGENTS2.md — Payroll SA

> **Purpose:** This file is the canonical reference for **human developers and AI coding agents** working on this repository. Read this before making any changes. It captures product intent, architecture decisions, domain rules, known gotchas, and conventions so context survives across sessions.

---

## Product summary

**Payroll SA** is an enterprise-grade South African payroll platform with feature parity to [SimplePay](https://www.simplepay.co.za). The goal is **not** to clone SimplePay's UI — the goal is to build a clean, modern, legislation-compliant payroll system using current Microsoft technologies.

| Attribute | Detail |
|-----------|--------|
| Target market | South African businesses of all sizes |
| Legislation | SARS PAYE, UIF, SDL, IRP5, IT3(a), EMP201, EMP501 |
| Business model | SaaS subscription (per company / per employee) |
| Database | SQLite (development) → SQL Server (production) |
| Key differentiator | Configurable tax tables (no hardcoded rates), full SARS export suite, payroll engine isolation |

---

## Repository layout

```
payrollsa/
├── payrollsa.sln                    # Visual Studio solution
├── src/
│   ├── Payroll.Api/                 # ASP.NET Core 10 Web API — controllers, middleware, DI composition root
│   ├── Payroll.Application/         # MediatR commands, queries, validators, handlers, DTOs, interfaces
│   ├── Payroll.Domain/              # Entities, value objects, domain events, enums, domain rules
│   ├── Payroll.Infrastructure/      # EF Core, repositories, email, PDF, Excel, Hangfire, external services
│   └── Payroll.Shared/              # Cross-cutting: result types, pagination, constants, extensions
├── tests/
│   ├── Payroll.Application.Tests/   # xUnit + FluentAssertions unit tests for handlers/validators
│   └── Payroll.Domain.Tests/        # Pure domain logic tests
├── docs/                            # Architecture notes, sequence diagrams, API examples
├── client/                          # Angular (latest) SPA
└── ExistingProject/                 # Reference implementation — inspect before generating any code
```

---

## Clean Architecture layer rules

```
Payroll.Api
  │  depends on
  ▼
Payroll.Application
  │  depends on
  ▼
Payroll.Domain          ← no dependencies on other layers
  ▲
  │  implements interfaces defined in Application
Payroll.Infrastructure

Payroll.Shared          ← depended on by all layers
```

**Hard rules:**
- Business logic lives in `Payroll.Domain` or `Payroll.Application` handlers — **never** in controllers.
- `Payroll.Infrastructure` implements interfaces declared in `Payroll.Application`.
- `Payroll.Api` only validates, calls MediatR, and returns results.
- `Payroll.Domain` has zero dependencies on EF Core, HTTP, or any framework.

---

## Backend — `Payroll.Api/`

### Stack

| Concern | Technology |
|---------|-----------|
| Runtime | .NET 10, nullable reference types enabled, implicit usings enabled |
| HTTP | ASP.NET Core Web API — attribute-routed controllers |
| Auth | ASP.NET Core Identity + JWT Bearer + Refresh Tokens |
| Internal bus | MediatR (handlers in `Payroll.Application`) |
| Validation | FluentValidation — pipeline behaviour in MediatR |
| Mapping | AutoMapper — profiles in `Payroll.Application` |
| Logging | Serilog — structured logs to console + file + (prod) sink |
| Background jobs | Hangfire — scheduled payroll runs, bulk payslip email |
| PDF | QuestPDF — payslip generation |
| Excel | ClosedXML — payroll register, SARS CSV/Excel exports |
| Docs | Swagger / OpenAPI at `/swagger` |
| ORM | EF Core — `Payroll.Infrastructure` |
| DB (dev) | SQLite |
| DB (prod) | SQL Server |

### Request pipeline (order matters)

```
Request
  │
  ▼
ExceptionMiddleware          ← returns ProblemDetails on all unhandled exceptions
  │
  ▼
HTTPS Redirection
  │
  ▼
Routing
  │
  ▼
CORS
  │
  ▼
Authentication               ← validates JWT Bearer token; RefreshToken endpoint bypasses
  │
  ▼
Authorization                ← role + permission policy checks
  │
  ▼
Swagger UI                   ← /swagger (all environments)
  │
  ▼
Static Files                 ← serves Angular build from wwwroot/
  │
  ▼
Controllers                  ← thin: validate → MediatR.Send → return result
  │
  ▼
FallbackController           ← SPA index.html for non-API routes
```

### API surface

Base route: `/api/[controller]`

| Controller | Routes | Auth |
|------------|--------|------|
| `AuthController` | `POST /login`, `POST /refresh-token`, `POST /revoke-token`, `POST /forgot-password`, `POST /reset-password` | Public |
| `UsersController` | `GET`, `GET {id}`, `POST`, `PUT {id}`, `DELETE {id}`, `POST edit-roles/{id}` | Admin |
| `CompaniesController` | `GET`, `GET {id}`, `POST`, `PUT {id}`, `DELETE {id}` | Admin |
| `EmployeesController` | `GET` (paginated), `GET {id}`, `POST`, `PUT {id}`, `DELETE {id}`, `POST {id}/documents`, `GET {id}/documents` | PayrollManager |
| `PayrollController` | `GET periods`, `GET {periodId}`, `POST generate`, `POST {periodId}/recalculate`, `POST {periodId}/approve`, `POST {periodId}/lock`, `POST {periodId}/mark-paid`, `GET {periodId}/lines` | PayrollManager |
| `LeaveController` | `GET`, `GET {id}`, `POST request`, `PUT {id}/approve`, `PUT {id}/reject`, `DELETE {id}`, `GET balances/{employeeId}`, `GET calendar` | PayrollManager / Employee (own) |
| `PayslipsController` | `GET {employeeId}`, `GET {id}/download`, `POST {periodId}/generate-all`, `POST {periodId}/email-all`, `GET {id}/preview` | PayrollManager / Employee (own) |
| `TaxController` | `GET tables`, `POST tables`, `PUT tables/{id}`, `GET thresholds`, `PUT thresholds/{id}`, `GET rebates`, `PUT rebates/{id}`, `GET tax-year` | Admin |
| `ReportsController` | `GET payroll-register`, `GET leave-report`, `GET uif-report`, `GET sdl-report`, `GET tax-report`, `GET employee-cost`, `GET audit` | PayrollManager |
| `SarsController` | `GET irp5/{taxYear}`, `GET it3a/{taxYear}`, `GET emp201/{period}`, `GET emp501/{taxYear}`, `POST validate/{taxYear}` | Admin |
| `SettingsController` | `GET`, `PUT`, `GET leave-types`, `POST leave-types`, `GET earning-types`, `POST earning-types`, `GET deduction-types`, `POST deduction-types` | Admin |
| `DashboardController` | `GET summary`, `GET charts` | PayrollManager |

### Hangfire background jobs

| Job | Schedule | Purpose |
|-----|----------|---------|
| `GenerateMonthlyPayrollJob` | 1st of month (configurable) | Auto-drafts payroll period |
| `SendPayslipEmailsJob` | On demand / trigger | Bulk email payslips to employees |
| `TaxYearRolloverJob` | 1 March annually | Creates new tax year, copies thresholds |
| `LeaveAccrualJob` | Monthly | Accrues leave balances per employee |
| `AuditLogCleanupJob` | Weekly | Archives old audit records |

---

## `Payroll.Application/`

All business orchestration lives here. Organized by **domain**.

```
Payroll.Application/
├── Common/
│   ├── Behaviours/
│   │   ├── ValidationBehaviour.cs       # FluentValidation MediatR pipeline
│   │   ├── LoggingBehaviour.cs          # Serilog request/response logging
│   │   └── AuditBehaviour.cs            # Writes audit log entry per command
│   ├── Interfaces/                      # IEmailService, IPdfService, IExcelService, ICurrentUser
│   ├── Mappings/                        # AutoMapper profiles
│   └── Models/
│       ├── PagedList.cs
│       ├── PaginationParams.cs
│       └── Result.cs                    # Result<T> — all handlers return this
├── Employees/
│   ├── Commands/                        # CreateEmployee, UpdateEmployee, TerminateEmployee, UploadDocument
│   ├── Queries/                         # GetEmployees, GetEmployeeById, GetEmployeeDocuments
│   ├── Validators/                      # CreateEmployeeValidator, UpdateEmployeeValidator
│   └── DTOs/                            # EmployeeDto, EmployeeListDto, CreateEmployeeDto, UpdateEmployeeDto
├── Payroll/
│   ├── Commands/                        # GeneratePayroll, RecalculatePayroll, ApprovePayroll, LockPayroll, MarkPaid
│   ├── Queries/                         # GetPayrollPeriods, GetPayrollById, GetPayrollLines
│   ├── Validators/
│   └── DTOs/                            # PayrollPeriodDto, PayrollLineDto, GeneratePayrollDto
├── Leave/
│   ├── Commands/                        # RequestLeave, ApproveLeave, RejectLeave, DeleteLeave
│   ├── Queries/                         # GetLeaveRequests, GetLeaveBalances, GetLeaveCalendar
│   ├── Validators/
│   └── DTOs/
├── Tax/
│   ├── Commands/                        # UpdateTaxTable, UpdateTaxThreshold, UpdateRebate
│   ├── Queries/                         # GetTaxTables, GetThresholds, GetRebates
│   └── DTOs/
├── Payslips/
│   ├── Commands/                        # GeneratePayslip, GenerateAllPayslips, EmailPayslip, EmailAllPayslips
│   ├── Queries/                         # GetPayslipsByEmployee, GetPayslipPreview
│   └── DTOs/
├── Reports/
│   ├── Queries/                         # GetPayrollRegister, GetLeaveReport, GetUifReport, GetSdlReport, etc.
│   └── DTOs/
├── Sars/
│   ├── Queries/                         # GetIrp5, GetIt3a, GetEmp201, GetEmp501
│   └── DTOs/
├── Companies/
│   ├── Commands/
│   ├── Queries/
│   └── DTOs/
├── Users/
│   ├── Commands/
│   ├── Queries/
│   └── DTOs/
└── Settings/
    ├── Commands/
    ├── Queries/
    └── DTOs/
```

---

## `Payroll.Domain/`

Pure business rules — no EF Core, no HTTP, no external dependencies.

```
Payroll.Domain/
├── Common/
│   ├── BaseEntity.cs                    # Id (Guid), CreatedAt, CreatedBy, ModifiedAt, ModifiedBy, IsDeleted, DeletedBy
│   └── IAuditableEntity.cs
├── Employees/
│   ├── Employee.cs                      # Core employee aggregate root
│   ├── EmployeeDocument.cs
│   ├── BankDetails.cs                   # Value object
│   └── Enums/                           # EmploymentStatus, EmploymentType, Gender
├── Payroll/
│   ├── PayrollPeriod.cs                 # Aggregate root — owns PayrollLines
│   ├── PayrollLine.cs                   # One line per employee per period
│   ├── Earning.cs
│   ├── Deduction.cs
│   └── Enums/                           # PayrollStatus (Draft, Approved, Locked, Paid)
├── Leave/
│   ├── LeaveRequest.cs
│   ├── LeaveBalance.cs
│   └── Enums/                           # LeaveType, LeaveStatus
├── Tax/
│   ├── TaxTable.cs                      # Progressive tax brackets (per tax year)
│   ├── TaxThreshold.cs                  # Age-based thresholds
│   ├── TaxRebate.cs                     # Primary, secondary, tertiary rebates
│   └── TaxYear.cs
├── Companies/
│   └── Company.cs
├── Settings/
│   ├── EarningType.cs                   # Configurable earning definitions
│   ├── DeductionType.cs                 # Configurable deduction definitions
│   └── LeaveType.cs
└── Identity/
    ├── AppUser.cs                        # extends IdentityUser
    └── AppRole.cs
```

### Domain model (entity relationships)

```
Company
  └── has many  Employee
                  ├── has many  BankDetails (value object)
                  ├── has many  EmployeeDocument
                  ├── has many  LeaveBalance      (one per LeaveType)
                  ├── has many  LeaveRequest
                  └── participates in many PayrollLine

PayrollPeriod  (owned by Company)
  └── has many  PayrollLine  (one per Employee per period)
                  ├── has many  Earning    (BasicSalary, Bonus, Overtime, Allowance, etc.)
                  └── has many  Deduction  (PAYE, UIF, SDL, MedicalAid, Pension, etc.)

TaxYear
  ├── has many  TaxTable      (progressive brackets)
  ├── has many  TaxThreshold  (age-based primary/secondary/tertiary)
  └── has many  TaxRebate

Reference / lookup:
  EarningType, DeductionType, LeaveType, EmploymentStatus, EmploymentType, Province
```

---

## `Payroll.Infrastructure/`

Implements all interfaces declared in `Payroll.Application`.

```
Payroll.Infrastructure/
├── Persistence/
│   ├── AppDbContext.cs                  # EF Core DbContext — all domain DbSets
│   ├── Seed.cs                          # Seeds roles, admin user, tax tables, leave types
│   ├── Migrations/
│   └── Configurations/                  # IEntityTypeConfiguration per entity
├── Services/
│   ├── EmailService.cs                  # IEmailService — SMTP transactional email
│   ├── PdfService.cs                    # IPdfService — QuestPDF payslip generation
│   ├── ExcelService.cs                  # IExcelService — ClosedXML exports
│   ├── CurrentUserService.cs            # ICurrentUser — reads claims from HttpContext
│   └── AuditService.cs                  # IAuditService — persists audit log entries
├── PayrollEngine/
│   ├── PayrollEngine.cs                 # Orchestrates calculators for a PayrollLine
│   ├── Interfaces/
│   │   └── IPayrollCalculator.cs        # Calculate(context) → CalculationResult
│   └── Calculators/
│       ├── PAYECalculator.cs
│       ├── UIFCalculator.cs
│       ├── SDLCalculator.cs
│       ├── OvertimeCalculator.cs
│       ├── LeaveCalculator.cs
│       ├── MedicalAidCalculator.cs
│       ├── PensionCalculator.cs
│       └── ProvidentFundCalculator.cs
├── BackgroundJobs/
│   ├── GenerateMonthlyPayrollJob.cs
│   ├── SendPayslipEmailsJob.cs
│   ├── TaxYearRolloverJob.cs
│   ├── LeaveAccrualJob.cs
│   └── AuditLogCleanupJob.cs
└── DependencyInjection.cs               # AddInfrastructure(services, config) extension
```

### Payroll engine design

```
PayrollEngine.ProcessAsync(employeeId, periodId)
  │
  ├── Load Employee + active Earnings + Deductions
  ├── Load TaxTable / TaxThreshold / TaxRebate for tax year
  │
  ├── foreach IPayrollCalculator (ordered):
  │     OvertimeCalculator     → adds Earning lines
  │     LeaveCalculator        → deducts unpaid leave
  │     PAYECalculator         → reads progressive tax table → adds PAYE Deduction
  │     UIFCalculator          → capped at UIF ceiling → adds UIF Deduction
  │     SDLCalculator          → 1% of remuneration → adds SDL Deduction
  │     MedicalAidCalculator   → adds MedicalAid Deduction
  │     PensionCalculator      → % of basic or fixed → adds Pension Deduction
  │     ProvidentFundCalculator
  │
  └── Persist PayrollLine with all Earning + Deduction child rows
```

**Rules:**
- Each calculator receives a `PayrollContext` (employee, earnings, tax tables, period).
- Each calculator returns a `CalculationResult` — never mutates shared state directly.
- Tax tables are loaded from the database — **never hardcoded**.
- PAYE uses progressive brackets from `TaxTable`; age thresholds from `TaxThreshold`.

---

## `Payroll.Shared/`

```
Payroll.Shared/
├── Result.cs                # Result<T> — Ok(value) / Fail(errors)
├── PagedList.cs             # Generic paginated wrapper
├── PaginationParams.cs
├── Constants.cs             # Role names, policy names, claim types
└── Extensions/              # String, DateTime, decimal helpers for SA payroll
```

---

## Auth & authorization

| Layer | Mechanism |
|-------|-----------|
| Token issuance | `TokenService` — HS512 signed JWT; claims: `sub` (userId), `email`, `role`, `companyId` |
| Refresh tokens | Stored in DB (`RefreshToken` entity on `AppUser`); rotated on each use |
| Token validation | `AddJwtBearer` in `DependencyInjection`; key from `Jwt:Key` |
| Role seed | `Seed.cs` creates: `SuperAdmin`, `Admin`, `PayrollManager`, `Employee` |
| Policies | `RequireAdminRole` · `RequirePayrollManagerRole` · `RequireEmployeeRole` |
| Audit logging | `AuditBehaviour` in MediatR pipeline writes every command to `AuditLog` table |

---

## Frontend — `client/`

### Stack

| Concern | Technology |
|---------|-----------|
| Framework | Angular (latest), TypeScript, RxJS |
| Components | Standalone components (no NgModules where avoidable) |
| State | Angular Signals (preferred over BehaviorSubject for local state) |
| UI | Bootstrap 5, Bootstrap Icons, ngx-toastr, ngx-spinner |
| Forms | Reactive Forms only — custom validators, reusable error components |
| HTTP | `HttpClient` + `JwtInterceptor` + `ErrorInterceptor` |
| Charts | (chart library per dashboard feature — TBD) |

### Folder layout

```
client/src/app/
├── core/
│   ├── auth/                            # Login, token storage, AuthService
│   ├── guards/                          # authGuard, roleGuard
│   ├── interceptors/                    # JwtInterceptor, ErrorInterceptor
│   └── models/                          # Core interfaces (User, AuthResponse)
├── shared/
│   ├── components/                      # Reusable UI: pagination, confirm-modal, loading, error-display
│   ├── directives/                      # has-role.directive, click-outside.directive
│   ├── pipes/                           # currency-za.pipe, date-za.pipe
│   └── validators/                      # Reusable custom validators
├── layout/
│   ├── navbar/
│   ├── sidebar/
│   └── footer/
└── features/
    ├── dashboard/
    │   ├── pages/
    │   ├── components/                  # Summary cards, charts
    │   ├── services/
    │   ├── models/
    │   └── dashboard.routes.ts
    ├── employees/
    │   ├── pages/                       # employee-list, employee-detail, employee-form
    │   ├── components/                  # bank-details-form, document-upload, employment-status-badge
    │   ├── services/
    │   ├── models/
    │   └── employees.routes.ts
    ├── payroll/
    │   ├── pages/                       # payroll-list, payroll-detail, payroll-run
    │   ├── components/                  # payroll-line-table, status-stepper, recalculate-button
    │   ├── services/
    │   ├── models/
    │   └── payroll.routes.ts
    ├── leave/
    │   ├── pages/                       # leave-list, leave-request-form, leave-calendar, leave-balances
    │   ├── components/
    │   ├── services/
    │   ├── models/
    │   └── leave.routes.ts
    ├── payslips/
    │   ├── pages/                       # payslip-list, payslip-viewer
    │   ├── components/
    │   ├── services/
    │   ├── models/
    │   └── payslips.routes.ts
    ├── reports/
    │   ├── pages/                       # report-selector, report-viewer
    │   ├── components/
    │   ├── services/
    │   ├── models/
    │   └── reports.routes.ts
    ├── settings/
    │   ├── pages/                       # general-settings, leave-types, earning-types, deduction-types, tax-tables
    │   ├── components/
    │   ├── services/
    │   ├── models/
    │   └── settings.routes.ts
    └── self-service/                    # Employee-facing: own payslips, leave requests, contact details
        ├── pages/
        ├── services/
        ├── models/
        └── self-service.routes.ts
```

### Angular data flow

```
Page / Smart Component
  │  calls method on
  ▼
Feature Service
  │  HttpClient.get/post/put/delete
  │  + environment.apiUrl prefix
  │  + JwtInterceptor adds Authorization: Bearer <token>
  ▼
API REST endpoint
  │  returns JSON wrapped in Result<T>
  ▼
Feature Service (maps to feature models)
  │  Observable<T> or Signal
  ▼
Dumb Component / Template
  │  async pipe or signal binding
  ▼
DOM
```

### Route access matrix

| Path pattern | Guard | Role |
|-------------|-------|------|
| `/login`, `/forgot-password`, `/reset-password` | none | Public |
| `/dashboard` | `authGuard` | All authenticated |
| `/employees/**` | `authGuard` + `roleGuard` | PayrollManager, Admin |
| `/payroll/**` | `authGuard` + `roleGuard` | PayrollManager |
| `/leave/**` | `authGuard` + `roleGuard` | PayrollManager (all), Employee (own) |
| `/payslips/**` | `authGuard` + `roleGuard` | PayrollManager (all), Employee (own) |
| `/reports/**` | `authGuard` + `roleGuard` | PayrollManager, Admin |
| `/settings/**` | `authGuard` + `roleGuard` | Admin |
| `/self-service/**` | `authGuard` + `roleGuard` | Employee |
| `/admin/**` | `authGuard` + `roleGuard` | Admin, SuperAdmin |

---

## External integrations

| Service | Purpose | Config key |
|---------|---------|-----------|
| **SMTP** | Transactional email — registration, payslip delivery, leave notifications | `EmailSettings` (`Host`, `Port`, `User`, `Password`) |
| **QuestPDF** | Payslip PDF generation (password-protected per employee) | No external service — library only |
| **ClosedXML** | Payroll register Excel, SARS CSV/Excel exports | No external service — library only |
| **Hangfire** | Scheduled and background jobs (payroll runs, bulk email) | `ConnectionStrings:HangfireConnection` |
| **Serilog** | Structured logging to console, rolling file, and (prod) external sink | `Serilog` section in `appsettings.json` |

---

## Infrastructure

```
Development
  SQLite database — no Docker required
  File: payroll.db in API output directory
  Run: dotnet ef database update (from Payroll.Api)

Production
  SQL Server (managed or Docker)
  Connection string via environment variable or secrets
  Hangfire uses a separate connection string (HangfireConnection)

Background jobs
  Hangfire dashboard at /hangfire (Admin role only)
  Jobs registered in Payroll.Infrastructure/DependencyInjection.cs
```

---

## Configuration

| File | Purpose |
|------|---------|
| `appsettings.json` | Defaults (Serilog, Swagger, CORS) |
| `appsettings.Development.json` | SQLite connection, JWT secret, SMTP test values |
| `appsettings.Production.json` | SQL Server connection, real SMTP, Serilog sinks |

Required keys:

- `ConnectionStrings:DefaultConnection` — SQLite path or SQL Server connection string.
- `ConnectionStrings:HangfireConnection` — separate DB for Hangfire (can share in dev).
- `Jwt:Key` — base64-encoded symmetric JWT signing secret; must decode to at least 64 bytes.
- `Jwt:ExpiryMinutes` — access token lifetime.
- `Jwt:RefreshTokenExpiryDays` — refresh token lifetime.
- `EmailSettings` — SMTP host, port, credentials.

---

## Coding standards (always enforce)

| Rule | Detail |
|------|--------|
| async/await | All I/O must be async — no `.Result` or `.Wait()` |
| Nullable reference types | Enabled — no `!` suppressions without justification |
| Constructor injection | Never use `ServiceLocator` or static accessors |
| Result pattern | All handlers return `Result<T>` — never throw business exceptions |
| XML comments | Required on all public APIs in `Payroll.Application` and `Payroll.Domain` |
| No magic strings | Use `Constants.cs` in `Payroll.Shared` |
| No static business logic | Calculators must implement `IPayrollCalculator` and be DI-registered |
| SOLID / DRY / KISS | One responsibility per class; extract when logic repeats twice |

---

## Gotchas for agents

| # | Issue | Impact |
|---|-------|--------|
| 1 | **Tax tables must not be hardcoded** — SARS changes rates annually | Always load from `TaxTable`, `TaxThreshold`, `TaxRebate` entities; never embed values in C# |
| 2 | **Payroll engine isolation** — calculators must only read from `PayrollContext`, not call `DbContext` directly | Violations make the engine untestable and break the calculation order |
| 3 | **EF Core in Domain is forbidden** — `Payroll.Domain` must have zero references to `Microsoft.EntityFrameworkCore` | EF attributes and navigation loading belong in `Payroll.Infrastructure/Configurations/` |
| 4 | **Inspect `ExistingProject/` before generating code** — the existing solution defines folder naming, DI style, response format, and logging patterns | Generating conflicting patterns breaks consistency across the solution |
| 5 | **Payroll period states are a state machine** — valid transitions are Draft → Approved → Locked → Paid; no reverse transitions | Controllers must validate state before calling `ApprovePayroll`, `LockPayroll`, `MarkPaid` |
| 6 | **UIF has a monthly ceiling** — currently R17,712 per month (configured in `TaxYear`/settings, not hardcoded) | UIFCalculator must read the ceiling from config, not a constant |
| 7 | **Refresh tokens are rotated** — on each `/refresh-token` call, old token is invalidated and a new one issued | Never reuse a refresh token; always persist the new one before responding |
| 8 | **SQLite does not support all SQL Server features** — avoid `ROWVERSION`, `SEQUENCE`, full-text search, or SQL Server-specific EF Core configurations | Use EF Core value converters and provider-agnostic patterns so migration to SQL Server is a config change |

---

## Extension checklist

When adding a new feature (e.g. a new domain area or endpoint), follow this order:

1. **Domain entity** — create class in `Payroll.Domain/<Area>/`, extending `BaseEntity`. Add `DbSet<T>` to `AppDbContext`, add `IEntityTypeConfiguration<T>` in `Configurations/`, run `dotnet ef migrations add <Name> --project Payroll.Infrastructure --startup-project Payroll.Api`.
2. **DTOs** — add request/response DTOs in `Payroll.Application/<Area>/DTOs/`; register mappings in the area's AutoMapper profile.
3. **Validator** — add `AbstractValidator<TDto>` in `Payroll.Application/<Area>/Validators/` using FluentValidation.
4. **Command / Query + Handler** — add in `Payroll.Application/<Area>/Commands|Queries/`; handler returns `Result<TDto>`.
5. **Interface (if external service needed)** — declare in `Payroll.Application/Common/Interfaces/`; implement in `Payroll.Infrastructure/Services/`; register in `DependencyInjection.cs`.
6. **Controller action** — add to `Payroll.Api/Controllers/<Area>Controller.cs`; action must only call `_mediator.Send(...)` and map `Result<T>` to HTTP response.
7. **Unit test** — add handler test in `Payroll.Application.Tests/<Area>/`; use xUnit + FluentAssertions; mock interfaces with Moq or NSubstitute.
8. **Angular** — add TypeScript model in `features/<area>/models/`, service method in `features/<area>/services/`, component/page in `features/<area>/pages/`, wire route in `<area>.routes.ts` with appropriate guard.

---

## Development order

Build features in this exact order. Do not skip ahead unless explicitly requested.

| # | Feature | Notes |
|---|---------|-------|
| 1 | Authentication | JWT + Refresh Tokens, roles, Identity seed |
| 2 | Users | CRUD, role assignment, profile |
| 3 | Companies | Company CRUD, company-scoped data |
| 4 | Employees | Full CRUD, bank details, documents, employment status |
| 5 | Payroll Engine | `IPayrollCalculator`, `PayrollEngine`, `PayrollContext` — no UI yet |
| 6 | PAYE | `PAYECalculator`, tax tables, progressive brackets, rebates |
| 7 | UIF | `UIFCalculator`, monthly ceiling, employer/employee split |
| 8 | SDL | `SDLCalculator`, 1% of remuneration |
| 9 | Leave | Leave types, requests, approval workflow, balances, calendar |
| 10 | Payslips | QuestPDF generation, password-protected PDF, email delivery |
| 11 | Reporting | Payroll register, leave report, UIF/SDL/tax/cost reports |
| 12 | SARS Exports | IRP5, IT3(a), EMP201, EMP501, CSV/XML, validation rules |
| 13 | Employee Self Service | Own payslips, leave requests, leave history, contact update |
| 14 | Notifications | Email notifications for leave approval, payslip ready, payroll locked |
| 15 | Background Jobs | Hangfire: monthly payroll draft, bulk payslip email, leave accrual, tax rollover |
| 16 | Dashboard | Summary cards, payroll chart, leave chart, upcoming payroll |
| 17 | Audit Logs | `AuditBehaviour`, audit log viewer in admin |
| 18 | Settings | Earning types, deduction types, leave types, tax year config, company settings |

---

## Contact

Developer: Dominique Kiboko Kitwa — [dominiquekitwa2@gmail.com](mailto:dominiquekitwa2@gmail.com) — [github.com/kitwa](https://github.com/kitwa)
