
import { Attachment } from './attachment';

export interface ContentBlock {
  content: string;
  contentType: 'text';
}

export interface Message {
  id: string;  contentBlocks: ContentBlock[];
  /** @deprecated use contentBlocks */
  content?: string;
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
  cumulativeCost?: number | null;
  durationMs?: number | null; // Ensure it can be null or number
  temperature?: number; // Temperature used for AI messages (optional) 
}

export interface Conv {
  id: string;
  messages: Message[];
}