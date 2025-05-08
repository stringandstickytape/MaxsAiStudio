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
  // Enable this flag to see detailed logging
    const enableLogging = false;
  
  if (!modelGuid) {
    if (enableLogging) console.log('🔍 getModelFriendlyName: No modelGuid provided');
    return 'Unknown Model';
  }
  
  // Check cache first without logging
  if (modelNameCache.has(modelGuid)) {
    const cachedName = modelNameCache.get(modelGuid)!;
    if (enableLogging) console.log(`🔍 getModelFriendlyName: Found in cache: ${cachedName}`);
    return cachedName;
  }
  
  // Only log when we need to do the expensive lookup
  if (enableLogging) {
    console.log(`🔍 getModelFriendlyName: Looking up model with GUID: ${modelGuid}`);
    console.log(`🔍 getModelFriendlyName: Cache miss, checking models store`);
  }
  
  const models = useModelStore.getState().models;
  if (enableLogging) console.log(`🔍 getModelFriendlyName: Models in store: ${models.length}`);
  
  // First try an exact GUID match
  let model = models.find(m => m.guid === modelGuid);
  
  // If no exact match, try to match by model name contained in the GUID
  // (in case the GUIDs were generated differently but contain the same model name)
  if (!model && modelGuid.includes('-')) {
    if (enableLogging) console.log('🔍 getModelFriendlyName: No exact match, trying to match by model name in GUID');
    const possibleModelName = modelGuid.split('-')[0];
    if (possibleModelName.length > 3) { // Only try matching if we have enough characters
      model = models.find(m => 
        m.modelName.toLowerCase().includes(possibleModelName.toLowerCase()) ||
        possibleModelName.toLowerCase().includes(m.modelName.toLowerCase())
      );
      if (enableLogging) console.log(`🔍 getModelFriendlyName: Tried matching with \"${possibleModelName}\", result:`, model ? 'Match found' : 'No match');
    }
  }
  
  if (enableLogging) console.log(`🔍 getModelFriendlyName: Found model?`, model ? 'Yes' : 'No');
  
  const name = model?.friendlyName || `Model ${modelGuid.substring(0, 8)}`;
  if (enableLogging) console.log(`🔍 getModelFriendlyName: Resolved name: ${name}`);
  
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