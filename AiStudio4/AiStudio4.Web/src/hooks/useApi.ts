// src/hooks/useApi.ts
import { useCallback, useEffect } from 'react';
import { apiClient, useApiStore } from '@/services/api/apiClient';

// Generic hook factory for API calls
export function createApiHook<TParams = void, TResponse = any>(
  endpoint: string,
  method: 'GET' | 'POST' | 'PUT' | 'DELETE' = 'POST',
  options: {
    defaultParams?: TParams;
    queryKey?: string;
    transformResponse?: (data: any) => TResponse;
    onSuccess?: (data: TResponse) => void;
    onError?: (error: any) => void;
  } = {}
) {
  // Return a custom hook for this endpoint
  return (customOptions: {
    skip?: boolean;
    params?: TParams;
    onSuccess?: (data: TResponse) => void;
    onError?: (error: any) => void;
  } = {}) => {
    const queryKey = options.queryKey || endpoint;
    const {
      queries,
      loading,
      errors,
      setQueryData,
      setLoading,
      setError
    } = useApiStore();

    // Combine default params with custom params
    const params = {
      ...options.defaultParams,
      ...customOptions.params
    } as TParams;

    // Execute the query
    const execute = useCallback(async (executeParams?: TParams) => {
      const finalParams = executeParams || params;
      
      try {
        setLoading(queryKey, true);
        
        const response = await apiClient.request({
          url: endpoint,
          method,
          [method === 'GET' ? 'params' : 'data']: finalParams
        });
        
        const transformedData = options.transformResponse 
          ? options.transformResponse(response.data)
          : response.data;
        
        setQueryData(queryKey, transformedData);
        
        if (options.onSuccess) options.onSuccess(transformedData);
        if (customOptions.onSuccess) customOptions.onSuccess(transformedData);
        
        return transformedData;
      } catch (error) {
        setError(queryKey, error);
        
        if (options.onError) options.onError(error);
        if (customOptions.onError) customOptions.onError(error);
        
        throw error;
      }
    }, [params, queryKey]);

    // Auto-execute on mount if not skipped
    useEffect(() => {
      if (!customOptions.skip) {
        execute();
      }
    }, [execute, customOptions.skip]);

    return {
      data: queries[queryKey] as TResponse | undefined,
      loading: loading[queryKey] || false,
      error: errors[queryKey],
      execute,
      refetch: execute
    };
  };
}