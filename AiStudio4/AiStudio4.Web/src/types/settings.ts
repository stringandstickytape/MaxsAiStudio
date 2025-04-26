export interface ServiceProvider {
  guid: string;
  url: string;
  apiKey: string;
  friendlyName: string;
  serviceName: string;
  iconName?: string;
}

export interface Model {
  guid: string;
  modelName: string;
  userNotes: string;
  providerGuid: string;
  additionalParams: string;
  input1MTokenPrice: number;
  output1MTokenPrice: number;
  color: string;
  starred: boolean;
  friendlyName: string;
  supportsPrefill: boolean;
}