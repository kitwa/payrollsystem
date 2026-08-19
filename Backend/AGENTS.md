# AGENTS.md

## Project

Build a modern South African payroll system similar in functionality to SimplePay.

The objective is NOT to clone SimplePay's UI.
The objective is to build an enterprise-grade payroll platform with similar capabilities using modern Microsoft technologies.

This project will eventually replace SQLite with SQL Server.

---

# Tech Stack

Always use the latest stable versions.

Backend

- .NET 10
- ASP.NET Core Web API
- Minimal APIs where appropriate
- Entity Framework Core
- SQLite (initially)
- SQL Server compatibility
- FluentValidation
- MediatR
- AutoMapper
- Serilog
- Hangfire (scheduled payroll)
- QuestPDF
- ClosedXML
- Swagger / OpenAPI

Frontend

- Angular (latest)
- Standalone Components
- Signals
- RxJS
- Angular Router
- Bootstrap 5.x
- Bootstrap Icons
- ngx-toastr
- Angular Reactive Forms

Authentication

- ASP.NET Identity
- JWT
- Refresh Tokens

Testing

- xUnit
- FluentAssertions

---

# Existing Solution

A folder called ExistingProject exists in this repository.

Before generating any code:

Always inspect ExistingProject.

Reuse

- architecture
- folder naming
- coding style
- naming conventions
- dependency injection
- logging
- exception handling
- API response format

Never create conflicting architecture.

The new Payroll solution must feel like it belongs to the existing solution.

---

# Architecture

Use Clean Architecture.

```
src

    Payroll.Api

    Payroll.Application

    Payroll.Domain

    Payroll.Infrastructure

    Payroll.Shared

tests

docs

```

Each project has one responsibility.

Never place business logic inside controllers.

Controllers should only:

- validate
- call MediatR
- return results

---

# Domain Driven Design

Organize by domain.

Examples

Employee

Payroll

Leave

Tax

Reports

Company

User

Settings

Each domain contains

Entities

Value Objects

Commands

Queries

Validators

Handlers

DTOs

Interfaces

---

# Coding Standards

Always

- use async/await
- use nullable reference types
- enable implicit usings
- XML comments for public APIs
- constructor injection
- dependency injection
- SOLID
- DRY
- KISS

Never

- duplicate code
- use magic strings
- use static helper classes for business logic
- use repository anti-pattern over EF Core unnecessarily

---

# Database

Initially use SQLite.

All entities must be EF Core compatible.

Use migrations.

Seed sample data.

Primary keys

Guid

Audit columns

Created

CreatedBy

Modified

ModifiedBy

Deleted

DeletedBy

Soft Delete

---

# Core Modules

## Employees

CRUD

Employee Number

Personal Details

Bank Details

Tax Number

UIF Number

Employment Status

Employment Type

Salary

Department

Job Title

Termination

Documents

---

## Payroll

Monthly payroll

Payroll Period

Draft

Approved

Locked

Paid

Generate payroll

Recalculate payroll

Payroll history

---

## Earnings

Basic Salary

Bonus

Commission

Allowance

Travel

Cell Phone

Overtime

Night Shift

Other Earnings

---

## Deductions

PAYE

UIF

SDL

Medical Aid

Pension

Provident Fund

Retirement Annuity

Loans

Advances

Custom Deductions

---

## Leave

Annual Leave

Sick Leave

Family Responsibility

Study Leave

Maternity

Paternity

Unpaid Leave

Leave Requests

Leave Approval Workflow

Leave Calendar

Leave Balances

---

## Payslips

Generate PDF

Email Payslip

Download Payslip

Bulk Download

Password Protected PDF

---

## SARS

Support

IRP5

IT3(a)

EMP201

EMP501

CSV Export

XML Export

Validation Rules

Tax Year

Bi-Annual Submission

---

## Reporting

Payroll Register

Leave Report

UIF Report

SDL Report

Tax Report

Employee Cost Report

Audit Report

---

## Self Service

Employee Login

Download Payslips

Leave Requests

Leave History

Update Contact Details

Upload Documents

Notifications

---

# Payroll Engine

The payroll engine must be completely isolated.

Create

PayrollEngine

Services

Interfaces

Rules

Calculators

Each deduction must have its own calculator.

Example

PAYECalculator

UIFCalculator

SDLCalculator

OvertimeCalculator

LeaveCalculator

Each calculator must implement

IPayrollCalculator

Never place payroll calculations directly inside controllers.

---

# South African Rules

Implement payroll according to South African legislation.

Support

PAYE

UIF

SDL

Tax Rebates

Tax Thresholds

Progressive Tax Tables

Age Thresholds

Tax Year Configuration

Never hardcode tax tables.

Create configurable tables in the database.

---

# API Design

RESTful

/api/employees

/api/payroll

/api/leave

/api/reports

/api/tax

/api/payslips

/api/settings

Return

ProblemDetails

Validation Errors

Pagination

Filtering

Sorting

---

# Angular

Feature based architecture

```
features/

employees/

payroll/

leave/

reports/

settings/

shared/

layout/

core/

```

Each feature has

pages

components

services

models

state

routes

Never put everything into app.component.

Use Signals whenever appropriate.

---

# UI

Bootstrap only.

Responsive.

Enterprise look.

Professional colors.

Dashboard

Cards

Tables

Charts

Modals

Toasts

Dark mode support

---

# Forms

Reactive Forms only.

Validation

Reusable components

Custom validators

Error components

Loading indicators

---

# Security

JWT

Refresh Tokens

Role Based Authorization

Permissions

Audit Logging

Password Policies

Account Lockout

---

# Logging

Serilog

Log

Errors

Warnings

Audit

Payroll Runs

User Actions

---

# Documentation

Every major feature must include

README

Architecture notes

API examples

Sequence diagrams where useful

---

# Copilot Instructions

When generating code

Think before coding.

Always inspect existing code first.

Prefer extending existing implementations.

Never overwrite working code.

Never generate placeholder implementations unless requested.

Implement complete working solutions.

Always generate

DTO

Validator

Command

Handler

Endpoint

Mapping

Unit Test

Migration

Documentation

when adding a feature.

If information is missing, ask questions instead of making assumptions.

---

# Development Order

Build features in this order

1 Authentication

2 Users

3 Companies

4 Employees

5 Payroll Engine

6 PAYE

7 UIF

8 SDL

9 Leave

10 Payslips

11 Reporting

12 SARS Exports

13 Employee Self Service

14 Notifications

15 Background Jobs

16 Dashboard

17 Audit Logs

18 Settings


