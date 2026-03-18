/**
 * API functions for ticket endpoints.
 */
import axiosClient from './axiosClient';
import { ApiResponse } from '../types/api.types';
import { TicketResponse, PagedResult } from '../types/ticket.types';

export interface GetTicketsParams {
  page?: number;
  pageSize?: number;
  status?: number; //Open, InProgress, OnHold, Resolved, Closed
}

// Role: Customer - get their own tickets with pagination and filters
//GetMyTickets - calls GET /api/tickets/my  : for customer dashboard
export const getMyTicketsApi = async (
  params: GetTicketsParams
): Promise<ApiResponse<PagedResult<TicketResponse>>> => {
  const response = await axiosClient.get('/tickets/my', { params });
  return response.data;
};

//
export interface CreateTicketRequest {
  title: string;
  description: string;
  categoryId: string;
  priority: number;
}

export const createTicketApi = async (
  data: CreateTicketRequest
): Promise<ApiResponse<string>> => {
  const response = await axiosClient.post<ApiResponse<string>>(
    '/tickets',
    data
  );
  return response.data;
};

// ── Ticket detail
export const getTicketById = (id: number | string) =>
  axiosClient.get(`/tickets/${id}`).then((r) => r.data.data);

// ── Comments and attachments
export const getComments = (ticketId: number | string) =>
  axiosClient
    .get(`/tickets/${ticketId}/comments`)
    .then((r) => r.data.data);

export const addComment = (
  ticketId: number | string,
  formData: FormData
) =>
  axiosClient.post(`/tickets/${ticketId}/comments`, formData, {
    headers: { "Content-Type": "multipart/form-data" },
  });

export const deleteAttachment = (
  ticketId: number | string,
  attachmentId: number
) =>
  axiosClient.delete(
    `/tickets/${ticketId}/attachments/${attachmentId}`
  );

// ── Status 
export const changeTicketStatus = (
  ticketId: number | string,
  status: string,
  note?: string
) =>
  axiosClient.patch(`/tickets/${ticketId}/status`, { status, note });

export const getStatusHistory = (ticketId: number | string) =>
  axiosClient
    .get(`/tickets/${ticketId}/status-history`)
    .then((r) => r.data.data);
