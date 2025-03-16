
import { useApiStore } from '@/services/api/apiClient';


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


export function normalizeError(error: any): AppError {
  
  if (error && error.category) {
    return error as AppError;
  }
  
  
  if (error?.response) {
    return createAppError(
      error.response.data?.message || error.message || 'API request failed',
      ErrorCategory.API,
      `${error.response.status}`,
      error.response.data,
      error
    );
  }
  
  
  if (error?.request) {
    return createAppError(
      'Network error occurred',
      ErrorCategory.NETWORK,
      undefined,
      undefined,
      error
    );
  }
  
  
  if (error instanceof Error) {
    return createAppError(error.message, ErrorCategory.UNEXPECTED, undefined, undefined, error);
  }
  
  
  if (typeof error === 'string') {
    return createAppError(error);
  }
  
  
  return createAppError('An unknown error occurred');
}


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
    
    if (setLoading) {
      setLoading(true);
    }
    
    if (setError) {
      setError(null);
    }
    
    
    if (queryKey) {
      useApiStore.getState().setLoading(queryKey, true);
    }
    
    
    const result = await operation();
    
    
    if (onSuccess) {
      onSuccess(result);
    }
    
    
    if (queryKey) {
      useApiStore.getState().setQueryData(queryKey, result);
    }
    
    return result;
  } catch (error) {
    
    const appError = normalizeError(error);
    
    
    if (errorMessage) {
      appError.message = errorMessage;
    }
    
    
    if (category) {
      appError.category = category;
    }
    
    
    if (setError) {
      setError(appError);
    }
    
    
    if (queryKey) {
      useApiStore.getState().setError(queryKey, appError);
    }
    
    
    if (onError) {
      onError(appError);
    }
    
    
    console.error('Operation failed:', appError);
    
    
    if (throwError) {
      throw appError;
    }
    
    return null;
  } finally {
    
    if (setLoading) {
      setLoading(false);
    }
    
    
    if (queryKey) {
      useApiStore.getState().setLoading(queryKey, false);
    }
  }
}


export function useErrorHandler() {
  const [error, setError] = useState<AppError | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  
  
  const clearError = useCallback(() => {
    setError(null);
  }, []);
  
  
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
