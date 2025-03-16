import { useModelStore } from '@/stores/useModelStore';

/**
 * Model information interface
 */
export interface ModelInfo {
  modelGuid?: string;
  friendlyName?: string;
}

// Cache for model names to avoid repeated store lookups
let modelNameCache: Map<string, string> = new Map();

// Function to clear the cache when models change
export function clearModelNameCache() {
  modelNameCache.clear();
}

/**
 * Gets a friendly name for a model based on its GUID
 */
export function getModelFriendlyName(modelGuid?: string): string {
  if (!modelGuid) return 'Unknown Model';
  
  // Check cache first
  if (modelNameCache.has(modelGuid)) {
    return modelNameCache.get(modelGuid)!;
  }
  
  // Not in cache, look up from store
  const models = useModelStore.getState().models;
  const model = models.find(m => m.guid === modelGuid);
  
  // Store in cache and return
  const name = model?.friendlyName || `Model ${modelGuid.substring(0, 8)}`;
  modelNameCache.set(modelGuid, name);
  return name;
}

/**
 * Formats model's friendly name for display
 */
export function formatModelDisplay(modelGuid?: string): string {
  if (!modelGuid) return '';
  return `Model: ${getModelFriendlyName(modelGuid)}`;
}

// Subscribe to model store to clear cache when models change
if (typeof window !== 'undefined') {
  useModelStore.subscribe(
    state => state.models,
    () => clearModelNameCache()
  );
}