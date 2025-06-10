// AiStudio4/AiStudioClient/src/types/settings.ts

export type ChargingStrategyType = 'NoCaching' | 'Claude' | 'OpenAI' | 'Gemini';

export interface ServiceProvider {
  guid: string;
  url: string;
  apiKey: string;
  friendlyName: string;
  serviceName: string;
  iconName?: string;
  chargingStrategy?: ChargingStrategyType; // Added for charging strategy support
}

export interface Model {
  guid: string;
  modelName: string;
  userNotes: string;
  providerGuid: string;
  additionalParams: string;
  
  // Original properties (now represent below-boundary or default pricing)
  input1MTokenPrice: number;
  output1MTokenPrice: number;
  
  // New optional tiered pricing properties
  priceBoundary?: number | null;
  inputPriceAboveBoundary?: number | null;
  outputPriceAboveBoundary?: number | null;
  
  color: string;
  starred: boolean;
  friendlyName: string;
  supportsPrefill: boolean;
  requires1fTemp: boolean;
  allowsTopP: boolean;
  reasoningEffort: 'none' | 'low' | 'medium' | 'high';
  isTtsModel?: boolean;
  ttsVoiceName?: string;
}