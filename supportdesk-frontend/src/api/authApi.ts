/**
 * API functions for authentication endpoints.
 * 
 * CONCEPT: Separation of Concerns
 * API calls isolated in /api folder — not mixed with UI components.
 * Components call these functions — don't use axios directly.
 * Easy to mock in tests — swap real API with fake data 
 */
import axiosClient from './axiosClient';
import { ApiResponse } from '../types/api.types';
import { LoginRequest, LoginResponse, RegisterRequest } from '../types/auth.types';

/**
 * Calls POST /api/auth/login
 * Returns JWT access token and user info on success.
 */
export const loginApi = async (
  data: LoginRequest
): Promise<ApiResponse<LoginResponse>> => {
  const response = await axiosClient.post<ApiResponse<LoginResponse>>(
    '/auth/login',
    data
  );
  return response.data;
};

/**
 * Calls POST /api/auth/register
 * Creates new customer account for specified tenant.
 */
export const registerApi = async (
  data: RegisterRequest
): Promise<ApiResponse<string>> => {
  const response = await axiosClient.post<ApiResponse<string>>(
    '/auth/register',
    data
  );
  return response.data;
};