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
  if (!modelGuid) {
    console.log('🔍 getModelFriendlyName: No modelGuid provided');
    return 'Unknown Model';
  }
  
  console.log(`🔍 getModelFriendlyName: Looking up model with GUID: ${modelGuid}`);
  
  if (modelNameCache.has(modelGuid)) {
    const cachedName = modelNameCache.get(modelGuid)!;
    console.log(`🔍 getModelFriendlyName: Found in cache: ${cachedName}`);
    return cachedName;
  }
  
  const models = useModelStore.getState().models;
  console.log(`🔍 getModelFriendlyName: Models in store: ${models.length}`);
  
  // First try an exact GUID match
  let model = models.find(m => m.guid === modelGuid);
  
  // If no exact match, try to match by model name contained in the GUID
  // (in case the GUIDs were generated differently but contain the same model name)
  if (!model && modelGuid.includes('-')) {
    console.log('🔍 getModelFriendlyName: No exact match, trying to match by model name in GUID');
    const possibleModelName = modelGuid.split('-')[0];
    if (possibleModelName.length > 3) { // Only try matching if we have enough characters
      model = models.find(m => 
        m.modelName.toLowerCase().includes(possibleModelName.toLowerCase()) ||
        possibleModelName.toLowerCase().includes(m.modelName.toLowerCase())
      );
      console.log(`🔍 getModelFriendlyName: Tried matching with \"${possibleModelName}\", result:`, model ? 'Match found' : 'No match');
    }
  }
  
  console.log(`🔍 getModelFriendlyName: Found model?`, model ? 'Yes' : 'No');
  
  const name = model?.friendlyName || `Model ${modelGuid.substring(0, 8)}`;
  console.log(`🔍 getModelFriendlyName: Resolved name: ${name}`);
  
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