// src/services/api/toolsApi.ts
import { baseApi } from './baseApi';
import { Tool, ToolCategory } from '@/types/toolTypes';

type ToolWithoutGuid = Omit<Tool, 'guid'>;

export const toolsApi = baseApi.injectEndpoints({
  endpoints: (builder) => ({
    getTools: builder.query<Tool[], void>({
      query: () => ({
        url: '/api/getTools',
        method: 'POST',
        body: {},
      }),
      transformResponse: (response: { success: boolean; tools: Tool[]; error?: string }) => {
        if (!response.success) {
          throw new Error(response.error || 'Failed to fetch tools');
        }
        return response.tools;
      },
      providesTags: (result) =>
        result
          ? [
              ...result.map(({ guid }) => ({ type: 'Tools' as const, id: guid })),
              { type: 'Tools', id: 'LIST' },
            ]
          : [{ type: 'Tools', id: 'LIST' }],
    }),
    
    getTool: builder.query<Tool, string>({
      query: (toolId) => ({
        url: '/api/getTool',
        method: 'POST',
        body: { toolId },
      }),
      transformResponse: (response: { success: boolean; tool: Tool; error?: string }) => {
        if (!response.success) {
          throw new Error(response.error || 'Failed to fetch tool');
        }
        return response.tool;
      },
      providesTags: (result, error, id) => [{ type: 'Tools', id }],
    }),
    
    addTool: builder.mutation<Tool, ToolWithoutGuid>({
      query: (tool) => ({
        url: '/api/addTool',
        method: 'POST',
        body: tool,
      }),
      transformResponse: (response: { success: boolean; tool: Tool; error?: string }) => {
        if (!response.success) {
          throw new Error(response.error || 'Failed to add tool');
        }
        return response.tool;
      },
      invalidatesTags: [{ type: 'Tools', id: 'LIST' }],
    }),
    
    updateTool: builder.mutation<Tool, Tool>({
      query: (tool) => ({
        url: '/api/updateTool',
        method: 'POST',
        body: tool,
      }),
      transformResponse: (response: { success: boolean; tool: Tool; error?: string }) => {
        if (!response.success) {
          throw new Error(response.error || 'Failed to update tool');
        }
        return response.tool;
      },
      invalidatesTags: (result, error, arg) => [{ type: 'Tools', id: arg.guid }],
    }),
    
    deleteTool: builder.mutation<boolean, string>({
      query: (toolId) => ({
        url: '/api/deleteTool',
        method: 'POST',
        body: { toolId },
      }),
      transformResponse: (response: { success: boolean; error?: string }) => {
        if (!response.success) {
          throw new Error(response.error || 'Failed to delete tool');
        }
        return true;
      },
      invalidatesTags: (result, error, id) => [{ type: 'Tools', id }, { type: 'Tools', id: 'LIST' }],
    }),
    
    getToolCategories: builder.query<ToolCategory[], void>({
      query: () => ({
        url: '/api/getToolCategories',
        method: 'POST',
        body: {},
      }),
      transformResponse: (response: { success: boolean; categories: ToolCategory[]; error?: string }) => {
        if (!response.success) {
          throw new Error(response.error || 'Failed to fetch tool categories');
        }
        return response.categories;
      },
      providesTags: (result) =>
        result
          ? [
              ...result.map(({ id }) => ({ type: 'ToolCategories' as const, id })),
              { type: 'ToolCategories', id: 'LIST' },
            ]
          : [{ type: 'ToolCategories', id: 'LIST' }],
    }),
    
    addToolCategory: builder.mutation<ToolCategory, Omit<ToolCategory, 'id'>>({
      query: (category) => ({
        url: '/api/addToolCategory',
        method: 'POST',
        body: category,
      }),
      transformResponse: (response: { success: boolean; category: ToolCategory; error?: string }) => {
        if (!response.success) {
          throw new Error(response.error || 'Failed to add tool category');
        }
        return response.category;
      },
      invalidatesTags: [{ type: 'ToolCategories', id: 'LIST' }],
    }),
    
    updateToolCategory: builder.mutation<ToolCategory, ToolCategory>({
      query: (category) => ({
        url: '/api/updateToolCategory',
        method: 'POST',
        body: category,
      }),
      transformResponse: (response: { success: boolean; category: ToolCategory; error?: string }) => {
        if (!response.success) {
          throw new Error(response.error || 'Failed to update tool category');
        }
        return response.category;
      },
      invalidatesTags: (result, error, arg) => [{ type: 'ToolCategories', id: arg.id }],
    }),
    
    deleteToolCategory: builder.mutation<boolean, string>({
      query: (categoryId) => ({
        url: '/api/deleteToolCategory',
        method: 'POST',
        body: { categoryId },
      }),
      transformResponse: (response: { success: boolean; error?: string }) => {
        if (!response.success) {
          throw new Error(response.error || 'Failed to delete tool category');
        }
        return true;
      },
      invalidatesTags: (result, error, id) => [
        { type: 'ToolCategories', id },
        { type: 'ToolCategories', id: 'LIST' },
      ],
    }),
    
    validateToolSchema: builder.mutation<boolean, string>({
      query: (schema) => ({
        url: '/api/validateToolSchema',
        method: 'POST',
        body: { schema },
      }),
      transformResponse: (response: { success: boolean; isValid: boolean; error?: string }) => {
        if (!response.success) {
          throw new Error(response.error || 'Failed to validate tool schema');
        }
        return response.isValid;
      },
    }),
    
    importTools: builder.mutation<Tool[], string>({
      query: (json) => ({
        url: '/api/importTools',
        method: 'POST',
        body: { json },
      }),
      transformResponse: (response: { success: boolean; tools: Tool[]; error?: string }) => {
        if (!response.success) {
          throw new Error(response.error || 'Failed to import tools');
        }
        return response.tools;
      },
      invalidatesTags: [{ type: 'Tools', id: 'LIST' }],
    }),
    
    exportTools: builder.mutation<string, string[] | undefined>({
      query: (toolIds) => ({
        url: '/api/exportTools',
        method: 'POST',
        body: { toolIds },
      }),
      transformResponse: (response: { success: boolean; json: string; error?: string }) => {
        if (!response.success) {
          throw new Error(response.error || 'Failed to export tools');
        }
        return response.json;
      },
    }),
  }),
});

export const {
  useGetToolsQuery,
  useGetToolQuery,
  useAddToolMutation,
  useUpdateToolMutation,
  useDeleteToolMutation,
  useGetToolCategoriesQuery,
  useAddToolCategoryMutation,
  useUpdateToolCategoryMutation,
  useDeleteToolCategoryMutation,
  useValidateToolSchemaMutation,
  useImportToolsMutation,
  useExportToolsMutation,
} = toolsApi;
