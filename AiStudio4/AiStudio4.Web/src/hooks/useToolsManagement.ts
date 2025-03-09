// src/hooks/useToolsManagement.ts
import { useState, useCallback, useEffect, useRef } from 'react';
import { useToolStore } from '@/stores/useToolStore';
import { Tool, ToolCategory } from '@/types/toolTypes';
import { v4 as uuidv4 } from 'uuid';
import { apiClient } from '@/services/api/apiClient';

export function useToolsManagement() {
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  
  // Use ref to track initialization state
  const initialized = useRef(false);
  
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
    try {
      setIsLoading(true);
      setError(null);
      
      const response = await apiClient.post('/api/getTools', {});
      const data = response.data;
      
      if (!data.success) {
        throw new Error(data.error || 'Failed to fetch tools');
      }
      
      // Ensure all tools have a filetype property
      const toolsWithFiletype = data.tools.map((tool: Tool) => ({
        ...tool,
        filetype: tool.filetype || ''
      }));
      
      setTools(toolsWithFiletype);
      return toolsWithFiletype;
    } catch (err) {
      const errMsg = err instanceof Error ? err.message : 'Unknown error fetching tools';
      setError(errMsg);
      console.error('Error fetching tools:', err);
      return [];
    } finally {
      setIsLoading(false);
    }
  }, [setTools]);
  
  // Fetch tool categories
  const fetchToolCategories = useCallback(async () => {
    try {
      setIsLoading(true);
      setError(null);
      
      const response = await apiClient.post('/api/getToolCategories', {});
      const data = response.data;
      
      if (!data.success) {
        throw new Error(data.error || 'Failed to fetch tool categories');
      }
      
      setCategories(data.categories);
      return data.categories;
    } catch (err) {
      const errMsg = err instanceof Error ? err.message : 'Unknown error fetching tool categories';
      setError(errMsg);
      console.error('Error fetching tool categories:', err);
      return [];
    } finally {
      setIsLoading(false);
    }
  }, [setCategories]);
  
  // Add a tool
  const addTool = useCallback(async (toolData: Omit<Tool, 'guid'>) => {
    try {
      setIsLoading(true);
      setError(null);
      
      // Generate a new GUID if not provided
      const toolWithGuid = {
        ...toolData,
        guid: uuidv4()
      };
      
      const response = await apiClient.post('/api/addTool', toolWithGuid);
      const data = response.data;
      
      if (!data.success) {
        throw new Error(data.error || 'Failed to add tool');
      }
      
      // Refresh tools list
      await fetchTools();
      
      return data.tool;
    } catch (err) {
      const errMsg = err instanceof Error ? err.message : 'Unknown error adding tool';
      setError(errMsg);
      console.error('Error adding tool:', err);
      throw err;
    } finally {
      setIsLoading(false);
    }
  }, [fetchTools]);
  
  // Update a tool
  const updateTool = useCallback(async (toolData: Tool) => {
    try {
      setIsLoading(true);
      setError(null);
      
      const response = await apiClient.post('/api/updateTool', toolData);
      const data = response.data;
      
      if (!data.success) {
        throw new Error(data.error || 'Failed to update tool');
      }
      
      // Refresh tools list
      await fetchTools();
      
      return data.tool;
    } catch (err) {
      const errMsg = err instanceof Error ? err.message : 'Unknown error updating tool';
      setError(errMsg);
      console.error('Error updating tool:', err);
      throw err;
    } finally {
      setIsLoading(false);
    }
  }, [fetchTools]);
  
  // Delete a tool
  const deleteTool = useCallback(async (toolId: string) => {
    try {
      setIsLoading(true);
      setError(null);
      
      const response = await apiClient.post('/api/deleteTool', { toolId });
      const data = response.data;
      
      if (!data.success) {
        throw new Error(data.error || 'Failed to delete tool');
      }
      
      // If this tool was active, remove it
      if (activeTools.includes(toolId)) {
        removeActiveTool(toolId);
      }
      
      // Refresh tools list
      await fetchTools();
      
      return true;
    } catch (err) {
      const errMsg = err instanceof Error ? err.message : 'Unknown error deleting tool';
      setError(errMsg);
      console.error('Error deleting tool:', err);
      throw err;
    } finally {
      setIsLoading(false);
    }
  }, [fetchTools, activeTools, removeActiveTool]);
  
  // Validate a tool schema
  const validateToolSchema = useCallback(async (schema: string) => {
    try {
      setIsLoading(true);
      setError(null);
      
      const response = await apiClient.post('/api/validateToolSchema', { schema });
      const data = response.data;
      
      if (!data.success) {
        throw new Error(data.error || 'Failed to validate tool schema');
      }
      
      return data.isValid;
    } catch (err) {
      const errMsg = err instanceof Error ? err.message : 'Unknown error validating tool schema';
      setError(errMsg);
      console.error('Error validating tool schema:', err);
      return false;
    } finally {
      setIsLoading(false);
    }
  }, []);
  
  // Import tools
  const importTools = useCallback(async (jsonData: string) => {
    try {
      setIsLoading(true);
      setError(null);
      
      const response = await apiClient.post('/api/importTools', { json: jsonData });
      const data = response.data;
      
      if (!data.success) {
        throw new Error(data.error || 'Failed to import tools');
      }
      
      // Refresh tools list
      await fetchTools();
      
      return data.tools || [];
    } catch (err) {
      const errMsg = err instanceof Error ? err.message : 'Unknown error importing tools';
      setError(errMsg);
      console.error('Error importing tools:', err);
      throw err;
    } finally {
      setIsLoading(false);
    }
  }, [fetchTools]);
  
  // Export tools
  const exportTools = useCallback(async (toolIds?: string[]) => {
    try {
      setIsLoading(true);
      setError(null);
      
      const response = await apiClient.post('/api/exportTools', { toolIds });
      const data = response.data;
      
      if (!data.success) {
        throw new Error(data.error || 'Failed to export tools');
      }
      
      return data.json;
    } catch (err) {
      const errMsg = err instanceof Error ? err.message : 'Unknown error exporting tools';
      setError(errMsg);
      console.error('Error exporting tools:', err);
      throw err;
    } finally {
      setIsLoading(false);
    }
  }, []);
  
  // Toggle a tool's active state
  const toggleTool = useCallback((toolId: string, activate: boolean) => {
    if (activate) {
      addActiveTool(toolId);
    } else {
      removeActiveTool(toolId);
    }
  }, [addActiveTool, removeActiveTool]);
  
  // Initialize on first mount - use ref to prevent infinite loops
  useEffect(() => {
    // Only run this effect once
    if (!initialized.current) {
      const initialize = async () => {
        try {
          // Only fetch if data isn't already loaded
          if (tools.length === 0) {
            await fetchTools();
          }
          
          if (categories.length === 0) {
            await fetchToolCategories();
          }
          
          // Mark as initialized after successful fetching
          initialized.current = true;
        } catch (error) {
          console.error("Initialization error:", error);
        }
      };
      
      initialize();
    }
  }, [fetchTools, fetchToolCategories, tools.length, categories.length]);
  
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
    clearError: () => setError(null)
  };
}