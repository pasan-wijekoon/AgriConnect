import React, { createContext, useContext, useState, useEffect } from 'react';
import { api, normalizeRole, normalizeUser, type User } from '../utils/marketApi';

interface AuthContextType {
  user: User | null;
  isLoading: boolean;
  login: (email: string, password: string) => Promise<User>;
  register: (data: {
    fullName: string;
    email: string;
    password: string;
    role: string;
    phone?: string;
    region?: string;
  }) => Promise<User>;
  logout: () => void;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const AuthProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [user, setUser] = useState<User | null>(null);
  const [isLoading, setIsLoading] = useState<boolean>(true);

  useEffect(() => {
    const saved = localStorage.getItem('agriconnect_user');
    if (saved) {
      try {
        const parsed = JSON.parse(saved);
        api.getMe().then((profile) => setUser({ ...profile, role: normalizeRole(profile.role), token: parsed.token })).catch(() => {
          localStorage.removeItem('agriconnect_user');
          setUser(null);
        }).finally(() => setIsLoading(false));
      } catch (e) {
        console.error('Failed to parse saved user', e);
        localStorage.removeItem('agriconnect_user');
        setIsLoading(false);
      }
    } else {
      setIsLoading(false);
    }
  }, []);

  const login = async (email: string, password: string) => {
    setIsLoading(true);
    try {
      const loggedUser = await api.login(email, password);
      const normalizedUser = normalizeUser(loggedUser);
      setUser(normalizedUser);
      localStorage.setItem('agriconnect_user', JSON.stringify(normalizedUser));
      return normalizedUser;
    } finally {
      setIsLoading(false);
    }
  };

  const register = async (data: {
    fullName: string;
    email: string;
    password: string;
    role: string;
    phone?: string;
    region?: string;
  }) => {
    setIsLoading(true);
    try {
      const newUser = await api.register(data);
      const normalizedUser = normalizeUser(newUser);
      setUser(normalizedUser);
      localStorage.setItem('agriconnect_user', JSON.stringify(normalizedUser));
      return normalizedUser;
    } finally {
      setIsLoading(false);
    }
  };

  const logout = () => {
    setUser(null);
    localStorage.removeItem('agriconnect_user');
  };

  return (
    <AuthContext.Provider value={{ user, isLoading, login, register, logout }}>
      {children}
    </AuthContext.Provider>
  );
};

export const useAuth = () => {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
};
