// src/hooks/useToolsManagement.ts
import { useState, useCallback, useEffect } from 'react';
import { useToolStore } from '@/stores/useToolStore';
import { Tool, ToolCategory } from '@/types/toolTypes';
import { v4 as uuidv4 } from 'uuid';

export function useToolsManagement() {
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  
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
      const clientId = localStorage.getItem('clientId');
      const response = await fetch('/api/getTools', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'X-Client-Id': clientId || ''
        },
        body: JSON.stringify({})
      });
      
      const data = await response.json();
      
      if (!data.success) {
        throw new Error(data.error || 'Failed to fetch tools');
      }
      
      const toolsWithFiletype = data.tools.map((tool: any) => ({
        ...tool,
        filetype: tool.filetype || ''
      }));
      
      setTools(toolsWithFiletype);
      return toolsWithFiletype;
    } catch (err) {
      setError(`Failed to fetch tools: ${err instanceof Error ? err.message : 'Unknown error'}`);
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
      const clientId = localStorage.getItem('clientId');
      const response = await fetch('/api/getToolCategories', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'X-Client-Id': clientId || ''
        },
        body: JSON.stringify({})
      });
      
      const data = await response.json();
      
      if (!data.success) {
        throw new Error(data.error || 'Failed to fetch tool categories');
      }
      
      setCategories(data.categories);
      return data.categories;
    } catch (err) {
      setError(`Failed to fetch tool categories: ${err instanceof Error ? err.message : 'Unknown error'}`);
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
      const clientId = localStorage.getItem('clientId');
      
      // Generate a new GUID if not provided
      const toolWithGuid = {
        ...toolData,
        guid: uuidv4()
      };
      
      const response = await fetch('/api/addTool', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'X-Client-Id': clientId || ''
        },
        body: JSON.stringify(toolWithGuid)
      });
      
      const data = await response.json();
      
      if (!data.success) {
        throw new Error(data.error || 'Failed to add tool');
      }
      
      // Refresh tools list
      await fetchTools();
      
      return data.tool;
    } catch (err) {
      setError(`Failed to add tool: ${err instanceof Error ? err.message : 'Unknown error'}`);
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
      const clientId = localStorage.getItem('clientId');
      
      const response = await fetch('/api/updateTool', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'X-Client-Id': clientId || ''
        },
        body: JSON.stringify(toolData)
      });
      
      const data = await response.json();
      
      if (!data.success) {
        throw new Error(data.error || 'Failed to update tool');
      }
      
      // Refresh tools list
      await fetchTools();
      
      return data.tool;
    } catch (err) {
      setError(`Failed to update tool: ${err instanceof Error ? err.message : 'Unknown error'}`);
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
      const clientId = localStorage.getItem('clientId');
      
      const response = await fetch('/api/deleteTool', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'X-Client-Id': clientId || ''
        },
        body: JSON.stringify({ toolId })
      });
      
      const data = await response.json();
      
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
      setError(`Failed to delete tool: ${err instanceof Error ? err.message : 'Unknown error'}`);
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
      const clientId = localStorage.getItem('clientId');
      
      const response = await fetch('/api/validateToolSchema', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'X-Client-Id': clientId || ''
        },
        body: JSON.stringify({ schema })
      });
      
      const data = await response.json();
      
      if (!data.success) {
        throw new Error(data.error || 'Failed to validate tool schema');
      }
      
      return data.isValid;
    } catch (err) {
      setError(`Failed to validate tool schema: ${err instanceof Error ? err.message : 'Unknown error'}`);
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
      const clientId = localStorage.getItem('clientId');
      
      const response = await fetch('/api/importTools', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'X-Client-Id': clientId || ''
        },
        body: JSON.stringify({ json: jsonData })
      });
      
      const data = await response.json();
      
      if (!data.success) {
        throw new Error(data.error || 'Failed to import tools');
      }
      
      // Refresh tools list
      await fetchTools();
      
      return data.tools || [];
    } catch (err) {
      setError(`Failed to import tools: ${err instanceof Error ? err.message : 'Unknown error'}`);
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
      const clientId = localStorage.getItem('clientId');
      
      const response = await fetch('/api/exportTools', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'X-Client-Id': clientId || ''
        },
        body: JSON.stringify({ toolIds })
      });
      
      const data = await response.json();
      
      if (!data.success) {
        throw new Error(data.error || 'Failed to export tools');
      }
      
      return data.json;
    } catch (err) {
      setError(`Failed to export tools: ${err instanceof Error ? err.message : 'Unknown error'}`);
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
  
  // Initialize on first mount
  useEffect(() => {
    // Only fetch if data isn't already loaded
    if (tools.length === 0) {
      fetchTools();
    }
    
    if (categories.length === 0) {
      fetchToolCategories();
    }
  }, [tools.length, categories.length, fetchTools, fetchToolCategories]);
  
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