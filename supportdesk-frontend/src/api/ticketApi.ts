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