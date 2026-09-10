# Security Overview

## Authentication and authorization

The API uses ASP.NET Identity with JWT authentication, role-based authorization policies and company tenant isolation. Administrative endpoints require the appropriate role. Frontend navigation is not treated as a security boundary.

## Passwords and account abuse

Identity enforces password complexity and account lockout settings. Authentication endpoints have a stricter ASP.NET Core rate-limit policy than normal API endpoints. Keep reset and password-recovery flows generic so they do not reveal whether an email exists when those flows are enabled.

## API and input protection

Controllers use model binding, validation and MediatR handlers. EF Core parameterizes database access. API request bodies are capped at 10 MB. CORS is limited to configured application origins. Sensitive payment data is never stored; only provider references are retained.

## Headers and transport

Production enables HSTS. The API emits `X-Content-Type-Options`, frame protection, `Referrer-Policy`, `Permissions-Policy` and a restrictive Content Security Policy. Review the CSP when adding an external payment, analytics or font provider.

## Rate limiting and bot controls

Normal API traffic uses a moderate fixed-window limit and login/registration use a stricter limit. Legitimate search crawlers are not blocked based on User-Agent. Put production traffic behind a CDN/WAF for DDoS protection, managed bot detection, challenge rules and IP reputation controls.

## Data protection

Keep connection strings, JWT secrets, webhook secrets and email credentials in environment variables, user secrets or a managed secret store. Do not commit them to appsettings files. Restrict production database users to the minimum required privileges and encrypt backups.

## Logging and monitoring

Serilog and API audit logging provide request and business-event visibility. Production should alert on repeated failed authentication, rate-limit responses, disabled-company events, webhook failures, database errors and unusual administrative actions.

## Incident response

Document an owner and escalation path for `DOMAIN`. Preserve relevant logs, revoke compromised credentials, rotate secrets, suspend affected accounts, notify impacted parties where required, and record the timeline and remediation.

## Dependency and deployment hygiene

Run dependency audits during CI, keep .NET/EF/provider packages aligned, use HTTPS, review WAF rules and test the production CSP before release. DNS, SPF, DKIM, DMARC and DNSSEC are deployment responsibilities described in `docs/SEO-SECURITY-DNS.md`.
