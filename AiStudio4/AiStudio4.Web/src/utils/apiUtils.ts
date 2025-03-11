// src/utils/apiUtils.ts
import { useState, useRef, useCallback, useEffect } from 'react';
import { apiClient } from '@/services/api/apiClient';

/**
 * Helper function for API error normalization
 */
export function normalizeError(error: any): string {
  if (typeof error === 'string') return error;
  if (error instanceof Error) return error.message;
  if (error && typeof error.message === 'string') return error.message;
  return 'An unknown error occurred';
}

/**
 * Common type for hooks that manage API operations
 */
export interface ApiOperationState {
  isLoading: boolean;
  error: string | null;
  initialized: boolean;
}

/**
 * Generic hook factory for creating API operation hooks with common patterns
 */
export function createApiOperationHook<T = any>(options?: {
  initializeOnMount?: boolean;
  onError?: (error: any) => void;
}) {
  return function useApiOperation() {
    const [isLoading, setIsLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const initialized = useRef(false);
    
    const executeApiCall = useCallback(async <R>(apiCall: () => Promise<R>): Promise<R | null> => {
      try {
        setIsLoading(true);
        setError(null);
        const result = await apiCall();
        return result;
      } catch (err) {
        const normalizedError = normalizeError(err);
        setError(normalizedError);
        if (options?.onError) options.onError(err);
        console.error('API operation error:', err);
        return null;
      } finally {
        setIsLoading(false);
      }
    }, []);
    
    // Initialize on mount if configured
    useEffect(() => {
      if (options?.initializeOnMount && !initialized.current) {
        initialized.current = true;
      }
    }, []);
    
    return {
      isLoading,
      error,
      initialized: initialized.current,
      executeApiCall,
      clearError: () => setError(null)
    };
  };
}

/**
 * Hook for tracking API call state
 */
export function useApiCallState() {
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  
  const executeApiCall = useCallback(async <T>(apiCall: () => Promise<T>): Promise<T | null> => {
    try {
      setIsLoading(true);
      setError(null);
      return await apiCall();
    } catch (err) {
      const normalizedError = normalizeError(err);
      setError(normalizedError);
      console.error('API call error:', err);
      return null;
    } finally {
      setIsLoading(false);
    }
  }, []);
  
  return {
    isLoading,
    error,
    executeApiCall,
    setIsLoading,
    setError,
    clearError: () => setError(null)
  };
}

/**
 * Utility function for creating standardized API request functions
 */
export function createApiRequest<TParams, TResponse>(
  endpoint: string,
  method: 'GET' | 'POST' | 'PUT' | 'DELETE' = 'POST',
  options?: {
    transformResponse?: (data: any) => TResponse;
  }
) {
  return async (params?: TParams): Promise<TResponse> => {
    try {
      const response = await apiClient.request({
        url: endpoint,
        method,
        [method === 'GET' ? 'params' : 'data']: params || {}
      });
      
      const data = response.data;
      
      if (data && typeof data === 'object' && 'success' in data && data.success === false) {
        throw new Error(data.error || `API request to ${endpoint} failed`);
      }
      
      return options?.transformResponse ? options.transformResponse(data) : data;
    } catch (err) {
      console.error(`Error in API request to ${endpoint}:`, err);
      throw err;
    }
  };
}