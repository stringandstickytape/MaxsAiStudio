
import { useCallback } from 'react';
import { useApiCallState, createApiRequest } from '@/utils/apiUtils';
import { useToolStore } from '@/stores/useToolStore';
import { Tool, ToolCategory } from '@/types/toolTypes';
import { createResourceHook } from './useResourceFactory';


const useToolResource = createResourceHook<Tool>({
  endpoints: {
    fetch: '/api/getTools',
    create: '/api/addTool',
    update: '/api/updateTool',
    delete: '/api/deleteTool',
  },
  storeActions: {
    setItems: (tools) => useToolStore.getState().setTools(tools),
  },
  options: {
    idField: 'guid',
    generateId: true,
    transformFetchResponse: (data) => {
      
      return (data.tools || []).map((tool: Tool) => ({
        ...tool,
        filetype: tool.filetype || '',
      }));
    },
    transformItemResponse: (data) => data.tool,
  },
});


const useToolCategoryResource = createResourceHook<ToolCategory>({
  endpoints: {
    fetch: '/api/getToolCategories',
  },
  storeActions: {
    setItems: (categories) => useToolStore.getState().setCategories(categories),
  },
  options: {
    idField: 'id',
    transformFetchResponse: (data) => data.categories || [],
  },
});

export function useToolsManagement() {
  
  const {
    isLoading: toolsLoading,
    error: toolsError,
    fetchItems: fetchTools,
    createItem: addTool,
    updateItem: updateTool,
    deleteItem: deleteTool,
    clearError: clearToolsError,
  } = useToolResource();

  
  const {
    isLoading: categoriesLoading,
    error: categoriesError,
    fetchItems: fetchToolCategories,
    clearError: clearCategoriesError,
  } = useToolCategoryResource();

  
  const { executeApiCall } = useApiCallState();

  
  const { tools, categories, activeTools, addActiveTool, removeActiveTool, clearActiveTools } = useToolStore();

  
  const validateToolSchema = useCallback(
    async (schema: string) => {
      return executeApiCall(async () => {
        const validateSchemaRequest = createApiRequest('/api/validateToolSchema', 'POST');
        const data = await validateSchemaRequest({ schema });

        return data.isValid;
      });
    },
    [executeApiCall],
  );

  
  const exportTools = useCallback(
    async (toolIds?: string[]) => {
      return executeApiCall(async () => {
        const exportToolsRequest = createApiRequest('/api/exportTools', 'POST');
        const data = await exportToolsRequest({ toolIds });

        return data.json;
      });
    },
    [executeApiCall],
  );

  
  const toggleTool = useCallback(
    (toolId: string, activate: boolean) => {
      if (activate) {
        addActiveTool(toolId);
      } else {
        removeActiveTool(toolId);
      }
    },
    [addActiveTool, removeActiveTool],
  );

  
  const isLoading = toolsLoading || categoriesLoading;

  
  const error = toolsError || categoriesError;

  
  const clearError = useCallback(() => {
    clearToolsError();
    clearCategoriesError();
  }, [clearToolsError, clearCategoriesError]);

  return {
    
    tools,
    categories,
    activeTools,
    isLoading,
    error,

    
    fetchTools,
    fetchToolCategories,
    addTool,
    updateTool,
    deleteTool,
    validateToolSchema,
    exportTools,
    toggleTool,
    addActiveTool,
    removeActiveTool,
    clearActiveTools,
    clearError,
  };
}

