// AiStudio4.Web\src\components\ConvTreeView\types.ts
import { Message } from '@/types/conv';

export interface TreeViewProps {
  convId: string;
  messages: Message[];
}

export interface TreeNode {
  id: string;
  content: string;
  source: string;
  children: TreeNode[];
  parentId?: string;
  depth?: number;
  x?: number;
  y?: number;
  timestamp?: number;
  durationMs?: number;
  costInfo?: {
    modelGuid?: string;
    totalCost?: number;
    tokenUsage?: {
      inputTokens: number;
      outputTokens: number;
    };
  } | null;
}