// src/hooks/useMessageGraph.ts
import { useMemo } from 'react';
import { Message } from '@/types/conv';
import { MessageGraph } from '@/utils/messageGraph';

export function useMessageGraph(messages: Message[]) {
  const graph = useMemo(() => new MessageGraph(messages), [messages]);

  return graph;
}

