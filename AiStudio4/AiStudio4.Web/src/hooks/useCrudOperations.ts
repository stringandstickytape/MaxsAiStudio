// src/hooks/useCrudOperations.ts
import { useState, useCallback } from 'react';
import { apiClient } from '@/services/api/apiClient';

/**
 * Generic hook for CRUD operations on a resource
 */
export function useCrudOperations<T, CreateParams = Omit<T, 'guid'>, UpdateParams = T, IdType = string>(
  resourceName: string,
  options: {
    idField?: string;
    createEndpoint?: string;
    readEndpoint?: string;
    updateEndpoint?: string;
    deleteEndpoint?: string;
    listEndpoint?: string;
    transformResponse?: (data: any) => any;
  } = {}
) {
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  
  // Default ID field is 'guid'
  const idField = options.idField || 'guid';
  
  // Default endpoints based on resourceName
  const endpoints = {
    create: options.createEndpoint || `/api/create${resourceName}`,
    read: options.readEndpoint || `/api/get${resourceName}`,
    update: options.updateEndpoint || `/api/update${resourceName}`,
    delete: options.deleteEndpoint || `/api/delete${resourceName}`,
    list: options.listEndpoint || `/api/get${resourceName}s`
  };
  
  // Transform response data if provided
  const transformResponse = options.transformResponse || ((data) => data);
  
  /**
   * Create a new resource
   */
  const create = useCallback(async (params: CreateParams): Promise<T> => {
    try {
      setIsLoading(true);
      setError(null);
      
      const response = await apiClient.post(endpoints.create, params);
      const data = response.data;
      
      if (!data.success) {
        throw new Error(data.error || `Failed to create ${resourceName}`);
      }
      
      return transformResponse(data[resourceName.toLowerCase()] || data);
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : `Unknown error creating ${resourceName}`;
      setError(errorMessage);
      console.error(`Error creating ${resourceName}:`, err);
      throw err;
    } finally {
      setIsLoading(false);
    }
  }, [endpoints.create, resourceName, transformResponse]);
  
  /**
   * Read a specific resource by ID
   */
  const read = useCallback(async (id: IdType): Promise<T | null> => {
    try {
      setIsLoading(true);
      setError(null);
      
      const response = await apiClient.post(endpoints.read, { [`${resourceName.toLowerCase()}Id`]: id });
      const data = response.data;
      
      if (!data.success) {
        throw new Error(data.error || `Failed to get ${resourceName}`);
      }
      
      return transformResponse(data[resourceName.toLowerCase()] || data);
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : `Unknown error reading ${resourceName}`;
      setError(errorMessage);
      console.error(`Error reading ${resourceName}:`, err);
      return null;
    } finally {
      setIsLoading(false);
    }
  }, [endpoints.read, resourceName, idField, transformResponse]);
  
  /**
   * Update an existing resource
   */
  const update = useCallback(async (params: UpdateParams): Promise<T> => {
    try {
      setIsLoading(true);
      setError(null);
      
      const response = await apiClient.post(endpoints.update, params);
      const data = response.data;
      
      if (!data.success) {
        throw new Error(data.error || `Failed to update ${resourceName}`);
      }
      
      return transformResponse(data[resourceName.toLowerCase()] || data);
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : `Unknown error updating ${resourceName}`;
      setError(errorMessage);
      console.error(`Error updating ${resourceName}:`, err);
      throw err;
    } finally {
      setIsLoading(false);
    }
  }, [endpoints.update, resourceName, transformResponse]);
  
  /**
   * Delete a resource by ID
   */
  const deleteResource = useCallback(async (id: IdType): Promise<boolean> => {
    try {
      setIsLoading(true);
      setError(null);
      
      const response = await apiClient.post(endpoints.delete, { [`${resourceName.toLowerCase()}Id`]: id });
      const data = response.data;
      
      if (!data.success) {
        throw new Error(data.error || `Failed to delete ${resourceName}`);
      }
      
      return true;
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : `Unknown error deleting ${resourceName}`;
      setError(errorMessage);
      console.error(`Error deleting ${resourceName}:`, err);
      throw err;
    } finally {
      setIsLoading(false);
    }
  }, [endpoints.delete, resourceName]);
  
  /**
   * List all resources
   */
  const list = useCallback(async (params: any = {}): Promise<T[]> => {
    try {
      setIsLoading(true);
      setError(null);
      
      const response = await apiClient.post(endpoints.list, params);
      const data = response.data;
      
      if (!data.success) {
        throw new Error(data.error || `Failed to list ${resourceName}s`);
      }
      
      return transformResponse(data[`${resourceName.toLowerCase()}s`] || []);
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : `Unknown error listing ${resourceName}s`;
      setError(errorMessage);
      console.error(`Error listing ${resourceName}s:`, err);
      return [];
    } finally {
      setIsLoading(false);
    }
  }, [endpoints.list, resourceName, transformResponse]);
  
  return {
    create,
    read,
    update,
    delete: deleteResource,
    list,
    isLoading,
    error,
    clearError: () => setError(null)
  };
}