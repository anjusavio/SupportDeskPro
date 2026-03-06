/**
 * Axios HTTP client configured for SupportDeskPro API.
 * 
 * CONCEPTS:
 * 
 * 1. Interceptors — middleware for every HTTP request/response
 *    Request interceptor  → runs BEFORE every request leaves browser
 *    Response interceptor → runs AFTER every response arrives
 * 
 * 2. JWT Auto-Attach — request interceptor reads token from
 *    localStorage and adds to Authorization header automatically.
 *    No need to manually add token in every API call.
 * 
 * 3. 401 Handling — response interceptor catches unauthorized
 *    responses and redirects to login automatically.
 */
import axios from 'axios';

// Create axios instance with base configuration
const axiosClient = axios.create({
  baseURL: 'https://localhost:7230/api', // .NET API URL
  headers: {
    'Content-Type': 'application/json',
  },
});

/**
 * REQUEST INTERCEPTOR
 * Runs before every API call — attaches JWT token automatically.
 * Without this: every API call needs manual Authorization header.
 * With this: token attached to ALL requests automatically ✅
 */
axiosClient.interceptors.request.use(
  (config) => {
    // Read token from localStorage
    const token = localStorage.getItem('accessToken');

    // Attach to Authorization header if token exists
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }

    return config;
  },
  (error) => Promise.reject(error)
);

/**
 * RESPONSE INTERCEPTOR
 * Runs after every API response arrives.
 * Handles 401 Unauthorized — clears token and redirects to login.
 * Without this: expired token shows blank screen or weird errors.
 * With this: auto-redirects to login when token expires ✅
 */
axiosClient.interceptors.response.use(
  // Success — pass response through unchanged
  (response) => response,

  // Error — handle based on status code
  (error) => {
    if (error.response?.status === 401) {
      // Token expired or invalid — clear storage and redirect
      localStorage.removeItem('accessToken');
      localStorage.removeItem('user');
      window.location.href = '/login';
    }
    return Promise.reject(error);
  }
);

export default axiosClient;