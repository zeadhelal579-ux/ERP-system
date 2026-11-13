// Defines the authenticated identity and shared React context.
import { createContext, useContext } from 'react';
import type { UserRole } from '@/lib/types/api';

export interface AuthenticatedUser {
  fullName: string;
  role: UserRole;
}

interface AuthContextValue {
  user: AuthenticatedUser | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  login: (username: string, password: string) => Promise<void>;
  logout: () => void;
}

export const AuthContext = createContext<AuthContextValue | undefined>(undefined);

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext);
  if (!context) throw new Error('useAuth must be used within AuthProvider');
  return context;
}
