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
  searchResults: SearchResult[] | null;
  searchError: string | null;
  highlightedMessageId: string | null;
  
  // Actions
  setSearchTerm: (term: string) => void;
  startSearch: () => void;
  cancelSearch: () => void;
  clearSearch: () => void;
  setSearchResults: (results: SearchResult[]) => void;
  setSearchError: (error: string | null) => void;
  highlightMessage: (messageId: string | null) => void;
}

export const useSearchStore = create<SearchState>((set, get) => {
  // Initialize WebSocket event listeners
  if (typeof window !== 'undefined') {
    // Listen for search results
    window.addEventListener('websocket:message', (event: any) => {
      const data = event.detail;
      
      if (data.messageType === 'searchResults') {
        const { searchId, results } = data.content;
        const currentSearchId = get().searchId;
        
        // Only process if this is the current search
        if (searchId === currentSearchId) {
          set({
            searchResults: results,
            isSearching: false
          });
        }
      }
      else if (data.messageType === 'searchStarted') {
        const { searchId } = data.content;
        const currentSearchId = get().searchId;
        
        // Only process if this is the current search
        if (searchId === currentSearchId) {
          set({ isSearching: true });
        }
      }
      else if (data.messageType === 'searchCancelled') {
        const { searchId } = data.content;
        const currentSearchId = get().searchId;
        
        // Only process if this is the current search
        if (searchId === currentSearchId) {
          set({ isSearching: false });
        }
      }
      else if (data.messageType === 'searchError') {
        set({
          searchError: data.content.error,
          isSearching: false
        });
      }
    });
  }
  
  return {
    searchTerm: '',
    isSearching: false,
    searchId: null,
    searchResults: null,
    searchError: null,
    highlightedMessageId: null,
    
    setSearchTerm: (term) => set({ searchTerm: term }),
    
    startSearch: () => {
      const { searchTerm } = get();
      
      // Don't search if term is empty
      if (!searchTerm.trim()) {
        set({
          searchResults: null,
          searchError: null,
          searchId: null,
          isSearching: false
        });
        return;
      }
      
      // Cancel previous search if any
      const prevSearchId = get().searchId;
      if (prevSearchId) {
        webSocketService.send({
          messageType: 'cancelSearch',
          content: { searchId: prevSearchId }
        });
      }
      
      // Generate new search ID
      const searchId = uuidv4();
      
      // Update state
      set({
        searchId,
        searchResults: null,
        searchError: null,
        isSearching: true,
        highlightedMessageId: null
      });
      
      // Send search request
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
    
    setSearchResults: (results) => set({ searchResults: results }),
    
    setSearchError: (error) => set({ searchError: error }),
    
    highlightMessage: (messageId) => set({ highlightedMessageId: messageId })
  };
});