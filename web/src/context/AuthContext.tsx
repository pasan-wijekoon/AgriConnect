import React, { createContext, useContext, useState, useEffect } from 'react';
import { api, type User } from '../utils/api';

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
        setUser(parsed);
      } catch (e) {
        console.error('Failed to parse saved user', e);
        localStorage.removeItem('agriconnect_user');
      }
    }
    setIsLoading(false);
  }, []);

  const login = async (email: string, password: string) => {
    setIsLoading(true);
    try {
      const loggedUser = await api.login(email, password);
      setUser(loggedUser);
      localStorage.setItem('agriconnect_user', JSON.stringify(loggedUser));
      return loggedUser;
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
      setUser(newUser);
      localStorage.setItem('agriconnect_user', JSON.stringify(newUser));
      return newUser;
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
