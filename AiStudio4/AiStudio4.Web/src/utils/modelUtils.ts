
import { useModelStore } from '@/stores/useModelStore';


export interface ModelInfo {
  modelGuid?: string;
  friendlyName?: string;
}

/**
 * Gets a friendly name for a model based on its GUID
 * @param modelGuid The GUID of the model
 * @returns The friendly name of the model, or a default fallback if not found
 */
export function getModelFriendlyName(modelGuid?: string): string {
  if (!modelGuid) return 'Unknown Model';
  
  
  const models = useModelStore.getState().models;
  const model = models.find(m => m.guid === modelGuid);
  
  
  return model?.friendlyName || `Model ${modelGuid.substring(0, 8)}`;
}

/**
 * Utility function to format a model's friendly name for display
 * @param modelGuid The GUID of the model
 * @returns Formatted string with the model's friendly name
 */
export function formatModelDisplay(modelGuid?: string): string {
  if (!modelGuid) return '';
  
  const friendlyName = getModelFriendlyName(modelGuid);
  return `Model: ${friendlyName}`;
}
