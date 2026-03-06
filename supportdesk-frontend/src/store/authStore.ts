/**
 * Global authentication state using Zustand.
 * 
 * CONCEPTS:
 * 
 * 1. State Management — stores data accessible by ANY component
 *    without prop drilling (passing data through many parent components).
 * 
 * 2. Zustand vs Redux
 *    Redux  → complex, boilerplate, good for large apps
 *    Zustand → simple, minimal code, good for most apps 
 * 
 * 3. Persist — saves state to localStorage automatically.
 *    User stays logged in after browser refresh 
 * 
 * 4. Why not Context API?
 *    Context re-renders ALL consumers on any change.
 *    Zustand only re-renders components using changed state 
 */
import { create } from 'zustand';
import { persist } from 'zustand/middleware';
import { UserDto } from '../types/auth.types';

// Shape of our auth state
interface AuthState {
  user: UserDto | null;
  accessToken: string | null;
  isAuthenticated: boolean;

  // Actions — functions that update state
  setAuth: (user: UserDto, token: string) => void;
  logout: () => void;
}

/**
 * useAuthStore — global auth state hook.
 * Use in any component: const { user, logout } = useAuthStore();
 * persist middleware → saves to localStorage automatically.
 */
const useAuthStore = create<AuthState>()(
  persist(
    (set) => ({
      // Initial state — not logged in
      user: null,
      accessToken: null,
      isAuthenticated: false,

      /**
       * setAuth — called after successful login.
       * Saves user info and token to state and localStorage.
       */
      setAuth: (user: UserDto, token: string) => {
        localStorage.setItem('accessToken', token);
        set({
          user,
          accessToken: token,
          isAuthenticated: true,
        });
      },

      /**
       * logout — clears all auth state.
       * Called on logout button click or 401 response.
       */
      logout: () => {
        localStorage.removeItem('accessToken');
        set({
          user: null,
          accessToken: null,
          isAuthenticated: false,
        });
      },
    }),
    {
      name: 'auth-storage', // localStorage key name
    }
  )
);

export default useAuthStore;