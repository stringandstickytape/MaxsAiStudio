import { useState, useRef, useCallback, useEffect } from 'react';
import { apiClient } from '@/services/api/apiClient';

export enum ErrorCategory {
  API = 'api_error',
  NETWORK = 'network_error',
  AUTH = 'auth_error',
  VALIDATION = 'validation_error',
  WEBSOCKET = 'websocket_error',
  UNEXPECTED = 'unexpected_error'
}

export interface AppError {
  message: string;
  category: ErrorCategory;
  code?: string;
  data?: any;
  originalError?: Error;
}

export function normalizeError(error: any): string | AppError {
  
  if (error && error.category && error.message) {
    return error as AppError;
  }
  
  
  if (error?.response) {
    return {
      message: error.response.data?.message || error.message || 'API request failed',
      category: ErrorCategory.API,
      code: `${error.response.status}`,
      data: error.response.data,
      originalError: error
    };
  }
  
  
  if (error?.request) {
    return {
      message: 'Network error occurred',
      category: ErrorCategory.NETWORK,
      originalError: error
    };
  }
  
  
  if (error instanceof Error) {
    return error.message;
  }
  
  
  if (typeof error === 'string') {
    return error;
  }
  
  
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
    //console.log(`[API Debug] Making ${method} request to ${endpoint}`);
    //console.log('[API Debug] Request params:', params);
    
    try {
      const requestConfig = {
        url: endpoint,
        method,
        [method === 'GET' ? 'params' : 'data']: params || {},
      };
      //console.log('[API Debug] Request config:', requestConfig);
      
      const response = await apiClient.request(requestConfig);
      //console.log(`[API Debug] Response from ${endpoint}:`, response);

      const data = response.data;
      //console.log(`[API Debug] Response data from ${endpoint}:`, data);

      if (data && typeof data === 'object' && 'success' in data && data.success === false) {
        console.error(`[API Debug] Request to ${endpoint} returned success: false with error:`, data.error);
        throw new Error(data.error || `API request to ${endpoint} failed`);
      }

      const transformedData = options?.transformResponse ? options.transformResponse(data) : data;
      //console.log(`[API Debug] Transformed response data from ${endpoint}:`, transformedData);
      return transformedData;
    } catch (err) {
      console.error(`[API Debug] Error in API request to ${endpoint}:`, err);
      throw err;
    }
  };
}