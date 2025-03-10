// src/stores/useHistoricalConversationsStore.ts
import { create } from 'zustand';
import { webSocketService } from '@/services/websocket/WebSocketService';
import { listenToWebSocketEvent } from '@/services/websocket/websocketEvents';
import { apiClient } from '@/services/api/apiClient';

export interface HistoricalConversation {
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

interface HistoricalConversationsStore {
    // State
    conversations: HistoricalConversation[];
    isLoading: boolean;
    error: string | null;
    
    // Actions
    fetchAllConversations: () => Promise<void>;
    fetchConversationTree: (conversationId: string) => Promise<TreeNode | null>;
    addOrUpdateConversation: (conversation: HistoricalConversation) => void;
    deleteConversation: (conversationId: string) => Promise<void>;
    clearError: () => void;
}

export const useHistoricalConversationsStore = create<HistoricalConversationsStore>((set, get) => {
    // Set up event listeners for historical conversation updates
    if (typeof window !== 'undefined') {
        listenToWebSocketEvent('historical:update', (detail) => {
            const content = detail.content;
            if (content) {
                // Get the current state
                const store = get();
                
                // Use the store's action to update the conversation
                store.addOrUpdateConversation({
                    convGuid: content.conversationId || content.convGuid,
                    summary: content.summary || content.content || 'Untitled Conversation',
                    fileName: `conv_${content.conversationId || content.convGuid}.json`,
                    lastModified: content.lastModified || new Date().toISOString(),
                    highlightColour: content.highlightColour
                });
            }
        });
    }
    
    return {
        // Initial state
        conversations: [],
        isLoading: false,
        error: null,
        
        // Actions
        fetchAllConversations: async () => {
            const clientId = localStorage.getItem('clientId');
            if (!clientId) {
                set({ error: 'No client ID available' });
                return;
            }
            
            set({ isLoading: true, error: null });
            
            try {
                const response = await apiClient.post('/api/getAllHistoricalConversationTrees', {});
                const data = response.data;
                
                if (!data.success) {
                    throw new Error('Failed to fetch historical conversations');
                }
                
                if (!Array.isArray(data.conversations)) {
                    throw new Error('Invalid response format');
                }
                
                // Process and update state with the received conversations
                const newConversations = data.conversations.map((conv: any) => ({
                    convGuid: conv.conversationId,
                    summary: conv.summary || 'Untitled Conversation',
                    fileName: `conv_${conv.conversationId}.json`,
                    lastModified: conv.lastModified || new Date().toISOString(),
                    highlightColour: undefined
                }));

                set({ conversations: newConversations, isLoading: false });
            } catch (error) {
                console.error('Error fetching historical conversations:', error);
                set({ 
                    error: error instanceof Error ? error.message : 'Failed to fetch conversations',
                    isLoading: false
                });
            }
        },
        
        fetchConversationTree: async (conversationId: string) => {
            const clientId = localStorage.getItem('clientId');
            if (!clientId) {
                set({ error: 'No client ID available' });
                return null;
            }
            
            set({ isLoading: true, error: null });
            
            try {
                const response = await apiClient.post('/api/historicalConversationTree', {
                    conversationId
                });
                
                const data = response.data;
                
                if (!data.success) {
                    throw new Error('Failed to fetch conversation tree');
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
                    // Update the conversation in our store with the summary
                    const store = get();
                    const conversationToUpdate = store.conversations.find(c => c.convGuid === conversationId);
                    if (conversationToUpdate) {
                        store.addOrUpdateConversation({
                            ...conversationToUpdate,
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
                console.error('Error fetching conversation tree:', error);
                set({ 
                    error: error instanceof Error ? error.message : 'Failed to fetch conversation tree',
                    isLoading: false
                });
                return null;
            }
        },
        
        addOrUpdateConversation: (conversation) => {
            set(state => {
                // Check if conversation already exists
                const exists = state.conversations.some(conv => conv.convGuid === conversation.convGuid);
                
                if (exists) {
                    // Update existing conversation
                    return {
                        conversations: state.conversations.map(conv =>
                            conv.convGuid === conversation.convGuid ? conversation : conv
                        )
                    };
                } else {
                    // Add new conversation at the beginning of the list
                    return {
                        conversations: [conversation, ...state.conversations]
                    };
                }
            });
        },
        
        deleteConversation: async (conversationId) => {
            const clientId = localStorage.getItem('clientId');
            if (!clientId) {
                set({ error: 'No client ID available' });
                return;
            }
            
            set({ isLoading: true, error: null });
            
            try {
                const response = await apiClient.post('/api/deleteConversation', {
                    conversationId
                });
                
                const data = response.data;
                
                if (!data.success) {
                    throw new Error(data.error || 'Failed to delete conversation');
                }
                
                // Remove conversation from the store
                set(state => ({
                    conversations: state.conversations.filter(conv => conv.convGuid !== conversationId),
                    isLoading: false
                }));
            } catch (error) {
                console.error('Error deleting conversation:', error);
                set({ 
                    error: error instanceof Error ? error.message : 'Failed to delete conversation',
                    isLoading: false
                });
            }
        },
        
        clearError: () => set({ error: null })
    };
});

// Debug helper for console
export const debugHistoricalConversations = () => {
    const state = useHistoricalConversationsStore.getState();
    console.group('Historical Conversations Debug');
    console.log('Conversations Count:', state.conversations.length);
    console.log('All Conversations:', state.conversations);
    console.log('Loading:', state.isLoading);
    console.log('Error:', state.error);
    console.groupEnd();
    return state;
};

// Export for console access
(window as any).debugHistoricalConversations = debugHistoricalConversations;