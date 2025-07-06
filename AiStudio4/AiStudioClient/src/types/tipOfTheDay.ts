export interface TipOfTheDay {
  id: string;
  tip: string;
  samplePrompt: string;
  category?: string;
  createdAt?: string;
}

export interface TipOfTheDaySettings {
  showOnStartup: boolean;
  currentTipIndex: number;
  tips: TipOfTheDay[];
}

export interface TipOfTheDayResponse {
  success: boolean;
  data?: TipOfTheDaySettings;
  error?: string;
}