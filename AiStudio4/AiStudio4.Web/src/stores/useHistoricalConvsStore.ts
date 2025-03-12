// src/stores/useHistoricalConvsStore.ts
import { create } from 'zustand';
import { listenToWebSocketEvent } from '@/services/websocket/websocketEvents';
import { apiClient } from '@/services/api/apiClient';

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
  
  if (typeof window !== 'undefined') {
    listenToWebSocketEvent('historical:update', (detail) => {
      const content = detail.content;
      if (content) {
        
        const store = get();

        
        store.addOrUpdateConv({
          convGuid: content.convId || content.convGuid,
          summary: content.summary || content.content || 'Untitled Conv',
          fileName: `conv_${content.convId || content.convGuid}.json`,
          lastModified: content.lastModified || new Date().toISOString(),
          highlightColour: content.highlightColour,
        });
      }
    });
  }

  return {
    
    convs: [],
    isLoading: false,
    error: null,

    
    fetchAllConvs: async () => {
      const clientId = localStorage.getItem('clientId');
      if (!clientId) {
        set({ error: 'No client ID available' });
        return;
      }

      set({ isLoading: true, error: null });

      try {
        const response = await apiClient.post('/api/getAllHistoricalConvTrees', {});
        const data = response.data;

        if (!data.success) {
          throw new Error('Failed to fetch historical convs');
        }

        if (!Array.isArray(data.convs)) {
          throw new Error('Invalid response format');
        }

        
        const newConvs = data.convs.map((conv: any) => ({
          convGuid: conv.convId,
          summary: conv.summary || 'Untitled Conv',
          fileName: `conv_${conv.convId}.json`,
          lastModified: conv.lastModified || new Date().toISOString(),
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
      const clientId = localStorage.getItem('clientId');
      if (!clientId) {
        set({ error: 'No client ID available' });
        return null;
      }

      set({ isLoading: true, error: null });

      try {
        const response = await apiClient.post('/api/historicalConvTree', {
          convId,
        });

        const data = response.data;

        if (!data.success) {
          throw new Error('Failed to fetch conv tree');
        }

        if (!data.flatMessageStructure) {
          throw new Error('Invalid response format or empty tree');
        }

        
        const flatNodes = data.flatMessageStructure;
        const nodeMap = new Map();

        
        flatNodes.forEach((node: TreeNode) => {
          nodeMap.set(node.id, {
            id: node.id,
            text: node.text,
            children: [],
            parentId: node.parentId, 
            source: node.source, 
            costInfo: node.costInfo
          });
        });

        
        let rootNode = null;
        flatNodes.forEach((node: TreeNode) => {
          const treeNode = nodeMap.get(node.id);

          if (!node.parentId) {
            
            rootNode = treeNode;
          } else if (nodeMap.has(node.parentId)) {
            
            const parentNode = nodeMap.get(node.parentId);
            parentNode.children.push(treeNode);
          }
        });

        
        if (data.summary) {
          
          const store = get();
          const convToUpdate = store.convs.find((c) => c.convGuid === convId);
          if (convToUpdate) {
            store.addOrUpdateConv({
              ...convToUpdate,
              summary: data.summary,
            });
          }
        }

        
        set({ isLoading: false });

        if (rootNode) {
          return rootNode;
        } else if (flatNodes.length > 0) {
          
          return nodeMap.get(flatNodes[0].id);
        } else {
          return null;
        }
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
        
        const exists = state.convs.some((conv) => conv.convGuid === conv.convGuid);

        if (exists) {
          
          return {
            convs: state.convs.map((conv) => (conv.convGuid === conv.convGuid ? conv : conv)),
          };
        } else {
          
          return {
            convs: [conv, ...state.convs],
          };
        }
      });
    },

    deleteConv: async (convId) => {
      const clientId = localStorage.getItem('clientId');
      if (!clientId) {
        set({ error: 'No client ID available' });
        return;
      }

      set({ isLoading: true, error: null });

      try {
        const response = await apiClient.post('/api/deleteConv', {
          convId,
        });

        const data = response.data;

        if (!data.success) {
          throw new Error(data.error || 'Failed to delete conv');
        }

        
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


export const debugHistoricalConvs = () => {
  const state = useHistoricalConvsStore.getState();
  console.group('Historical Convs Debug');
  console.log('Convs Count:', state.convs.length);
  console.log('All Convs:', state.convs);
  console.log('Loading:', state.isLoading);
  console.log('Error:', state.error);
  console.groupEnd();
  return state;
};


(window as any).debugHistoricalConvs = debugHistoricalConvs;

