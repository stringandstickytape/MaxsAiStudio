
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

interface TreeNode {
  id: string;
  text: string;
  children: TreeNode[];
  parentId?: string;
  timestamp?: number;
  durationMs?: number;
  source?: string;
  costInfo?: any;
  attachments?: any[];
}

interface HistoricalConvsStore {
  
  convs: HistoricalConv[];
  isLoading: boolean;
  error: string | null;

  
  fetchAllConvs: () => Promise<void>;
  fetchConvTree: (convId: string) => Promise<TreeNode | null>;
  addOrUpdateConv: (conv: HistoricalConv) => void;
  deleteConv: (convId: string) => Promise<void>;
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
    error: null,

    
    fetchAllConvs: async () => {
      const clientId = webSocketService.getClientId();
      if (!clientId) return set({ error: 'No client ID available' });

      set({ isLoading: true, error: null });

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

        set({ convs: newConvs, isLoading: false });
      } catch (error) {
        console.error('Error fetching historical convs:', error);
        set({
          error: error instanceof Error ? error.message : 'Failed to fetch convs',
          isLoading: false,
        });
      }
    },

    fetchConvTree: async (convId: string) => {
      const clientId = webSocketService.getClientId();
      if (!clientId) return set({ error: 'No client ID available' }) ?? null;

      set({ isLoading: true, error: null });

        try {
          console.log('Fetching historical conv tree for convId:', convId);
          const { data } = await apiClient.post('/api/historicalConvTree', { convId });

          if (!data.success) throw new Error('Failed to fetch conv tree');
          if (!data.flatMessageStructure) throw new Error('Invalid response format or empty tree');
          
          console.log('Historical conv tree data received:', data);
          console.log('Flat message structure:', data.flatMessageStructure);
          
          const flatNodes = data.flatMessageStructure;
          const nodeMap = new Map();
          
          flatNodes.forEach((node: TreeNode) => {
            nodeMap.set(node.id, {
              id: node.id,
              text: node.text,
              children: [],
              parentId: node.parentId, 
              source: node.source, 
              costInfo: node.costInfo,
              attachments: node.attachments,
              timestamp: node.timestamp || null,
              durationMs: node.durationMs || null
            });
          });
        
        let rootNode = null;
        flatNodes.forEach((node: TreeNode) => {
          const treeNode = nodeMap.get(node.id);
          !node.parentId ? (rootNode = treeNode) : 
            nodeMap.has(node.parentId) && nodeMap.get(node.parentId).children.push(treeNode);
        });
        
        console.log('Extracted flat nodes:', flatNodes.map(node => ({
          id: node.id,
          source: node.source,
          timestamp: node.timestamp,
          durationMs: node.durationMs
        })));
        
        if (data.summary) {
          const convToUpdate = get().convs.find((c) => c.convGuid === convId);
          convToUpdate && get().addOrUpdateConv({
            ...convToUpdate,
            summary: data.summary,
          });
        }
        
        set({ isLoading: false });
        const result = rootNode ?? (flatNodes.length > 0 ? nodeMap.get(flatNodes[0].id) : null);
        console.log('Final tree rootNode:', result);
        return result;
      } catch (error) {
        console.error('Error fetching conv tree:', error);
        set({
          error: error instanceof Error ? error.message : 'Failed to fetch conv tree',
          isLoading: false,
        });
        return null;
      }
    },

    addOrUpdateConv: (conv) => {
      set((state) => {
        const exists = state.convs.some((c) => c.convGuid === conv.convGuid);
        return exists ? { convs: state.convs } : { convs: [conv, ...state.convs] };
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
        console.error('Error deleting conv:', error);
        set({
          error: error instanceof Error ? error.message : 'Failed to delete conv',
          isLoading: false,
        });
      }
    },

    clearError: () => set({ error: null }),
  };
});
