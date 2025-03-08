// src/services/api/models.ts
import { createApi, fetchBaseQuery } from '@reduxjs/toolkit/query/react';
import { Model, ServiceProvider } from '@/types/settings';
import { apiClient } from './apiClient';

// API slice for model-related endpoints
export const modelsApi = createApi({
  reducerPath: 'modelsApi',
  baseQuery: fetchBaseQuery({ baseUrl: '/' }),
  tagTypes: ['Models', 'Providers'],
  endpoints: (builder) => ({
    // Get all models
    getModels: builder.query<Model[], void>({
      queryFn: async () => {
        try {
          const response = await apiClient.post('/api/getModels', {});
          return { data: response.data.models || [] };
        } catch (error) {
          return { error: { status: 'CUSTOM_ERROR', data: error } };
        }
      },
      providesTags: ['Models']
    }),
    
    // Get all service providers
    getProviders: builder.query<ServiceProvider[], void>({
      queryFn: async () => {
        try {
          const response = await apiClient.post('/api/getServiceProviders', {});
          return { data: response.data.providers || [] };
        } catch (error) {
          return { error: { status: 'CUSTOM_ERROR', data: error } };
        }
      },
      providesTags: ['Providers']
    }),
    
    // Add a new model
    addModel: builder.mutation<Model, Omit<Model, 'guid'>>({ 
      queryFn: async (model) => {
        try {
          const response = await apiClient.post('/api/addModel', model);
          return { data: response.data.model };
        } catch (error) {
          return { error: { status: 'CUSTOM_ERROR', data: error } };
        }
      },
      invalidatesTags: ['Models']
    }),
    
    // Update an existing model
    updateModel: builder.mutation<Model, Model>({
      queryFn: async (model) => {
        try {
          const response = await apiClient.post('/api/updateModel', model);
          return { data: response.data.model };
        } catch (error) {
          return { error: { status: 'CUSTOM_ERROR', data: error } };
        }
      },
      invalidatesTags: ['Models']
    }),
    
    // Delete a model
    deleteModel: builder.mutation<{ success: boolean }, string>({
      queryFn: async (modelGuid) => {
        try {
          const response = await apiClient.post('/api/deleteModel', { modelGuid });
          return { data: { success: response.data.success } };
        } catch (error) {
          return { error: { status: 'CUSTOM_ERROR', data: error } };
        }
      },
      invalidatesTags: ['Models']
    }),
    
    // Add a new provider
    addProvider: builder.mutation<ServiceProvider, Omit<ServiceProvider, 'guid'>>({ 
      queryFn: async (provider) => {
        try {
          const response = await apiClient.post('/api/addServiceProvider', provider);
          return { data: response.data.provider };
        } catch (error) {
          return { error: { status: 'CUSTOM_ERROR', data: error } };
        }
      },
      invalidatesTags: ['Providers']
    }),
    
    // Update an existing provider
    updateProvider: builder.mutation<ServiceProvider, ServiceProvider>({
      queryFn: async (provider) => {
        try {
          const response = await apiClient.post('/api/updateServiceProvider', provider);
          return { data: response.data.provider };
        } catch (error) {
          return { error: { status: 'CUSTOM_ERROR', data: error } };
        }
      },
      invalidatesTags: ['Providers']
    }),
    
    // Delete a provider
    deleteProvider: builder.mutation<{ success: boolean }, string>({
      queryFn: async (providerGuid) => {
        try {
          const response = await apiClient.post('/api/deleteServiceProvider', { providerGuid });
          return { data: { success: response.data.success } };
        } catch (error) {
          return { error: { status: 'CUSTOM_ERROR', data: error } };
        }
      },
      invalidatesTags: ['Providers']
    }),
  }),
});

// Export hooks
export const {
  useGetModelsQuery,
  useGetProvidersQuery,
  useAddModelMutation,
  useUpdateModelMutation,
  useDeleteModelMutation,
  useAddProviderMutation,
  useUpdateProviderMutation,
  useDeleteProviderMutation,
} = modelsApi;
