// AiStudioClient/src/hooks/useAppInitializer.ts
import { useState, useEffect, useCallback } from 'react';
import { createApiRequest } from '@/utils/apiUtils';
import { useModelStore } from '@/stores/useModelStore';
import { useToolStore } from '@/stores/useToolStore';
import { useUserPromptStore } from '@/stores/useUserPromptStore';
import { usePinnedCommandsStore } from '@/stores/usePinnedCommandsStore';
import useProjectStore from '@/stores/useProjectStore';
import { useMcpServerStore } from '@/stores/useMcpServerStore';
import { useGeneralSettingsStore } from '@/stores/useGeneralSettingsStore';
import { useSystemPromptStore } from '@/stores/useSystemPromptStore';

export function useAppInitializer() {
  const [isInitialized, setIsInitialized] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const initialize = useCallback(async () => {
    if (isInitialized) return;

    try {
      console.log('[AppInitializer] Starting application initialization...');
      
      const getInitialDataRequest = createApiRequest('/api/getInitialData', 'POST');
      const response = await getInitialDataRequest({});

      if (!response.success) {
        throw new Error(response.error || 'Failed to fetch initial data');
      }

      const { data } = response;
      console.log('[AppInitializer] Received initial data:', data);

      // Hydrate all the stores with the fetched data
      useModelStore.getState().setModels(data.models || []);
      useModelStore.getState().setProviders(data.providers || []);
      useToolStore.getState().setTools(data.tools || []);
      useToolStore.getState().setCategories(data.toolCategories || []);
      useSystemPromptStore.getState().setPrompts(data.systemPrompts || []);
      useUserPromptStore.getState().setPrompts(data.userPrompts || []);
      usePinnedCommandsStore.getState().setPinnedCommands(data.pinnedCommands || []);
      useProjectStore.getState().setProjects(data.projects || []);
      useMcpServerStore.getState().setServers(data.mcpServers || []);

      // Hydrate config settings
      if (data.config) {
        if (data.config.defaultModelGuid) {
          useModelStore.getState().selectPrimaryModel(data.config.defaultModelGuid);
        }
        if (data.config.secondaryModelGuid) {
          useModelStore.getState().selectSecondaryModel(data.config.secondaryModelGuid);
        }
        if (typeof data.config.temperature === 'number') {
          useGeneralSettingsStore.getState().setTemperatureLocally(data.config.temperature);
        }
        if (typeof data.config.topP === 'number') {
          useGeneralSettingsStore.getState().setTopPLocally(data.config.topP);
        }
      }

      console.log('[AppInitializer] Successfully hydrated all stores');
      setIsInitialized(true);
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : 'Unknown initialization error';
      setError(errorMessage);
      console.error('[AppInitializer] Application initialization failed:', err);
    }
  }, [isInitialized]);

  useEffect(() => {
    initialize();
  }, [initialize]);

  return { isInitialized, error };
}