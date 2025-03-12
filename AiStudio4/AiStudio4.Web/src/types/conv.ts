// src/types/conv.ts
export interface Message {
  id: string;
  content: string;
  source: 'user' | 'ai' | 'system';
  timestamp: number;
  parentId?: string | null;
  costInfo?: {
    inputCostPer1M: number;
    outputCostPer1M: number;
    totalCost: number;
    tokenUsage: {
      inputTokens: number;
      outputTokens: number;
      cacheCreationInputTokens: number;
      cacheReadInputTokens: number;
    };
    modelGuid?: string;
  } | null;
}

export interface Conv {
  id: string;
  messages: Message[];
}