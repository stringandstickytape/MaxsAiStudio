// src/hooks/useToolsManagement.ts
import { useCallback } from 'react';
import { useApiCallState, createApiRequest } from '@/utils/apiUtils';
import { useToolStore } from '@/stores/useToolStore';
import { Tool, ToolCategory } from '@/types/toolTypes';
import { createResourceHook } from './useResourceFactory';

// Create resource hook for tools
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
      // Ensure all tools have a filetype property
      return (data.tools || []).map((tool: Tool) => ({
        ...tool,
        filetype: tool.filetype || '',
      }));
    },
    transformItemResponse: (data) => data.tool,
  },
});

// Create resource hook for tool categories
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
  // Use the tools resource hook
  const {
    isLoading: toolsLoading,
    error: toolsError,
    fetchItems: fetchTools,
    createItem: addTool,
    updateItem: updateTool,
    deleteItem: deleteTool,
    clearError: clearToolsError,
  } = useToolResource();

  // Use the tool categories resource hook
  const {
    isLoading: categoriesLoading,
    error: categoriesError,
    fetchItems: fetchToolCategories,
    clearError: clearCategoriesError,
  } = useToolCategoryResource();

  // Use API call state utility for specialized operations
  const { executeApiCall } = useApiCallState();

  // Get state from the store
  const { tools, categories, activeTools, addActiveTool, removeActiveTool, clearActiveTools } = useToolStore();

  // Validate a tool schema
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

  // Export tools
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

  // Toggle a tool's active state
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

  // Combined loading state
  const isLoading = toolsLoading || categoriesLoading;

  // Combined error state
  const error = toolsError || categoriesError;

  // Function to clear all errors
  const clearError = useCallback(() => {
    clearToolsError();
    clearCategoriesError();
  }, [clearToolsError, clearCategoriesError]);

  return {
    // State
    tools,
    categories,
    activeTools,
    isLoading,
    error,

    // Actions
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
