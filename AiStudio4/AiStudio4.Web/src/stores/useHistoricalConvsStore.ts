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
    // State
    convs: HistoricalConv[];
    isLoading: boolean;
    error: string | null;
    
    // Actions
    fetchAllConvs: () => Promise<void>;
    fetchConvTree: (convId: string) => Promise<TreeNode | null>;
    addOrUpdateConv: (conv: HistoricalConv) => void;
    deleteConv: (convId: string) => Promise<void>;
    clearError: () => void;
}

export const useHistoricalConvsStore = create<HistoricalConvsStore>((set, get) => {
    // Set up event listeners for historical conv updates
    if (typeof window !== 'undefined') {
        listenToWebSocketEvent('historical:update', (detail) => {
            const content = detail.content;
            if (content) {
                // Get the current state
                const store = get();
                
                // Use the store's action to update the conv
                store.addOrUpdateConv({
                    convGuid: content.convId || content.convGuid,
                    summary: content.summary || content.content || 'Untitled Conv',
                    fileName: `conv_${content.convId || content.convGuid}.json`,
                    lastModified: content.lastModified || new Date().toISOString(),
                    highlightColour: content.highlightColour
                });
            }
        });
    }
    
    return {
        // Initial state
        convs: [],
        isLoading: false,
        error: null,
        
        // Actions
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
                
                // Process and update state with the received convs
                const newConvs = data.convs.map((conv: any) => ({
                    convGuid: conv.convId,
                    summary: conv.summary || 'Untitled Conv',
                    fileName: `conv_${conv.convId}.json`,
                    lastModified: conv.lastModified || new Date().toISOString(),
                    highlightColour: undefined
                }));

                set({ convs: newConvs, isLoading: false });
            } catch (error) {
                console.error('Error fetching historical convs:', error);
                set({ 
                    error: error instanceof Error ? error.message : 'Failed to fetch convs',
                    isLoading: false
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
                    convId
                });
                
                const data = response.data;
                
                if (!data.success) {
                    throw new Error('Failed to fetch conv tree');
                }
                
                if (!data.flatMessageStructure) {
                    throw new Error('Invalid response format or empty tree');
                }
                
                // Convert flat array to hierarchical tree structure
                const flatNodes = data.flatMessageStructure;
                const nodeMap = new Map();

                // First pass: create all nodes
                flatNodes.forEach((node: TreeNode) => {
                    nodeMap.set(node.id, {
                        id: node.id,
                        text: node.text,
                        children: [],
                        parentId: node.parentId, // Keep parentId for easier reference
                        source: node.source,    // Keep source information if available
                        tokenUsage: node.tokenUsage // Store token usage information
                    });
                });

                // Second pass: build the tree by connecting parents and children
                let rootNode = null;
                flatNodes.forEach((node: TreeNode) => {
                    const treeNode = nodeMap.get(node.id);

                    if (!node.parentId) {
                        // This is a root node
                        rootNode = treeNode;
                    } else if (nodeMap.has(node.parentId)) {
                        // Add this node as a child of its parent
                        const parentNode = nodeMap.get(node.parentId);
                        parentNode.children.push(treeNode);
                    }
                });

                // Store the summary from the data for future reference
                if (data.summary) {
                    // Update the conv in our store with the summary
                    const store = get();
                    const convToUpdate = store.convs.find(c => c.convGuid === convId);
                    if (convToUpdate) {
                        store.addOrUpdateConv({
                            ...convToUpdate,
                            summary: data.summary
                        });
                    }
                }

                // Return the root node or the first node
                set({ isLoading: false });
                
                if (rootNode) {
                    return rootNode;
                } else if (flatNodes.length > 0) {
                    // If no explicit root found, use the first node
                    return nodeMap.get(flatNodes[0].id);
                } else {
                    return null;
                }
            } catch (error) {
                console.error('Error fetching conv tree:', error);
                set({ 
                    error: error instanceof Error ? error.message : 'Failed to fetch conv tree',
                    isLoading: false
                });
                return null;
            }
        },
        
        addOrUpdateConv: (conv) => {
            set(state => {
                // Check if conv already exists
                const exists = state.convs.some(conv => conv.convGuid === conv.convGuid);
                
                if (exists) {
                    // Update existing conv
                    return {
                        convs: state.convs.map(conv =>
                            conv.convGuid === conv.convGuid ? conv : conv
                        )
                    };
                } else {
                    // Add new conv at the beginning of the list
                    return {
                        convs: [conv, ...state.convs]
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
                    convId
                });
                
                const data = response.data;
                
                if (!data.success) {
                    throw new Error(data.error || 'Failed to delete conv');
                }
                
                // Remove conv from the store
                set(state => ({
                    convs: state.convs.filter(conv => conv.convGuid !== convId),
                    isLoading: false
                }));
            } catch (error) {
                console.error('Error deleting conv:', error);
                set({ 
                    error: error instanceof Error ? error.message : 'Failed to delete conv',
                    isLoading: false
                });
            }
        },
        
        clearError: () => set({ error: null })
    };
});

// Debug helper for console
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

// Export for console access
(window as any).debugHistoricalConvs = debugHistoricalConvs;