// src/utils/errorHandler.ts
import { useApiStore } from '@/services/api/apiClient';

// Error categories for consistent handling
export enum ErrorCategory {
  API = 'api_error',
  NETWORK = 'network_error',
  AUTH = 'auth_error',
  VALIDATION = 'validation_error',
  WEBSOCKET = 'websocket_error',
  UNEXPECTED = 'unexpected_error'
}

// Standard error structure for the application
export interface AppError {
  message: string;
  category: ErrorCategory;
  code?: string;
  data?: any;
  originalError?: Error;
}

// Create a standardized error object
export function createAppError(
  message: string,
  category: ErrorCategory = ErrorCategory.UNEXPECTED,
  code?: string,
  data?: any,
  originalError?: Error
): AppError {
  return {
    message,
    category,
    code,
    data,
    originalError
  };
}

// Normalize different error types to our standard AppError format
export function normalizeError(error: any): AppError {
  // Already in the right format
  if (error && error.category) {
    return error as AppError;
  }
  
  // Handle Axios errors
  if (error?.response) {
    return createAppError(
      error.response.data?.message || error.message || 'API request failed',
      ErrorCategory.API,
      `${error.response.status}`,
      error.response.data,
      error
    );
  }
  
  // Handle network errors
  if (error?.request) {
    return createAppError(
      'Network error occurred',
      ErrorCategory.NETWORK,
      undefined,
      undefined,
      error
    );
  }
  
  // Handle standard Error objects
  if (error instanceof Error) {
    return createAppError(error.message, ErrorCategory.UNEXPECTED, undefined, undefined, error);
  }
  
  // Handle string errors
  if (typeof error === 'string') {
    return createAppError(error);
  }
  
  // Default case
  return createAppError('An unknown error occurred');
}

// Safely execute an async function with standardized error handling
export async function safeExecute<T>(
  operation: () => Promise<T>,
  options?: {
    errorMessage?: string;
    category?: ErrorCategory;
    setLoading?: (isLoading: boolean) => void;
    setError?: (error: AppError | null) => void;
    onSuccess?: (result: T) => void;
    onError?: (error: AppError) => void;
    throwError?: boolean;
    queryKey?: string;
  }
): Promise<T | null> {
  const {
    errorMessage,
    category = ErrorCategory.UNEXPECTED,
    setLoading,
    setError,
    onSuccess,
    onError,
    throwError = false,
    queryKey
  } = options || {};
  
  try {
    // Set loading state if provided
    if (setLoading) {
      setLoading(true);
    }
    // Clear any previous errors
    if (setError) {
      setError(null);
    }
    
    // If we have a queryKey, use ApiStore's loading state
    if (queryKey) {
      useApiStore.getState().setLoading(queryKey, true);
    }
    
    // Execute the operation
    const result = await operation();
    
    // Handle success callback
    if (onSuccess) {
      onSuccess(result);
    }
    
    // If we have a queryKey, update the query data
    if (queryKey) {
      useApiStore.getState().setQueryData(queryKey, result);
    }
    
    return result;
  } catch (error) {
    // Normalize the error
    const appError = normalizeError(error);
    
    // Override the error message if provided
    if (errorMessage) {
      appError.message = errorMessage;
    }
    
    // Override the error category if provided
    if (category) {
      appError.category = category;
    }
    
    // Set error state if provided
    if (setError) {
      setError(appError);
    }
    
    // If we have a queryKey, update the error in ApiStore
    if (queryKey) {
      useApiStore.getState().setError(queryKey, appError);
    }
    
    // Handle error callback
    if (onError) {
      onError(appError);
    }
    
    // Log the error
    console.error('Operation failed:', appError);
    
    // Rethrow if requested
    if (throwError) {
      throw appError;
    }
    
    return null;
  } finally {
    // Reset loading state
    if (setLoading) {
      setLoading(false);
    }
    
    // If we have a queryKey, reset loading state in ApiStore
    if (queryKey) {
      useApiStore.getState().setLoading(queryKey, false);
    }
  }
}

// Hook for component-level error handling
export function useErrorHandler() {
  const [error, setError] = useState<AppError | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  
  // Clear the error
  const clearError = useCallback(() => {
    setError(null);
  }, []);
  
  // Execute with error handling
  const execute = useCallback(
    async <T>(operation: () => Promise<T>, options?: Omit<Parameters<typeof safeExecute>[1], 'setLoading' | 'setError'>): Promise<T | null> => {
      return safeExecute(operation, {
        ...options,
        setLoading,
        setError
      });
    },
    []
  );
  
  return { error, isLoading, clearError, execute, setError, setIsLoading };
}
