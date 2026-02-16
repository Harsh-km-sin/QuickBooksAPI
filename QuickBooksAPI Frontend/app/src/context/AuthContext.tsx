import React, { createContext, useContext, useState, useEffect, useCallback } from 'react';
import { authApi, getToken, setToken, clearAuth, parseJwt, setRealmId, getRealmId } from '@/api/client';
import type { ApiResponse } from '@/types';
import { toast } from 'sonner';

interface AuthContextType {
  isAuthenticated: boolean;
  isLoading: boolean;
  user: {
    userId: string;
    name: string;
    realmIds: string[];
  } | null;
  currentRealmId: string | null;
  login: (email: string, password: string) => Promise<boolean>;
  signUp: (data: {
    firstName: string;
    lastName: string;
    username: string;
    email: string;
    password: string;
  }) => Promise<boolean>;
  logout: () => void;
  setCurrentRealm: (realmId: string) => void;
  checkAuth: () => boolean;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

function parseRealmIds(value: unknown): string[] {
  if (Array.isArray(value)) return value;
  if (typeof value === 'string') {
    try {
      const parsed = JSON.parse(value);
      return Array.isArray(parsed) ? parsed : [];
    } catch {
      return [];
    }
  }
  return [];
}

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [isAuthenticated, setIsAuthenticated] = useState(false);
  const [isLoading, setIsLoading] = useState(true);
  const [user, setUser] = useState<{
    userId: string;
    name: string;
    realmIds: string[];
  } | null>(null);
  const [currentRealmId, setCurrentRealmId] = useState<string | null>(null);

  // Check authentication status on mount
  useEffect(() => {
    const initAuth = () => {
      const token = getToken();
      if (token) {
        const claims = parseJwt(token);
        if (claims) {
          const realmIdsArr = parseRealmIds(claims.RealmIds);
          setUser({
            userId: claims.UserId,
            name: claims.Name ?? 'User',
            realmIds: realmIdsArr,
          });
          setIsAuthenticated(true);
          
          // Restore realm ID from storage or use first available
          const storedRealmId = getRealmId();
          if (storedRealmId && realmIdsArr.includes(storedRealmId)) {
            setCurrentRealmId(storedRealmId);
          } else if (realmIdsArr.length > 0) {
            setCurrentRealmId(realmIdsArr[0]);
            setRealmId(realmIdsArr[0]);
          }
        } else {
          clearAuth();
        }
      }
      setIsLoading(false);
    };

    initAuth();
  }, []);

  const login = useCallback(async (email: string, password: string): Promise<boolean> => {
    try {
      setIsLoading(true);
      const response: ApiResponse<string> = await authApi.login(email, password);
      
      if (response.success && response.data) {
        setToken(response.data);
        const claims = parseJwt(response.data);
        
        if (claims) {
          const realmIdsArr = parseRealmIds(claims.RealmIds);
          setUser({
            userId: claims.UserId,
            name: claims.Name ?? 'User',
            realmIds: realmIdsArr,
          });
          setIsAuthenticated(true);
          
          // Set default realm if available
          if (realmIdsArr.length > 0) {
            setCurrentRealmId(realmIdsArr[0]);
            setRealmId(realmIdsArr[0]);
          }
          
          const displayName = claims.Name ?? 'User';
          toast.success('Login successful', {
            description: `Welcome back, ${displayName}!`,
          });
          return true;
        }
      }
      
      toast.error('Login failed', {
        description: response.message || 'Invalid credentials',
      });
      return false;
    } catch (error) {
      toast.error('Login failed', {
        description: error instanceof Error ? error.message : 'An unexpected error occurred',
      });
      return false;
    } finally {
      setIsLoading(false);
    }
  }, []);

  const signUp = useCallback(async (data: {
    firstName: string;
    lastName: string;
    username: string;
    email: string;
    password: string;
  }): Promise<boolean> => {
    try {
      setIsLoading(true);
      const response: ApiResponse<number> = await authApi.signUp(data);
      
      if (response.success) {
        toast.success('Account created successfully', {
          description: 'Please log in with your new account.',
        });
        return true;
      }
      
      toast.error('Sign up failed', {
        description: response.message || 'Unable to create account',
      });
      return false;
    } catch (error) {
      toast.error('Sign up failed', {
        description: error instanceof Error ? error.message : 'An unexpected error occurred',
      });
      return false;
    } finally {
      setIsLoading(false);
    }
  }, []);

  const logout = useCallback(() => {
    clearAuth();
    setUser(null);
    setIsAuthenticated(false);
    setCurrentRealmId(null);
    toast.info('Logged out successfully');
    window.location.href = '/login';
  }, []);

  const setCurrentRealm = useCallback((realmId: string) => {
    setCurrentRealmId(realmId);
    setRealmId(realmId);
    toast.success('Company switched', {
      description: `Now viewing data for company: ${realmId}`,
    });
    // Reload page to refresh data with new realm
    window.location.reload();
  }, []);

  const checkAuth = useCallback((): boolean => {
    const token = getToken();
    if (!token) {
      setIsAuthenticated(false);
      setUser(null);
      return false;
    }
    return true;
  }, []);

  const value: AuthContextType = {
    isAuthenticated,
    isLoading,
    user,
    currentRealmId,
    login,
    signUp,
    logout,
    setCurrentRealm,
    checkAuth,
  };

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (context === undefined) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
}
