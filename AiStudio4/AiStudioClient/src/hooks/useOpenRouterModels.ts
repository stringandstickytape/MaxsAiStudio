import { useState, useEffect, useCallback } from 'react';
import { OpenRouterModel, fetchOpenRouterModels } from '@/services/api/openRouterApi';
import { useModelStore } from '@/stores/useModelStore';

export function useOpenRouterModels() {
  const [models, setModels] = useState<OpenRouterModel[]>([]);
  const [isLoading, setIsLoading] = useState<boolean>(false);
  const [error, setError] = useState<string | null>(null);
  
  const openRouterProvider = useModelStore(state => 
    state.providers.find(p => p.url.startsWith('https://openrouter.ai'))
  );

  const fetchModels = useCallback(async () => {
    if (!openRouterProvider?.apiKey) {
      setError('OpenRouter API key not found. Please configure a Service Provider for OpenRouter.');
      return;
    }
    
    setIsLoading(true);
    setError(null);
    
    try {
      const response = await fetchOpenRouterModels(openRouterProvider.apiKey);
      setModels(response.data);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'An unknown error occurred.');
    } finally {
      setIsLoading(false);
    }
  }, [openRouterProvider]);

  return { 
    models, 
    isLoading, 
    error, 
    fetchModels,
    hasProvider: !!openRouterProvider 
  };
}