// src/services/api/tools.ts
import { createApi, fetchBaseQuery } from '@reduxjs/toolkit/query/react';
import { Tool, ToolCategory } from '@/types/toolTypes';
import { apiClient } from './apiClient';

// API slice for tool-related endpoints
export const toolsApi = createApi({
  reducerPath: 'toolsApi',
  baseQuery: fetchBaseQuery({ baseUrl: '/' }),
  tagTypes: ['Tools', 'ToolCategories'],
  endpoints: (builder) => ({
    // Get all tools
    getTools: builder.query<Tool[], void>({
      queryFn: async () => {
        try {
          const response = await apiClient.post('/api/getTools', {});
          if (!response.data.success) {
            throw new Error(response.data.error || 'Failed to fetch tools');
          }
          const tools = response.data.tools.map((tool: any) => ({
            ...tool,
            filetype: tool.filetype || ''
          }));
          return { data: tools };
        } catch (error) {
          return { error: { status: 'CUSTOM_ERROR', data: error } };
        }
      },
      providesTags: ['Tools']
    }),
    
    // Get tool categories
    getToolCategories: builder.query<ToolCategory[], void>({
      queryFn: async () => {
        try {
          const response = await apiClient.post('/api/getToolCategories', {});
          if (!response.data.success) {
            throw new Error(response.data.error || 'Failed to fetch tool categories');
          }
          return { data: response.data.categories };
        } catch (error) {
          return { error: { status: 'CUSTOM_ERROR', data: error } };
        }
      },
      providesTags: ['ToolCategories']
    }),
    
    // Add a tool
    addTool: builder.mutation<Tool, any>({
      queryFn: async (toolData) => {
        try {
          const response = await apiClient.post('/api/addTool', toolData);
          if (!response.data.success) {
            throw new Error(response.data.error || 'Failed to add tool');
          }
          return { data: response.data.tool };
        } catch (error) {
          return { error: { status: 'CUSTOM_ERROR', data: error } };
        }
      },
      invalidatesTags: ['Tools']
    }),
    
    // Update a tool
    updateTool: builder.mutation<Tool, any>({
      queryFn: async (toolData) => {
        try {
          const response = await apiClient.post('/api/updateTool', toolData);
          if (!response.data.success) {
            throw new Error(response.data.error || 'Failed to update tool');
          }
          return { data: response.data.tool };
        } catch (error) {
          return { error: { status: 'CUSTOM_ERROR', data: error } };
        }
      },
      invalidatesTags: ['Tools']
    }),
    
    // Delete a tool
    deleteTool: builder.mutation<{ success: boolean }, string>({
      queryFn: async (toolId) => {
        try {
          const response = await apiClient.post('/api/deleteTool', { toolId });
          if (!response.data.success) {
            throw new Error(response.data.error || 'Failed to delete tool');
          }
          return { data: { success: true } };
        } catch (error) {
          return { error: { status: 'CUSTOM_ERROR', data: error } };
        }
      },
      invalidatesTags: ['Tools']
    }),
    
    // Import tools
    importTools: builder.mutation<{ success: boolean }, string>({
      queryFn: async (jsonData) => {
        try {
          const response = await apiClient.post('/api/importTools', { jsonData });
          if (!response.data.success) {
            throw new Error(response.data.error || 'Failed to import tools');
          }
          return { data: { success: true } };
        } catch (error) {
          return { error: { status: 'CUSTOM_ERROR', data: error } };
        }
      },
      invalidatesTags: ['Tools']
    }),
    
    // Export tools
    exportTools: builder.mutation<string, void>({
      queryFn: async () => {
        try {
          const response = await apiClient.post('/api/exportTools', {});
          if (!response.data.success) {
            throw new Error(response.data.error || 'Failed to export tools');
          }
          return { data: response.data.json };
        } catch (error) {
          return { error: { status: 'CUSTOM_ERROR', data: error } };
        }
      }
    }),
  }),
});

// Export hooks
export const {
  useGetToolsQuery,
  useGetToolCategoriesQuery,
  useAddToolMutation,
  useUpdateToolMutation,
  useDeleteToolMutation,
  useImportToolsMutation,
  useExportToolsMutation,
} = toolsApi;
