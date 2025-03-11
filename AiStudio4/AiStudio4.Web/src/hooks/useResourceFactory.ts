// src/hooks/useResourceFactory.ts
import { useCallback, useState } from 'react';
import { useInitializeIfEmpty } from '@/utils/hookUtils';
import { useApiCallState, createApiRequest } from '@/utils/apiUtils';
import { v4 as uuidv4 } from 'uuid';


interface ResourceFactoryOptions<T, CreateParams, UpdateParams> {
  
  endpoints: {
    fetch?: string;
    create?: string;
    update?: string;
    delete?: string;
  };

  
  storeActions: {
    setItems: (items: T[]) => void;
    addItem?: (item: T) => void;
    updateItem?: (item: T) => void;
    removeItem?: (id: string) => void;
  };

  
  options?: {
    idField?: string;
    generateId?: boolean;
    transformFetchResponse?: (data: any) => T[];
    transformItemResponse?: (data: any) => T;
    
    additionalInit?: () => Promise<void>;
  };
}


export function createResourceHook<
  T extends { [key: string]: any },
  CreateParams = Omit<T, 'id' | 'guid'>,
  UpdateParams = T,
>(config: ResourceFactoryOptions<T, CreateParams, UpdateParams>) {
  
  return function useResource(initialItems: T[] = []) {
    
    const { isLoading, error, executeApiCall, clearError } = useApiCallState();

    
    const [localItems, setLocalItems] = useState<T[]>(initialItems);

    
    const idField = config.options?.idField || 'guid';

    
    const isInitialized = useInitializeIfEmpty(async () => {
      await fetchItems();

      
      if (config.options?.additionalInit) {
        await config.options.additionalInit();
      }
    });

    
    const fetchItems = useCallback(async () => {
      if (!config.endpoints.fetch) return null;

      return executeApiCall(async () => {
        const fetchRequest = createApiRequest(config.endpoints.fetch!, 'POST');
        const data = await fetchRequest({});

        
        const items = config.options?.transformFetchResponse
          ? config.options.transformFetchResponse(data)
          : data.items || data.results || [];

        
        config.storeActions.setItems(items);

        return items;
      });
    }, []);

    
    const createItem = useCallback(async (params: CreateParams) => {
      if (!config.endpoints.create) return null;

      return executeApiCall(async () => {
        
        const shouldGenerateId = config.options?.generateId !== false;
        const itemWithId = shouldGenerateId ? { ...(params as object), [idField]: uuidv4() } : params;

        const createRequest = createApiRequest(config.endpoints.create!, 'POST');
        const data = await createRequest(itemWithId);

        
        const newItem = config.options?.transformItemResponse
          ? config.options.transformItemResponse(data)
          : data.item || data;

        
        await fetchItems();

        return newItem;
      });
    }, []);

    
    const updateItem = useCallback(async (params: UpdateParams) => {
      if (!config.endpoints.update) return null;

      return executeApiCall(async () => {
        const updateRequest = createApiRequest(config.endpoints.update!, 'POST');
        const data = await updateRequest(params);

        
        const updatedItem = config.options?.transformItemResponse
          ? config.options.transformItemResponse(data)
          : data.item || data;

        
        await fetchItems();

        return updatedItem;
      });
    }, []);

    
    const deleteItem = useCallback(async (id: string) => {
      if (!config.endpoints.delete) return null;

      return executeApiCall(async () => {
        const deleteRequest = createApiRequest(config.endpoints.delete!, 'POST');
        await deleteRequest({ [`${idField}`]: id });

        
        await fetchItems();

        return true;
      });
    }, []);

    return {
      
      items: localItems,
      isLoading,
      error,
      isInitialized,

      
      fetchItems,
      createItem,
      updateItem,
      deleteItem,
      clearError,
    };
  };
}

