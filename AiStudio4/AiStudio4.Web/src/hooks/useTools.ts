// src/hooks/useTools.ts
import { createApiHook } from './useApi';
import { Tool, ToolCategory } from '@/types/toolTypes';

// Get all tools
export const useGetTools = createApiHook<void, Tool[]>(
  '/api/getTools',
  'POST',
  {
    queryKey: 'tools',
    transformResponse: (response) => {
      if (!response.success) {
        throw new Error(response.error || 'Failed to fetch tools');
      }
      return response.tools.map(tool => ({
        ...tool,
        filetype: tool.filetype || ''
      }));
    }
  }
);

// Get tool categories
export const useGetToolCategories = createApiHook<void, ToolCategory[]>(
  '/api/getToolCategories',
  'POST',
  {
    queryKey: 'toolCategories',
    transformResponse: (response) => {
      if (!response.success) {
        throw new Error(response.error || 'Failed to fetch tool categories');
      }
      return response.categories;
    }
  }
);

// Add a tool
export const useAddTool = createApiHook<any, Tool>(
  '/api/addTool',
  'POST',
  {
    transformResponse: (response) => {
      if (!response.success) {
        throw new Error(response.error || 'Failed to add tool');
      }
      return response.tool;
    },
    // Invalidate the tools cache when a tool is added
    onSuccess: () => {
      const { setQueryData } = useApiStore.getState();
      setQueryData('tools', null); // Force refetch
    }
  }
);

// ... implement other tool-related API hooks