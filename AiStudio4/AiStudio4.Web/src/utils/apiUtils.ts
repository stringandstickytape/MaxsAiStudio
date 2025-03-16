
import { useState, useRef, useCallback, useEffect } from 'react';
import { apiClient } from '@/services/api/apiClient';

/**
 * Error categories for API operations
 */
export enum ErrorCategory {
  API = 'api_error',
  NETWORK = 'network_error',
  AUTH = 'auth_error',
  VALIDATION = 'validation_error',
  WEBSOCKET = 'websocket_error',
  UNEXPECTED = 'unexpected_error'
}

/**
 * Normalized error structure
 */
export interface AppError {
  message: string;
  category: ErrorCategory;
  code?: string;
  data?: any;
  originalError?: Error;
}

/**
 * Normalizes different error types into a consistent structure
 */
export function normalizeError(error: any): string | AppError {
  // Handle already normalized errors
  if (error && error.category && error.message) {
    return error as AppError;
  }
  
  // Handle axios response errors
  if (error?.response) {
    return {
      message: error.response.data?.message || error.message || 'API request failed',
      category: ErrorCategory.API,
      code: `${error.response.status}`,
      data: error.response.data,
      originalError: error
    };
  }
  
  // Handle network errors
  if (error?.request) {
    return {
      message: 'Network error occurred',
      category: ErrorCategory.NETWORK,
      originalError: error
    };
  }
  
  // Handle standard errors
  if (error instanceof Error) {
    return error.message;
  }
  
  // Handle string errors
  if (typeof error === 'string') {
    return error;
  }
  
  // Default case
  return 'An unknown error occurred';
}

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
    clearError: () => setError(null),
  };
}

export function createApiRequest<TParams, TResponse>(
  endpoint: string,
  method: 'GET' | 'POST' | 'PUT' | 'DELETE' = 'POST',
  options?: {
    transformResponse?: (data: any) => TResponse;
  },
) {
  return async (params?: TParams): Promise<TResponse> => {
    try {
      const response = await apiClient.request({
        url: endpoint,
        method,
        [method === 'GET' ? 'params' : 'data']: params || {},
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
