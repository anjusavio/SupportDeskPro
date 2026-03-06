/**
 * TypeScript interfaces for authentication.
 * 
 * CONCEPT: TypeScript interfaces define the SHAPE of data.
 * Like C# classes but only for type checking — no runtime existence.
 * Catches type errors at compile time before running the app.
 */

// What we send to login API
export interface LoginRequest {
  email: string;
  password: string;
}

// What we send to register API
export interface RegisterRequest {
  firstName: string;
  lastName: string;
  email: string;
  password: string;
  confirmPassword: string;
  tenantSlug: string;
}

// User info from JWT token
export interface UserDto {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  role: string;          // "Admin" | "Agent" | "Customer"
  tenantId: string;
  tenantName: string;
}

// Login API response
export interface LoginResponse {
  accessToken: string;
  refreshToken: string;
  expiresIn: number;
  user: UserDto;
}