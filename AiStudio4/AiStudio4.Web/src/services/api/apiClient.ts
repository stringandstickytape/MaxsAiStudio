// src/services/api/apiClient.ts
import axios from 'axios';
import { create } from 'zustand';

// Basic axios instance with common config
export const apiClient = axios.create({
  baseURL: '/',
  headers: {
    'Content-Type': 'application/json',
  },
});

// Add request interceptor for client ID
apiClient.interceptors.request.use((config) => {
  const clientId = localStorage.getItem('clientId');
    if (clientId) {
    config.headers['X-Client-Id'] = clientId;
  }
  return config;
});

// Add response interceptor to standardize error handling
apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    // Create standardized error object
    const errorResponse = {
      message: error.message || 'An unknown error occurred',
      status: error.response?.status,
      data: error.response?.data,
    };
    
    // Console error for debugging
    console.error('API Error:', errorResponse);
    
    // Return rejected promise with standardized error
    return Promise.reject(errorResponse);
  }
);

// Create a Zustand store for API state management
interface ApiState {
  // Global API state
  queries: Record<string, any>;
  loading: Record<string, boolean>;
  errors: Record<string, any>;
  
  // Actions
  setQueryData: (key: string, data: any) => void;
  setLoading: (key: string, isLoading: boolean) => void;
  setError: (key: string, error: any) => void;
  resetQuery: (key: string) => void;
}

export const useApiStore = create<ApiState>((set) => ({
  queries: {},
  loading: {},
  errors: {},
  
  setQueryData: (key, data) => set((state) => ({
    queries: { ...state.queries, [key]: data },
    loading: { ...state.loading, [key]: false }
  })),
  
  setLoading: (key, isLoading) => set((state) => ({
    loading: { ...state.loading, [key]: isLoading }
  })),
  
  setError: (key, error) => set((state) => ({
    errors: { ...state.errors, [key]: error },
    loading: { ...state.loading, [key]: false }
  })),
  
  resetQuery: (key) => set((state) => {
    const { [key]: _, ...remainingQueries } = state.queries;
    const { [key]: __, ...remainingLoading } = state.loading;
    const { [key]: ___, ...remainingErrors } = state.errors;
    
    return {
      queries: remainingQueries,
      loading: remainingLoading,
      errors: remainingErrors
    };
  })
}));