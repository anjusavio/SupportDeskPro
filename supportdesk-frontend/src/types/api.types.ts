/**
 * Generic API response wrapper matching backend ApiResponse<T>.
 * 
 * CONCEPT: Generic Types in TypeScript
 * <T> is a placeholder — replaced with actual type when used.
 * ApiResponse<LoginResponse> → T becomes LoginResponse
 * ApiResponse<TicketResponse[]> → T becomes TicketResponse[]
 * Same pattern as C# generics.
 */
export interface ApiResponse<T> {
  success: boolean;
  data: T | null;
  message: string | null;
  errors: Record<string, string[]> | null;
}