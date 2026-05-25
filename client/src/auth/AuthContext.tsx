import { createContext, useContext, useMemo, useState, type ReactNode } from 'react';
import { api, setToken } from '../api/client';
import type { LoginResponse, Role } from '../types';

interface AuthState {
  email: string;
  fullName: string;
  role: Role;
}

interface AuthContextValue {
  user: AuthState | null;
  login: (email: string, password: string) => Promise<void>;
  logout: () => void;
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined);

const STORAGE_KEY = 'sm_user';

function loadUser(): AuthState | null {
  const raw = localStorage.getItem(STORAGE_KEY);
  return raw ? (JSON.parse(raw) as AuthState) : null;
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthState | null>(loadUser());

  const value = useMemo<AuthContextValue>(
    () => ({
      user,
      login: async (email, password) => {
        const res = await api.post<LoginResponse>('/api/auth/login', { email, password });
        setToken(res.token);
        const state: AuthState = { email: res.email, fullName: res.fullName, role: res.role };
        localStorage.setItem(STORAGE_KEY, JSON.stringify(state));
        setUser(state);
      },
      logout: () => {
        setToken(null);
        localStorage.removeItem(STORAGE_KEY);
        setUser(null);
      },
    }),
    [user],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext);
  if (!ctx) {
    throw new Error('useAuth must be used within AuthProvider');
  }
  return ctx;
}
