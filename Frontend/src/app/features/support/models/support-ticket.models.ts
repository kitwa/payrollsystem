export enum SupportTicketType {
  TechnicalIssue = 0,
  BugReport = 1,
  FeatureRequest = 2,
  PayrollQuestion = 3,
  Other = 4
}

export enum SupportTicketStatus {
  Open = 0,
  InReview = 1,
  InProgress = 2,
  Resolved = 3,
  Closed = 4
}

export interface SupportTicket {
  id: string;
  ticketNumber: string;
  companyId: string;
  companyName: string;
  createdByUserId: string;
  createdByName: string;
  subject: string;
  type: SupportTicketType;
  status: SupportTicketStatus;
  createdAt: string;
  modifiedAt?: string;
  closedAt?: string;
}

export interface SupportTicketDetail extends SupportTicket {
  createdByEmail: string;
  description: string;
  closedByUserId?: string;
}

export interface SupportTicketPage {
  items: SupportTicket[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
}

export interface CreateSupportTicketRequest {
  subject: string;
  type: SupportTicketType;
  description: string;
}
