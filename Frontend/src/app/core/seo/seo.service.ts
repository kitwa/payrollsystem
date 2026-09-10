import { DOCUMENT } from '@angular/common';
import { Injectable, inject } from '@angular/core';
import { Meta, Title } from '@angular/platform-browser';

export interface SeoPage {
  title: string;
  description: string;
  path: string;
  type?: 'website' | 'article';
  image?: string;
  published?: string;
  modified?: string;
  author?: string;
  breadcrumbs?: { name: string; path: string }[];
  faqs?: { question: string; answer: string }[];
}

@Injectable({ providedIn: 'root' })
export class SeoService {
  private readonly document = inject(DOCUMENT);
  private readonly title = inject(Title);
  private readonly meta = inject(Meta);
  private readonly siteUrl = 'https://payrollsa.co.za';

  update(page: SeoPage): void {
    const canonicalUrl = `${this.siteUrl}${page.path}`;
    this.title.setTitle(page.title);
    this.meta.updateTag({ name: 'description', content: page.description });
    this.meta.updateTag({ name: 'robots', content: 'index,follow' });
    this.meta.updateTag({ property: 'og:type', content: page.type ?? 'website' });
    this.meta.updateTag({ property: 'og:title', content: page.title });
    this.meta.updateTag({ property: 'og:description', content: page.description });
    this.meta.updateTag({ property: 'og:url', content: canonicalUrl });
    this.meta.updateTag({ property: 'og:site_name', content: 'Payroll SA' });
    this.meta.updateTag({ name: 'twitter:card', content: 'summary' });
    this.meta.updateTag({ name: 'twitter:title', content: page.title });
    this.meta.updateTag({ name: 'twitter:description', content: page.description });

    let canonical = this.document.head.querySelector<HTMLLinkElement>('link[rel="canonical"]');
    if (!canonical) {
      canonical = this.document.createElement('link');
      canonical.rel = 'canonical';
      this.document.head.appendChild(canonical);
    }
    canonical.href = canonicalUrl;
    this.setJsonLd('seo-json-ld', this.buildSchema(page, canonicalUrl));
  }

  noIndex(): void {
    this.meta.updateTag({ name: 'robots', content: 'noindex,nofollow' });
  }

  private buildSchema(page: SeoPage, canonicalUrl: string): object {
    const graph: object[] = [
      {
        '@type': 'Organization',
        '@id': `${this.siteUrl}/#organization`,
        name: 'Payroll SA',
        url: this.siteUrl,
        logo: `${this.siteUrl}/icons/icon-512x512.png`
      },
      {
        '@type': page.type === 'article' ? 'BlogPosting' : 'SoftwareApplication',
        name: page.title,
        headline: page.title,
        description: page.description,
        url: canonicalUrl,
        applicationCategory: page.type === 'article' ? undefined : 'BusinessApplication',
        operatingSystem: page.type === 'article' ? undefined : 'Web',
        author: page.type === 'article' ? { '@type': 'Organization', name: page.author ?? 'Payroll SA' } : undefined,
        publisher: page.type === 'article' ? { '@id': `${this.siteUrl}/#organization` } : undefined,
        datePublished: page.published,
        dateModified: page.modified ?? page.published,
        mainEntityOfPage: page.type === 'article' ? canonicalUrl : undefined
      },
      {
        '@type': 'WebSite',
        name: 'Payroll SA',
        url: this.siteUrl
      }
    ];

    if (page.breadcrumbs?.length) {
      graph.push({
        '@type': 'BreadcrumbList',
        itemListElement: page.breadcrumbs.map((item, index) => ({
          '@type': 'ListItem', position: index + 1, name: item.name, item: `${this.siteUrl}${item.path}`
        }))
      });
    }

    if (page.faqs?.length) {
      graph.push({
        '@type': 'FAQPage',
        mainEntity: page.faqs.map(faq => ({
          '@type': 'Question', name: faq.question,
          acceptedAnswer: { '@type': 'Answer', text: faq.answer }
        }))
      });
    }

    return { '@context': 'https://schema.org', '@graph': graph };
  }

  private setJsonLd(id: string, schema: object): void {
    let script = this.document.head.querySelector<HTMLScriptElement>(`script#${id}`);
    if (!script) {
      script = this.document.createElement('script');
      script.id = id;
      script.type = 'application/ld+json';
      this.document.head.appendChild(script);
    }
    script.textContent = JSON.stringify(schema);
  }
}
