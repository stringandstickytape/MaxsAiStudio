// src/utils/modelUtils.ts
import { useModelStore } from '@/stores/useModelStore';

// Define a type for the modelGuid field
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
  
  // Access the model store to find the model with the matching GUID
  const models = useModelStore.getState().models;
  const model = models.find(m => m.guid === modelGuid);
  
  // Return the friendly name if found, otherwise return a formatted version of the GUID
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
