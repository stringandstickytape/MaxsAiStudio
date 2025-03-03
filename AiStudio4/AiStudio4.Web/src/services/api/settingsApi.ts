// src/services/api/settingsApi.ts
import { baseApi } from './baseApi';
import { Model, ServiceProvider } from '@/types/settings';

interface ServiceProviderResponse {
  success: boolean;
  providers?: ServiceProvider[];
  error?: string;
}

interface ModelResponse {
  success: boolean;
  models?: Model[];
  error?: string;
}

type ModelCreateData = Omit<Model, 'guid'>;
type ServiceProviderCreateData = Omit<ServiceProvider, 'guid'>;

export const settingsApi = baseApi.injectEndpoints({
  endpoints: (builder) => ({
    getModels: builder.query<Model[], void>({
      query: () => ({
        url: '/api/getModels',
        method: 'POST',
        body: {},
      }),
      transformResponse: (response: ModelResponse) => {
        if (!response.success) {
          throw new Error(response.error || 'Failed to fetch models');
        }
        return response.models || [];
      },
      providesTags: (result) =>
        result
          ? [
              ...result.map(({ guid }) => ({ type: 'Models' as const, id: guid })),
              { type: 'Models', id: 'LIST' },
            ]
          : [{ type: 'Models', id: 'LIST' }],
    }),
    
    getServiceProviders: builder.query<ServiceProvider[], void>({
      query: () => ({
        url: '/api/getServiceProviders',
        method: 'POST',
        body: {},
      }),
      transformResponse: (response: ServiceProviderResponse) => {
        if (!response.success) {
          throw new Error(response.error || 'Failed to fetch service providers');
        }
        return response.providers || [];
      },
      providesTags: (result) =>
        result
          ? [
              ...result.map(({ guid }) => ({ type: 'ServiceProviders' as const, id: guid })),
              { type: 'ServiceProviders', id: 'LIST' },
            ]
          : [{ type: 'ServiceProviders', id: 'LIST' }],
    }),
    
    addModel: builder.mutation<void, ModelCreateData>({
      query: (model) => ({
        url: '/api/addModel',
        method: 'POST',
        body: model,
      }),
      transformResponse: (response: { success: boolean; error?: string }) => {
        if (!response.success) {
          throw new Error(response.error || 'Failed to add model');
        }
      },
      invalidatesTags: [{ type: 'Models', id: 'LIST' }],
    }),
    
    updateModel: builder.mutation<void, Model>({
      query: (model) => ({
        url: '/api/updateModel',
        method: 'POST',
        body: model,
      }),
      transformResponse: (response: { success: boolean; error?: string }) => {
        if (!response.success) {
          throw new Error(response.error || 'Failed to update model');
        }
      },
      invalidatesTags: (result, error, arg) => [{ type: 'Models', id: arg.guid }],
    }),
    
    deleteModel: builder.mutation<void, string>({
      query: (modelGuid) => ({
        url: '/api/deleteModel',
        method: 'POST',
        body: { modelGuid },
      }),
      transformResponse: (response: { success: boolean; error?: string }) => {
        if (!response.success) {
          throw new Error(response.error || 'Failed to delete model');
        }
      },
      invalidatesTags: (result, error, id) => [{ type: 'Models', id }, { type: 'Models', id: 'LIST' }],
    }),
    
    addServiceProvider: builder.mutation<void, ServiceProviderCreateData>({
      query: (provider) => ({
        url: '/api/addServiceProvider',
        method: 'POST',
        body: provider,
      }),
      transformResponse: (response: { success: boolean; error?: string }) => {
        if (!response.success) {
          throw new Error(response.error || 'Failed to add service provider');
        }
      },
      invalidatesTags: [{ type: 'ServiceProviders', id: 'LIST' }],
    }),
    
    updateServiceProvider: builder.mutation<void, ServiceProvider>({
      query: (provider) => ({
        url: '/api/updateServiceProvider',
        method: 'POST',
        body: provider,
      }),
      transformResponse: (response: { success: boolean; error?: string }) => {
        if (!response.success) {
          throw new Error(response.error || 'Failed to update service provider');
        }
      },
      invalidatesTags: (result, error, arg) => [{ type: 'ServiceProviders', id: arg.guid }],
    }),
    
    deleteServiceProvider: builder.mutation<void, string>({
      query: (providerGuid) => ({
        url: '/api/deleteServiceProvider',
        method: 'POST',
        body: { providerGuid },
      }),
      transformResponse: (response: { success: boolean; error?: string }) => {
        if (!response.success) {
          throw new Error(response.error || 'Failed to delete service provider');
        }
      },
      invalidatesTags: (result, error, id) => [
        { type: 'ServiceProviders', id },
        { type: 'ServiceProviders', id: 'LIST' },
      ],
    }),
  }),
});

export const {
  useGetModelsQuery,
  useGetServiceProvidersQuery,
  useAddModelMutation,
  useUpdateModelMutation,
  useDeleteModelMutation,
  useAddServiceProviderMutation,
  useUpdateServiceProviderMutation,
  useDeleteServiceProviderMutation,
} = settingsApi;
