/**
 * TypeScript interfaces for ticket management.
 * 
 * CONCEPT: Interfaces vs Types in TypeScript
 * interface — extendable, good for objects (use for API responses)
 * type      — flexible, good for unions (use for status/priority)
 */

// Ticket status union type — only these values allowed
export type TicketStatus = 'Open' | 'InProgress' | 'Resolved' | 'Closed';

// Ticket priority union type
export type TicketPriority = 'Low' | 'Medium' | 'High' | 'Critical';

// Ticket list item
export interface TicketResponse {
  id: string;
  ticketNumber: number;
  title: string;
  description: string;
  status: TicketStatus;
  priority: TicketPriority;
  categoryId: string;
  categoryName: string;
  customerId: string;
  customerName: string;
  assignedAgentId: string | null;
  assignedAgentName: string | null;
  slaFirstResponseDueAt: string | null;
  slaResolutionDueAt: string | null;
  isSLABreached: boolean;
  lastActivityAt: string;
  createdAt: string;
}

// Create ticket request
export interface CreateTicketRequest {
  title: string;
  description: string;
  categoryId: string;
  priority: number;
}

// Paginated response wrapper
export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}