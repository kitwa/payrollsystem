# SEO, Security and DNS Deployment Notes

Replace the placeholders before production deployment:

- `DOMAIN`: the canonical website domain
- `DNS_PROVIDER`: the authoritative DNS provider
- `EMAIL_PROVIDER`: the service that sends transactional email
- `CLOUD_PROVIDER`: the hosting provider
- `CDN/WAF_PROVIDER`: Cloudflare or the selected CDN/WAF

## DNS and TLS

- Serve the website and API over HTTPS only.
- Enable HSTS after HTTPS is verified across every production hostname.
- Enable DNSSEC at `DNS_PROVIDER` where supported.
- Keep domain ownership and Google Search Console verification under the organisation's control.
- Put the application behind `CDN/WAF_PROVIDER` for DDoS absorption, managed firewall rules, bot controls and rate limiting.

## Email authentication

Configure records from the actual `EMAIL_PROVIDER`; do not copy generic examples:

- SPF authorises the provider that sends mail for `DOMAIN`.
- DKIM keys are generated and published by `EMAIL_PROVIDER`.
- DMARC starts with monitoring (`p=none`) and can be tightened after legitimate senders are confirmed.

## Search Console

1. Add `DOMAIN` as a Domain property in Google Search Console.
2. Complete DNS ownership verification.
3. Submit `https://DOMAIN/sitemap.xml`.
4. Inspect the homepage, feature pages, educational payroll pages and blog articles.
5. Monitor indexing, Core Web Vitals, search queries, manual actions and security issues.

## Analytics

Analytics is optional. If enabled, provide `GOOGLE_ANALYTICS_ID` through deployment configuration. Do not send employee names, IDs, salaries, bank details, payroll values or other sensitive data to analytics.

## Public crawl surface

The frontend publishes `robots.txt` and `sitemap.xml`. Public content is crawlable; authenticated application routes and API paths are disallowed in robots directives but remain protected by server authorization.
