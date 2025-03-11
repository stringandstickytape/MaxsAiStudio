// src/hooks/useToolsManagement.ts
import { useCallback } from 'react';
import { useInitialization } from '@/utils/hookUtils';
import { useApiCallState, createApiRequest } from '@/utils/apiUtils';
import { useToolStore } from '@/stores/useToolStore';
import { Tool, ToolCategory } from '@/types/toolTypes';
import { v4 as uuidv4 } from 'uuid';
import { apiClient } from '@/services/api/apiClient';

export function useToolsManagement() {
  // Use API call state utility
  const { 
    isLoading, 
    error, 
    executeApiCall, 
    clearError 
  } = useApiCallState();
  
  // Perform initialization with safer approach
  const isInitialized = useInitialization(async () => {
    // Fetch data directly without checking length
    await fetchTools();
    await fetchToolCategories();
  }, []);
  
  const { 
    tools,
    categories,
    activeTools,
    setTools, 
    setCategories, 
    addActiveTool,
    removeActiveTool,
    clearActiveTools 
  } = useToolStore();
  
  // Fetch all tools
  const fetchTools = useCallback(async () => {
    return executeApiCall(async () => {
      const getTools = createApiRequest('/api/getTools', 'POST');
      const data = await getTools({});
      
      // Ensure all tools have a filetype property
      const toolsWithFiletype = data.tools.map((tool: Tool) => ({
        ...tool,
        filetype: tool.filetype || ''
      }));
      
      setTools(toolsWithFiletype);
      return toolsWithFiletype;
    });
  }, [setTools]);
  
  // Fetch tool categories
  const fetchToolCategories = useCallback(async () => {
    return executeApiCall(async () => {
      const getToolCategories = createApiRequest('/api/getToolCategories', 'POST');
      const data = await getToolCategories({});
      
      setCategories(data.categories);
      return data.categories;
    });
  }, [setCategories]);
  
  // Add a tool
  const addTool = useCallback(async (toolData: Omit<Tool, 'guid'>) => {
    return executeApiCall(async () => {
      // Generate a new GUID if not provided
      const toolWithGuid = {
        ...toolData,
        guid: uuidv4()
      };
      
      const addToolRequest = createApiRequest('/api/addTool', 'POST');
      const data = await addToolRequest(toolWithGuid);
      
      // Refresh tools list
      await fetchTools();
      
      return data.tool;
    });
  }, [fetchTools]);
  
  // Update a tool
  const updateTool = useCallback(async (toolData: Tool) => {
    return executeApiCall(async () => {
      const updateToolRequest = createApiRequest('/api/updateTool', 'POST');
      const data = await updateToolRequest(toolData);
      
      // Refresh tools list
      await fetchTools();
      
      return data.tool;
    });
  }, [fetchTools]);
  
  // Delete a tool
  const deleteTool = useCallback(async (toolId: string) => {
    return executeApiCall(async () => {
      const deleteToolRequest = createApiRequest('/api/deleteTool', 'POST');
      await deleteToolRequest({ toolId });
      
      // If this tool was active, remove it
      if (activeTools.includes(toolId)) {
        removeActiveTool(toolId);
      }
      
      // Refresh tools list
      await fetchTools();
      
      return true;
    });
  }, [fetchTools, activeTools, removeActiveTool]);
  
  // Validate a tool schema
  const validateToolSchema = useCallback(async (schema: string) => {
    return executeApiCall(async () => {
      const validateSchemaRequest = createApiRequest('/api/validateToolSchema', 'POST');
      const data = await validateSchemaRequest({ schema });
      
      return data.isValid;
    });
  }, []);
  
  // Import tools
  const importTools = useCallback(async (jsonData: string) => {
    return executeApiCall(async () => {
      const importToolsRequest = createApiRequest('/api/importTools', 'POST');
      const data = await importToolsRequest({ json: jsonData });
      
      // Refresh tools list
      await fetchTools();
      
      return data.tools || [];
    });
  }, [fetchTools]);
  
  // Export tools
  const exportTools = useCallback(async (toolIds?: string[]) => {
    return executeApiCall(async () => {
      const exportToolsRequest = createApiRequest('/api/exportTools', 'POST');
      const data = await exportToolsRequest({ toolIds });
      
      return data.json;
    });
  }, []);
  
  // Toggle a tool's active state
  const toggleTool = useCallback((toolId: string, activate: boolean) => {
    if (activate) {
      addActiveTool(toolId);
    } else {
      removeActiveTool(toolId);
    }
  }, [addActiveTool, removeActiveTool]);
  
  // Initialization is now handled by useInitialization hook
  
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
    importTools,
    exportTools,
    toggleTool,
    addActiveTool,
    removeActiveTool,
    clearActiveTools,
    clearError
  };
}