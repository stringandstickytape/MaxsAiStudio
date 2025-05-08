// AiStudioClient\src\components\ConvTreeView\useMessageTree.ts
import { useMemo } from 'react';
import { Message } from '@/types/conv';
import { MessageGraph } from '@/utils/messageGraph';
import { TreeNode } from './types';

/**
 * Hook to convert messages to a hierarchical tree structure
 */
export const useMessageTree = (messages: Message[]) => {
  const hierarchicalData = useMemo(() => {
    if (!messages || messages.length === 0) return null;

    try {
      // Create a graph from the messages
      const graph = new MessageGraph(messages);

      // Get root messages
      const rootMessages = graph.getRootMessages();
      if (rootMessages.length === 0) return null;

      // Use the first root message
      const rootMessage = rootMessages[0];

      // Recursive function to build the tree
      const buildTree = (message: Message, depth: number = 0): TreeNode => {
        const node: TreeNode = {
          id: message.id,
          content: message.content,
          source: message.source,
          children: [],
          parentId: message.parentId,
          depth: depth,
          timestamp: message.timestamp,
          durationMs: message.durationMs,
          costInfo: message.costInfo,
        };

        // Add children recursively
        const childMessages = graph.getChildren(message.id);
        node.children = childMessages.map((child) => buildTree(child, depth + 1));

        return node;
      };

      // Build and return the tree
      return buildTree(rootMessage);
    } catch (error) {
      console.error('Error building message tree:', error);
      return null;
    }
  }, [messages]);

  return hierarchicalData;
};