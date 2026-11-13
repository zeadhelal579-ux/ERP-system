// Manages the authenticated session and clears cached data on logout.
import {

  useCallback,

  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from 'react';
import { apiClient, registerUnauthorizedHandler, setAuthToken, unwrap } from '@/lib/api-client';
import type { ApiResponse, LoginResponse } from '@/lib/types/api';

import { useQueryClient } from '@tanstack/react-query';
import { AuthContext, type AuthenticatedUser } from './useAuth';

export function AuthProvider({ children }: { children: ReactNode }) {
  const cache = useQueryClient();
  const [user, setUser] = useState<AuthenticatedUser | null>(null);
  const [isLoading, setIsLoading] = useState(false);

  const logout = useCallback(() => {
    setAuthToken(null);
    setUser(null);
    cache.clear();
  }, [cache]);

  useEffect(() => {
    registerUnauthorizedHandler(logout);
    return () => registerUnauthorizedHandler(null);
  }, [logout]);

  const login = useCallback(async (username: string, password: string) => {
    setIsLoading(true);
    try {
      const result = await unwrap<LoginResponse>(
        apiClient.post<ApiResponse<LoginResponse>>('/auth/login', { username, password })
      );
      setAuthToken(result.token);
      setUser({ fullName: result.fullName, role: result.role });
    } finally {
      setIsLoading(false);
    }
  }, []);

  const value = useMemo(
    () => ({ user, isAuthenticated: user !== null, isLoading, login, logout }),
    [user, isLoading, login, logout]
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

