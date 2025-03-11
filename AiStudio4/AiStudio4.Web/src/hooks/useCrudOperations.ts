// src/hooks/useCrudOperations.ts
import { useState, useCallback } from 'react';
import { apiClient } from '@/services/api/apiClient';

/**
 * Creates CRUD operation functions for a resource
 * Note: This is NOT a hook, but a factory function
 */
export function createCrudOperations<T, CreateParams = Omit<T, 'guid'>, UpdateParams = T, IdType = string>(
  resourceName: string,
  options: {
    idField?: string;
    createEndpoint?: string;
    readEndpoint?: string;
    updateEndpoint?: string;
    deleteEndpoint?: string;
    listEndpoint?: string;
    transformResponse?: (data: any) => any;
  } = {},
) {
  // Default ID field is 'guid'
  const idField = options.idField || 'guid';

  // Default endpoints based on resourceName
  const endpoints = {
    create: options.createEndpoint || `/api/create${resourceName}`,
    read: options.readEndpoint || `/api/get${resourceName}`,
    update: options.updateEndpoint || `/api/update${resourceName}`,
    delete: options.deleteEndpoint || `/api/delete${resourceName}`,
    list: options.listEndpoint || `/api/get${resourceName}s`,
  };

  // Transform response data if provided
  const transformResponse = options.transformResponse || ((data) => data);

  return {
    /**
     * Create a new resource
     */
    create: async (params: CreateParams): Promise<T> => {
      try {
        const response = await apiClient.post(endpoints.create, params);
        const data = response.data;

        if (!data.success) {
          throw new Error(data.error || `Failed to create ${resourceName}`);
        }

        return transformResponse(data[resourceName.toLowerCase()] || data);
      } catch (err) {
        console.error(`Error creating ${resourceName}:`, err);
        throw err;
      }
    },

    /**
     * Read a specific resource by ID
     */
    read: async (id: IdType): Promise<T | null> => {
      try {
        const response = await apiClient.post(endpoints.read, { [`${resourceName.toLowerCase()}Id`]: id });
        const data = response.data;

        if (!data.success) {
          throw new Error(data.error || `Failed to get ${resourceName}`);
        }

        return transformResponse(data[resourceName.toLowerCase()] || data);
      } catch (err) {
        console.error(`Error reading ${resourceName}:`, err);
        return null;
      }
    },

    /**
     * Update an existing resource
     */
    update: async (params: UpdateParams): Promise<T> => {
      try {
        const response = await apiClient.post(endpoints.update, params);
        const data = response.data;

        if (!data.success) {
          throw new Error(data.error || `Failed to update ${resourceName}`);
        }

        return transformResponse(data[resourceName.toLowerCase()] || data);
      } catch (err) {
        console.error(`Error updating ${resourceName}:`, err);
        throw err;
      }
    },

    /**
     * Delete a resource by ID
     */
    delete: async (id: IdType): Promise<boolean> => {
      try {
        const response = await apiClient.post(endpoints.delete, { [`${resourceName.toLowerCase()}Id`]: id });
        const data = response.data;

        if (!data.success) {
          throw new Error(data.error || `Failed to delete ${resourceName}`);
        }

        return true;
      } catch (err) {
        console.error(`Error deleting ${resourceName}:`, err);
        throw err;
      }
    },

    /**
     * List all resources
     */
    list: async (params: any = {}): Promise<T[]> => {
      try {
        const response = await apiClient.post(endpoints.list, params);
        const data = response.data;

        if (!data.success) {
          throw new Error(data.error || `Failed to list ${resourceName}s`);
        }

        return transformResponse(data[`${resourceName.toLowerCase()}s`] || []);
      } catch (err) {
        console.error(`Error listing ${resourceName}s:`, err);
        return [];
      }
    },
  };
}

/**
 * Hook for CRUD operations with loading and error state
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
  } = {},
) {
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Create the CRUD operations
  const crudOperations = createCrudOperations<T, CreateParams, UpdateParams, IdType>(resourceName, options);

  // Wrap the operations with loading and error state
  const wrappedOperations = {
    create: useCallback(async (params: CreateParams): Promise<T> => {
      try {
        setIsLoading(true);
        setError(null);
        const result = await crudOperations.create(params);
        return result;
      } catch (err) {
        const errorMessage = err instanceof Error ? err.message : `Unknown error creating ${resourceName}`;
        setError(errorMessage);
        throw err;
      } finally {
        setIsLoading(false);
      }
    }, []),

    read: useCallback(async (id: IdType): Promise<T | null> => {
      try {
        setIsLoading(true);
        setError(null);
        return await crudOperations.read(id);
      } catch (err) {
        const errorMessage = err instanceof Error ? err.message : `Unknown error reading ${resourceName}`;
        setError(errorMessage);
        return null;
      } finally {
        setIsLoading(false);
      }
    }, []),

    update: useCallback(async (params: UpdateParams): Promise<T> => {
      try {
        setIsLoading(true);
        setError(null);
        return await crudOperations.update(params);
      } catch (err) {
        const errorMessage = err instanceof Error ? err.message : `Unknown error updating ${resourceName}`;
        setError(errorMessage);
        throw err;
      } finally {
        setIsLoading(false);
      }
    }, []),

    delete: useCallback(async (id: IdType): Promise<boolean> => {
      try {
        setIsLoading(true);
        setError(null);
        return await crudOperations.delete(id);
      } catch (err) {
        const errorMessage = err instanceof Error ? err.message : `Unknown error deleting ${resourceName}`;
        setError(errorMessage);
        throw err;
      } finally {
        setIsLoading(false);
      }
    }, []),

    list: useCallback(async (params: any = {}): Promise<T[]> => {
      try {
        setIsLoading(true);
        setError(null);
        return await crudOperations.list(params);
      } catch (err) {
        const errorMessage = err instanceof Error ? err.message : `Unknown error listing ${resourceName}s`;
        setError(errorMessage);
        return [];
      } finally {
        setIsLoading(false);
      }
    }, []),
  };

  return {
    ...wrappedOperations,
    isLoading,
    error,
    clearError: () => setError(null),
  };
}
