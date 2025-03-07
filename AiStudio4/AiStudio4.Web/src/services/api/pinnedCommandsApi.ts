// src/services/api/pinnedCommandsApi.ts
import { baseApi } from './baseApi';
import { PinnedCommand } from '@/stores/usePinnedCommandsStore';

export const pinnedCommandsApi = baseApi.injectEndpoints({
    endpoints: (builder) => ({
        getPinnedCommands: builder.query<PinnedCommand[], void>({
            query: () => ({
                url: '/api/pinnedCommands/get',
                method: 'POST',
                body: {},
            }),
            transformResponse: (response: { success: boolean; pinnedCommands: PinnedCommand[]; error?: string }) => {
                if (!response.success) {
                    throw new Error(response.error || 'Failed to fetch pinned commands');
                }
                return response.pinnedCommands || [];
            },
            providesTags: ['PinnedCommands'],
        }),

        savePinnedCommands: builder.mutation<boolean, PinnedCommand[]>({
            query: (pinnedCommands) => ({
                url: '/api/pinnedCommands/save',
                method: 'POST',
                body: { pinnedCommands },
            }),
            transformResponse: (response: { success: boolean; error?: string }) => {
                if (!response.success) {
                    throw new Error(response.error || 'Failed to save pinned commands');
                }
                return true;
            },
            invalidatesTags: ['PinnedCommands'],
        }),
    }),
});

export const {
    useGetPinnedCommandsQuery,
    useSavePinnedCommandsMutation,
} = pinnedCommandsApi;