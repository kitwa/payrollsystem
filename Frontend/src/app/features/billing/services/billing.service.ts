import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../environments/environment';
import { PlanDto, SubscriptionSummaryDto, UpgradeSubscriptionDto } from '../models/billing.models';

@Injectable({ providedIn: 'root' })
export class BillingService {
  private readonly url = `${environment.apiUrl}subscription`;

  constructor(private http: HttpClient) {}

  getMine() {
    return this.http.get<SubscriptionSummaryDto>(this.url);
  }

  getPlans() {
    return this.http.get<PlanDto[]>(`${this.url}/plans`);
  }

  upgrade(dto: UpgradeSubscriptionDto) {
    return this.http.post<SubscriptionSummaryDto>(`${this.url}/upgrade`, dto);
  }

  cancel() {
    return this.http.post<void>(`${this.url}/cancel`, {});
  }
}
