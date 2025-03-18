
import { Attachment } from './attachment';

export interface Message {
  id: string;
  content: string;
  source: 'user' | 'ai' | 'system';
  timestamp: number;
  parentId?: string | null;
  attachments?: Attachment[];
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
  timestamp?: number;
  durationMs?: number;
}

export interface Conv {
  id: string;
  messages: Message[];
}