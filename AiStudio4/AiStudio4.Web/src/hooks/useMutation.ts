// src/hooks/useMutation.ts
import { useState, useCallback } from 'react';
import { apiClient } from '@/services/api/apiClient';

interface UseMutationOptions<TData, TError, TVariables> {
  onSuccess?: (data: TData, variables: TVariables) => void;
  onError?: (error: TError, variables: TVariables) => void;
  onSettled?: (data: TData | undefined, error: TError | null, variables: TVariables) => void;
}

export function useMutation<TData = unknown, TError = Error, TVariables = void>(
  endpoint: string,
  options: UseMutationOptions<TData, TError, TVariables> = {}
) {
  const [data, setData] = useState<TData | undefined>(undefined);
  const [error, setError] = useState<TError | null>(null);
  const [isLoading, setIsLoading] = useState(false);

  const mutate = useCallback(async (variables: TVariables) => {
    setIsLoading(true);
    setError(null);
    
    try {
      const response = await apiClient.post(endpoint, variables);
      const responseData = response.data as TData;
      
      setData(responseData);
      
      if (options.onSuccess) {
        options.onSuccess(responseData, variables);
      }
      
      if (options.onSettled) {
        options.onSettled(responseData, null, variables);
      }
      
      return responseData;
    } catch (err) {
      const typedError = err as TError;
      setError(typedError);
      
      if (options.onError) {
        options.onError(typedError, variables);
      }
      
      if (options.onSettled) {
        options.onSettled(undefined, typedError, variables);
      }
      
      throw typedError;
    } finally {
      setIsLoading(false);
    }
  }, [endpoint, options]);

  return {
    mutate,
    data,
    error,
    isLoading,
    reset: () => {
      setData(undefined);
      setError(null);
    }
  };
}