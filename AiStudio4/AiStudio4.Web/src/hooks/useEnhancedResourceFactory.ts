// src/hooks/useEnhancedResourceFactory.ts
import { useCallback, useState } from 'react';
import { v4 as uuidv4 } from 'uuid';
import { useResourceInitialization, HookLifecycleStage, createHookState } from '@/utils/hookLifecycle';
import { createApiRequest } from '@/utils/apiUtils';

/**
 * Resource operation types
 */
export enum ResourceOperation {
  Fetch = 'fetch',
  Create = 'create',
  Update = 'update',
  Delete = 'delete'
}

/**
 * Configuration options for resource hook factory
 */
export interface EnhancedResourceFactoryOptions<T, CreateParams, UpdateParams> {
  // Resource name (for logging and debugging)
  resourceName: string;
  
  // API endpoints for CRUD operations
  endpoints: {
    [ResourceOperation.Fetch]?: string;
    [ResourceOperation.Create]?: string;
    [ResourceOperation.Update]?: string;
    [ResourceOperation.Delete]?: string;
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
    // Operation-specific request transformers
    requestTransformers?: {
      [key in ResourceOperation]?: (params: any) => any;
    };
  };
}

/**
 * Factory function to create an enhanced resource management hook with lifecycle tracking
 */
export function createEnhancedResourceHook<
  T extends { [key: string]: any },
  CreateParams = Omit<T, 'id' | 'guid'>,
  UpdateParams = T
>(config: EnhancedResourceFactoryOptions<T, CreateParams, UpdateParams>) {
  // Return the custom hook
  return function useEnhancedResource(initialItems: T[] = []) {
    // ID field name (defaults to 'guid')
    const idField = config.options?.idField || 'guid';
    
    // Local state for items if not using store
    const [items, setItems] = useState<T[]>(initialItems);
    
    // Operation state tracking
    const [operationStates, setOperationStates] = useState<{
      [key in ResourceOperation]?: {
        isLoading: boolean;
        error: any;
      };
    }>({});
    
    // Track overall loading state
    const isLoading = Object.values(operationStates).some(state => state?.isLoading);
    
    // Track overall error state
    const error = Object.values(operationStates)
      .map(state => state?.error)
      .find(err => err != null) || null;
    
    // Update operation state
    const updateOperationState = (operation: ResourceOperation, isLoading: boolean, error: any = null) => {
      setOperationStates(prev => ({
        ...prev,
        [operation]: { isLoading, error }
      }));
    };
    
    // Execute API request with operation state tracking
    const executeOperation = async <R>(operation: ResourceOperation, apiCall: () => Promise<R>): Promise<R | null> => {
      updateOperationState(operation, true);
      
      try {
        const result = await apiCall();
        updateOperationState(operation, false);
        return result;
      } catch (err) {
        const errorMessage = err instanceof Error ? err.message : 'Unknown error';
        console.error(`${config.resourceName} ${operation} error:`, err);
        updateOperationState(operation, false, errorMessage);
        return null;
      }
    };
    
    // Fetch all items
    const fetchItems = useCallback(async () => {
      if (!config.endpoints[ResourceOperation.Fetch]) return null;
      
      return executeOperation(ResourceOperation.Fetch, async () => {
        const fetchRequest = createApiRequest(config.endpoints[ResourceOperation.Fetch]!, 'POST');
        const data = await fetchRequest(config.options?.requestTransformers?.[ResourceOperation.Fetch] ?  
          config.options.requestTransformers[ResourceOperation.Fetch]!({}) : 
          {});
        
        // Transform response if needed
        const items = config.options?.transformFetchResponse 
          ? config.options.transformFetchResponse(data)
          : data.items || data.results || [];
        
        // Update store if action provided, otherwise use local state
        config.storeActions.setItems(items);
        setItems(items);
        
        return items;
      });
    }, []);
    
    // Create a new item
    const createItem = useCallback(async (params: CreateParams) => {
      if (!config.endpoints[ResourceOperation.Create]) return null;
      
      return executeOperation(ResourceOperation.Create, async () => {
        // Generate ID if needed
        const shouldGenerateId = config.options?.generateId !== false;
        const baseItemData = shouldGenerateId 
          ? { ...params as object, [idField]: uuidv4() } 
          : params;
        
        // Apply request transformer if provided
        const itemData = config.options?.requestTransformers?.[ResourceOperation.Create] 
          ? config.options.requestTransformers[ResourceOperation.Create](baseItemData)
          : baseItemData;
        
        const createRequest = createApiRequest(config.endpoints[ResourceOperation.Create]!, 'POST');
        const data = await createRequest(itemData);
        
        // Transform response if needed
        const newItem = config.options?.transformItemResponse 
          ? config.options.transformItemResponse(data)
          : data.item || data;
        
        // Refresh items list
        await fetchItems();
        
        return newItem;
      });
    }, [fetchItems]);
    
    // Update an existing item
    const updateItem = useCallback(async (params: UpdateParams) => {
      if (!config.endpoints[ResourceOperation.Update]) return null;
      
      return executeOperation(ResourceOperation.Update, async () => {
        // Apply request transformer if provided
        const itemData = config.options?.requestTransformers?.[ResourceOperation.Update] 
          ? config.options.requestTransformers[ResourceOperation.Update](params)
          : params;
        
        const updateRequest = createApiRequest(config.endpoints[ResourceOperation.Update]!, 'POST');
        const data = await updateRequest(itemData);
        
        // Transform response if needed
        const updatedItem = config.options?.transformItemResponse 
          ? config.options.transformItemResponse(data)
          : data.item || data;
        
        // Refresh items list
        await fetchItems();
        
        return updatedItem;
      });
    }, [fetchItems]);
    
    // Delete an item
    const deleteItem = useCallback(async (id: string) => {
      if (!config.endpoints[ResourceOperation.Delete]) return null;
      
      return executeOperation(ResourceOperation.Delete, async () => {
        // Prepare payload
        const basePayload = { [`${idField}`]: id };
        
        // Apply request transformer if provided
        const payload = config.options?.requestTransformers?.[ResourceOperation.Delete] 
          ? config.options.requestTransformers[ResourceOperation.Delete](basePayload)
          : basePayload;
        
        const deleteRequest = createApiRequest(config.endpoints[ResourceOperation.Delete]!, 'POST');
        await deleteRequest(payload);
        
        // Refresh items list
        await fetchItems();
        
        return true;
      });
    }, [fetchItems]);
    
    // Initialize data
    const { isInitialized, load: initialize } = useResourceInitialization<T[]>(
      createHookState<T[]>(initialItems),
      fetchItems
    );
    
    // Clear all errors
    const clearError = useCallback(() => {
      setOperationStates({});
    }, []);
    
    return {
      // State
      items,
      isLoading,
      error,
      isInitialized,
      operationStates,
      
      // Actions
      initialize,
      fetchItems,
      createItem,
      updateItem,
      deleteItem,
      clearError
    };
  };
}
