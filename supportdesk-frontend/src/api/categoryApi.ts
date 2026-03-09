import axiosClient from './axiosClient';
import { ApiResponse } from '../types/api.types';

export interface CategorySummaryResponse {
  id: string;
  name: string;
  parentCategoryId: string | null;
  parentCategoryName: string | null;
}

export const getActiveCategoriesApi = async (): Promise <
  ApiResponse<CategorySummaryResponse[]>
> => {
  const response = await axiosClient.get<ApiResponse<CategorySummaryResponse[]>>(
    '/categories/active'
  );
  return response.data;
};