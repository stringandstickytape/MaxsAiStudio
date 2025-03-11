// src/hooks/useResourceFactory.ts
import { useCallback, useState } from 'react';
import { useInitializeIfEmpty } from '@/utils/hookUtils';
import { useApiCallState, createApiRequest } from '@/utils/apiUtils';
import { v4 as uuidv4 } from 'uuid';

/**
 * Configuration options for resource hook factory
 */
interface ResourceFactoryOptions<T, CreateParams, UpdateParams> {
  // API endpoints for CRUD operations
  endpoints: {
    fetch?: string;
    create?: string;
    update?: string;
    delete?: string;
  };

  // Zustand store actions
  storeActions: {
    setItems: (items: T[]) => void;
    addItem?: (item: T) => void;
    updateItem?: (item: T) => void;
    removeItem?: (id: string) => void;
  };

  // Additional options
  options?: {
    idField?: string;
    generateId?: boolean;
    transformFetchResponse?: (data: any) => T[];
    transformItemResponse?: (data: any) => T;
    // Additional initialization logic
    additionalInit?: () => Promise<void>;
  };
}

/**
 * Factory function to create a resource management hook with standardized operations
 */
export function createResourceHook<
  T extends { [key: string]: any },
  CreateParams = Omit<T, 'id' | 'guid'>,
  UpdateParams = T,
>(config: ResourceFactoryOptions<T, CreateParams, UpdateParams>) {
  // Return the custom hook
  return function useResource(initialItems: T[] = []) {
    // Use API call state utility
    const { isLoading, error, executeApiCall, clearError } = useApiCallState();

    // Local state for items if not using store
    const [localItems, setLocalItems] = useState<T[]>(initialItems);

    // ID field name (defaults to 'guid')
    const idField = config.options?.idField || 'guid';

    // Initialize if needed
    const isInitialized = useInitializeIfEmpty(async () => {
      await fetchItems();

      // Run additional initialization if provided
      if (config.options?.additionalInit) {
        await config.options.additionalInit();
      }
    });

    // Fetch all items
    const fetchItems = useCallback(async () => {
      if (!config.endpoints.fetch) return null;

      return executeApiCall(async () => {
        const fetchRequest = createApiRequest(config.endpoints.fetch!, 'POST');
        const data = await fetchRequest({});

        // Transform response if needed
        const items = config.options?.transformFetchResponse
          ? config.options.transformFetchResponse(data)
          : data.items || data.results || [];

        // Update store if action provided, otherwise use local state
        config.storeActions.setItems(items);

        return items;
      });
    }, []);

    // Create a new item
    const createItem = useCallback(async (params: CreateParams) => {
      if (!config.endpoints.create) return null;

      return executeApiCall(async () => {
        // Generate ID if needed
        const shouldGenerateId = config.options?.generateId !== false;
        const itemWithId = shouldGenerateId ? { ...(params as object), [idField]: uuidv4() } : params;

        const createRequest = createApiRequest(config.endpoints.create!, 'POST');
        const data = await createRequest(itemWithId);

        // Transform response if needed
        const newItem = config.options?.transformItemResponse
          ? config.options.transformItemResponse(data)
          : data.item || data;

        // Refresh items list
        await fetchItems();

        return newItem;
      });
    }, []);

    // Update an existing item
    const updateItem = useCallback(async (params: UpdateParams) => {
      if (!config.endpoints.update) return null;

      return executeApiCall(async () => {
        const updateRequest = createApiRequest(config.endpoints.update!, 'POST');
        const data = await updateRequest(params);

        // Transform response if needed
        const updatedItem = config.options?.transformItemResponse
          ? config.options.transformItemResponse(data)
          : data.item || data;

        // Refresh items list
        await fetchItems();

        return updatedItem;
      });
    }, []);

    // Delete an item
    const deleteItem = useCallback(async (id: string) => {
      if (!config.endpoints.delete) return null;

      return executeApiCall(async () => {
        const deleteRequest = createApiRequest(config.endpoints.delete!, 'POST');
        await deleteRequest({ [`${idField}`]: id });

        // Refresh items list
        await fetchItems();

        return true;
      });
    }, []);

    return {
      // State
      items: localItems,
      isLoading,
      error,
      isInitialized,

      // Actions
      fetchItems,
      createItem,
      updateItem,
      deleteItem,
      clearError,
    };
  };
}
