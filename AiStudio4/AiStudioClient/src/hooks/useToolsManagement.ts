import { useCallback } from 'react';
import { useApiCallState, createApiRequest } from '@/utils/apiUtils';
import { useToolStore } from '@/stores/useToolStore';
import { Tool, ToolCategory } from '@/types/toolTypes';
import { createResourceHook } from './useResourceFactory';


const useToolResource = createResourceHook<Tool>({
  endpoints: {
    create: '/api/addTool',
    update: '/api/updateTool',
    delete: '/api/deleteTool',
  },
  storeActions: {
    setItems: tools => useToolStore.getState().setTools(tools),
  },
  options: {
    idField: 'guid',
    generateId: true,
    transformFetchResponse: data => (data.tools || []).map((tool: Tool) => ({
      ...tool,
      filetype: tool.filetype || '',
    })),
    transformItemResponse: data => data.tool,
  },
});


const useToolCategoryResource = createResourceHook<ToolCategory>({
  endpoints: {},
  storeActions: {
    setItems: categories => useToolStore.getState().setCategories(categories),
  },
  options: {
    idField: 'id',
    transformFetchResponse: data => data.categories || [],
  },
});

export function useToolsManagement() {
  const {
    isLoading: toolsLoading,
    error: toolsError,
    createItem: addTool,
    updateItem: updateTool,
    deleteItem: deleteTool,
    clearError: clearToolsError,
  } = useToolResource();

  const {
    isLoading: categoriesLoading,
    error: categoriesError,
    clearError: clearCategoriesError,
  } = useToolCategoryResource();

  const { executeApiCall } = useApiCallState();
  const { tools, categories, activeTools, addActiveTool, removeActiveTool, clearActiveTools } = useToolStore();

  const validateToolSchema = useCallback(
    async (schema: string) => executeApiCall(async () => 
      (await createApiRequest('/api/validateToolSchema', 'POST')({ schema })).isValid
    ),
    [executeApiCall]
  );

  const exportTools = useCallback(
    async (toolIds?: string[]) => executeApiCall(async () => 
      (await createApiRequest('/api/exportTools', 'POST')({ toolIds })).json
    ),
    [executeApiCall]
  );

  const toggleTool = useCallback(
    (toolId: string, activate: boolean) => 
      activate ? addActiveTool(toolId) : removeActiveTool(toolId),
    [addActiveTool, removeActiveTool]
  );

  const isLoading = toolsLoading || categoriesLoading;
  const error = toolsError || categoriesError;
  const clearError = useCallback(
    () => { clearToolsError(); clearCategoriesError(); }, 
    [clearToolsError, clearCategoriesError]
  );

  return {
    tools, categories, activeTools, isLoading, error,
    addTool, updateTool, deleteTool,
    validateToolSchema, exportTools, toggleTool, addActiveTool,
    removeActiveTool, clearActiveTools, clearError,
  };
}