import { useModelStore } from '@/stores/useModelStore';

export interface ModelInfo {
  modelGuid?: string;
  friendlyName?: string;
}


let modelNameCache: Map<string, string> = new Map();


export function clearModelNameCache() {
  modelNameCache.clear();
}

export function getModelFriendlyName(modelGuid?: string): string {
  if (!modelGuid) return 'Unknown Model';
  
  
  if (modelNameCache.has(modelGuid)) {
    return modelNameCache.get(modelGuid)!;
  }
  
  
  const models = useModelStore.getState().models;
  const model = models.find(m => m.guid === modelGuid);
  
  
  const name = model?.friendlyName || `Model ${modelGuid.substring(0, 8)}`;
  modelNameCache.set(modelGuid, name);
  return name;
}

export function formatModelDisplay(modelGuid?: string): string {
  if (!modelGuid) return '';
  return `Model: ${getModelFriendlyName(modelGuid)}`;
}


if (typeof window !== 'undefined') {
  useModelStore.subscribe(
    state => state.models,
    () => clearModelNameCache()
  );
}