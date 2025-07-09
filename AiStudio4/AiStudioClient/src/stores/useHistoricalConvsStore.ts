import { create } from 'zustand';
import { listenToWebSocketEvent } from '@/services/websocket/websocketEvents';
import { apiClient } from '@/services/api/apiClient';
import { webSocketService } from '@/services/websocket/WebSocketService';

export interface HistoricalConv {
  convGuid: string;
  summary: string;
  fileName: string;
  lastModified: string;
  highlightColour?: string;
}

interface ContentBlock {
  content: string;
  contentType: 'text'|'system';
}

interface TreeNode {
  id: string;
  contentBlocks: ContentBlock[];
  children: TreeNode[];
  parentId?: string;
  timestamp?: number;
  durationMs?: number;
  source?: string;
    costInfo?: any;
    cumulativeCost?: number;
    attachments?: any[];
    temperature?: number;
}

interface HistoricalConvsStore {
  
  convs: HistoricalConv[];
  isLoading: boolean;
  isLoadingList: boolean;
  isLoadingConv: boolean;
  error: string | null;
  regeneratingSummaries: Set<string>;

  
  fetchAllConvs: () => Promise<void>;
  fetchConvTree: (convId: string) => Promise<TreeNode | null>;
  addOrUpdateConv: (conv: HistoricalConv) => void;
  deleteConv: (convId: string) => Promise<void>;
  regenerateSummary: (convId: string) => Promise<void>;
  clearError: () => void;
}

export const useHistoricalConvsStore = create<HistoricalConvsStore>((set, get) => {
  
  typeof window !== 'undefined' && 
      listenToWebSocketEvent('historical:update', (detail) => {
          
      const content = detail.content;
      content && get().addOrUpdateConv({
        convGuid: content.convId ?? content.convGuid,
        summary: content.summary ?? content.content ?? 'Untitled Conv',
        fileName: `conv_${content.convId ?? content.convGuid}.json`,
        lastModified: content.lastModified ?? new Date().toISOString(),
        highlightColour: content.highlightColour,
      });
    });

  return {
    
    convs: [],
    isLoading: false,
    isLoadingList: false,
    isLoadingConv: false,
    error: null,
    regeneratingSummaries: new Set<string>(),

    
    fetchAllConvs: async () => {
      const clientId = webSocketService.getClientId();
      if (!clientId) return set({ error: 'No client ID available' });

      set({ isLoading: true, isLoadingList: true, error: null });

      try {
        const { data } = await apiClient.post('/api/getAllHistoricalConvTrees', {});
        
        if (!data.success) throw new Error('Failed to fetch historical convs');
        if (!Array.isArray(data.convs)) throw new Error('Invalid response format');
        
        const newConvs = data.convs.map((conv: any) => ({
          convGuid: conv.convId,
          summary: conv.summary ?? 'Untitled Conv',
          fileName: `conv_${conv.convId}.json`,
          lastModified: conv.lastModified ?? new Date().toISOString(),
          highlightColour: undefined,
        }));

        set({ convs: newConvs, isLoading: false, isLoadingList: false });
      } catch (error) {
        set({
          error: error instanceof Error ? error.message : 'Failed to fetch convs',
          isLoading: false,
          isLoadingList: false,
        });
      }
    },

    fetchConvTree: async (convId: string) => {
      const clientId = webSocketService.getClientId();
      if (!clientId) return set({ error: 'No client ID available' }) ?? null;

      set({ isLoading: true, isLoadingConv: true, error: null });

        try {
          
          const { data } = await apiClient.post('/api/historicalConvTree', { convId });

          if (!data.success) throw new Error('Failed to fetch conv tree');
          if (!data.flatMessageStructure) throw new Error('Invalid response format or empty tree');
          
          
          const flatNodes = data.flatMessageStructure;
          const nodeMap = new Map();
          
          flatNodes.forEach((node: TreeNode) => {
            nodeMap.set(node.id, {
              id: node.id,
              contentBlocks: node.contentBlocks || [],
              children: [],
              parentId: node.parentId, 
              source: node.source, 
                costInfo: node.costInfo,
              cumulativeCost: node.cumulativeCost,
                attachments: node.attachments,
                temperature: node.temperature, 
              timestamp: node.timestamp,
              durationMs: node.durationMs
            });
          });
        
        let rootNode = null;
        flatNodes.forEach((node: TreeNode) => {
          const treeNode = nodeMap.get(node.id);
          !node.parentId ? (rootNode = treeNode) : 
            nodeMap.has(node.parentId) && nodeMap.get(node.parentId).children.push(treeNode);
        });
        
        
        if (data.summary) {
          const convToUpdate = get().convs.find((c) => c.convGuid === convId);
          convToUpdate && get().addOrUpdateConv({
            ...convToUpdate,
            summary: data.summary,
          });
        }
        
        set({ isLoading: false, isLoadingConv: false });
        const result = rootNode ?? (flatNodes.length > 0 ? nodeMap.get(flatNodes[0].id) : null);
        return result;
      } catch (error) {
        set({
          error: error instanceof Error ? error.message : 'Failed to fetch conv tree',
          isLoading: false,
          isLoadingConv: false,
        });
        return null;
      }
    },

      addOrUpdateConv: (conv) => {
          
      set((state) => {
        const exists = state.convs.some((c) => c.convGuid === conv.convGuid);
        
        if (exists) {
          // Update the existing conversation instead of ignoring the changes
          const updatedConvs = state.convs.map((c) => 
            c.convGuid === conv.convGuid ? { ...c, ...conv } : c
          );
          return { convs: updatedConvs };
        } else {
          // Add new conversation to the beginning of the array
          return { convs: [conv, ...state.convs] };
        }
      });
    },

    deleteConv: async (convId) => {
      const clientId = webSocketService.getClientId();
      if (!clientId) return set({ error: 'No client ID available' });

      set({ isLoading: true, error: null });

      try {
        const { data } = await apiClient.post('/api/deleteConv', { convId });
        if (!data.success) throw new Error(data.error ?? 'Failed to delete conv');
        
        set((state) => ({
          convs: state.convs.filter((conv) => conv.convGuid !== convId),
          isLoading: false,
        }));
      } catch (error) {
        set({
          error: error instanceof Error ? error.message : 'Failed to delete conv',
          isLoading: false,
        });
      }
    },

    regenerateSummary: async (convId: string) => {
      const clientId = webSocketService.getClientId();
      if (!clientId) return set({ error: 'No client ID available' });

      // Add convId to regenerating set
      set((state) => ({
        regeneratingSummaries: new Set(state.regeneratingSummaries).add(convId)
      }));

      try {
        const { data } = await apiClient.post('/api/regenerateSummary', { convId });
        
        if (!data.success) {
          throw new Error(data.error || 'Failed to regenerate summary');
        }
        
        // The summary update will come through the WebSocket notification
        // so we don't need to manually update the store here
        
      } catch (error) {
        set({
          error: error instanceof Error ? error.message : 'Failed to regenerate summary',
        });
      } finally {
        // Remove convId from regenerating set
        set((state) => {
          const newSet = new Set(state.regeneratingSummaries);
          newSet.delete(convId);
          return { regeneratingSummaries: newSet };
        });
      }
    },

    clearError: () => set({ error: null }),
  };
});