/**
 * API functions for notification endpoints.
 * 
 * CONCEPT: API Layer Pattern
 * All API calls in /api folder — never in components directly.
 * Components stay clean — only UI logic, no HTTP logic
 */
import axiosClient from './axiosClient';
import { ApiResponse } from '../types/api.types';

export interface UnreadCountResponse {
  count: number;
}

/**
 * Calls GET /api/notifications/unread-count
 * Returns unread notification count for bell badge.
 */
export const getUnreadCountApi = async (): Promise<
  ApiResponse<UnreadCountResponse>
> => {
  const response = await axiosClient.get<
    ApiResponse<UnreadCountResponse>
  >('/notifications/unread-count');
  return response.data;
};