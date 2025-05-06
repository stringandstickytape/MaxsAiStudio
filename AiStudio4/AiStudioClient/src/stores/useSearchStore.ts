// src/stores/useSearchStore.ts
import { create } from 'zustand';
import { v4 as uuidv4 } from 'uuid';
import { webSocketService } from '@/services/websocket/WebSocketService';

interface SearchResult {
  conversationId: string;
  matchingMessageIds: string[];
  summary: string;
  lastModified: string;
}

interface SearchState {
  searchTerm: string;
  isSearching: boolean;
  searchId: string | null;
  searchResults: SearchResult[]; // Always an array, never null for streaming
  searchError: string | null;
  highlightedMessageId: string | null;
  
  // Actions
  setSearchTerm: (term: string) => void;
  startSearch: () => void;
  cancelSearch: () => void;
  clearSearch: () => void;
  addSearchResult: (result: SearchResult) => void;
  markSearchComplete: () => void;
  setSearchError: (error: string | null) => void;
  highlightMessage: (messageId: string | null) => void;
}

export const useSearchStore = create<SearchState>((set, get) => {
  // Initialize WebSocket event listeners
  if (typeof window !== 'undefined') {
    // Listen for search events dispatched as 'message:received' (see WebSocketService)
    window.addEventListener('message:received', (event: any) => {
      const data = event.detail;
      const currentSearchId = get().searchId;
      const type = data.messageType;
      if (type === 'searchStarted') {
        const { searchId } = data.content;
        if (searchId === currentSearchId) set({ isSearching: true });
      } else if (type === 'searchResultPartial') {
        const { searchId, result } = data.content;
        if (searchId === currentSearchId) {
          get().addSearchResult(result);
        }
      } else if (type === 'searchResultsComplete') {
        const { searchId } = data.content;
        if (searchId === currentSearchId) {
          get().markSearchComplete();
        }
      } else if (type === 'searchCancelled') {
        const { searchId } = data.content;
        if (searchId === currentSearchId) set({ isSearching: false });
      } else if (type === 'searchError') {
        set({ searchError: data.content.error, isSearching: false });
      }
    });
  }
  
  return {
    searchTerm: '',
    isSearching: false,
    searchId: null,
    searchResults: [],
    searchError: null,
    highlightedMessageId: null,
    
    setSearchTerm: (term) => set({ searchTerm: term }),

    startSearch: () => {
      const { searchTerm } = get();
      if (!searchTerm.trim()) {
        set({
          searchResults: [],
          searchError: null,
          searchId: null,
          isSearching: false
        });
        return;
      }
      const prevSearchId = get().searchId;
      if (prevSearchId) {
        webSocketService.send({
          messageType: 'cancelSearch',
          content: { searchId: prevSearchId }
        });
      }
      const searchId = uuidv4();
      set({
        searchId,
        searchResults: [],
        searchError: null,
        isSearching: true,
        highlightedMessageId: null
      });
        
      webSocketService.send({
        messageType: 'searchConversations',
        content: {
          searchTerm,
          searchId
        }
      });
    },
    
    cancelSearch: () => {
      const { searchId } = get();
      
      if (searchId) {
        webSocketService.send({
          messageType: 'cancelSearch',
          content: { searchId }
        });
        
        set({
          isSearching: false
        });
      }
    },
    
    clearSearch: () => {
      const { searchId } = get();
      
      // Cancel ongoing search if any
      if (searchId && get().isSearching) {
        webSocketService.send({
          messageType: 'cancelSearch',
          content: { searchId }
        });
      }
      
      // Reset state
      set({
        searchTerm: '',
        isSearching: false,
        searchId: null,
        searchResults: null,
        searchError: null,
        highlightedMessageId: null
      });
    },
    
    addSearchResult: (result) => {
      // Avoid duplicates
      set((state) => {
        if (state.searchResults.some(r => r.conversationId === result.conversationId)) {
          return {};
        }
        return { searchResults: [...state.searchResults, result] };
      });
    },

    markSearchComplete: () => set({ isSearching: false }),

    setSearchError: (error) => set({ searchError: error }),

    highlightMessage: (messageId) => set({ highlightedMessageId: messageId })
  };
});