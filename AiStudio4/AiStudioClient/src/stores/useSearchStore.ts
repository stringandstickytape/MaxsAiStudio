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
  searchResults: SearchResult[] | null; // Can be null when no search is active
  searchError: string | null;
  highlightedMessageId: string | null;
  currentMatchIndex: number; // Track the current match index for cycling
  
  // Actions
  setSearchTerm: (term: string) => void;
  startSearch: () => void;
  cancelSearch: () => void;
  clearSearch: () => void;
  addSearchResult: (result: SearchResult) => void;
  markSearchComplete: () => void;
  setSearchError: (error: string | null) => void;
  highlightMessage: (messageId: string | null) => void;
  cycleToNextMatch: (convId: string) => void;
  cycleToPrevMatch: (convId: string) => void;
  getMatchStats: (convId: string) => { current: number, total: number } | null;
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
    searchResults: null,
    searchError: null,
    highlightedMessageId: null,
    currentMatchIndex: 0,
    
    setSearchTerm: (term) => set({ searchTerm: term }),

    startSearch: () => {
      const { searchTerm } = get();
      if (!searchTerm.trim()) {
        set({
          searchResults: null,
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
        searchResults: [],  // Initialize as empty array when starting a search
        searchError: null,
        isSearching: true,
        highlightedMessageId: null,
        currentMatchIndex: 0
      });
      
      // Add a small delay to ensure UI is responsive during search initialization
      setTimeout(() => {
        
        webSocketService.send({
          messageType: 'searchConversations',
          content: {
            searchTerm,
            searchId
          }
        });
      }, 50); // Small delay to allow UI to update before search begins
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
        highlightedMessageId: null,
        currentMatchIndex: 0
      });
    },
    
    // Batch update timer reference
    _batchUpdateTimer: null as NodeJS.Timeout | null,
    _pendingResults: [] as SearchResult[],
    
    addSearchResult: (result) => {
      const state = get();
      
      // Add to pending results if not a duplicate
      if (!state._pendingResults.some(r => r.conversationId === result.conversationId) && 
          !state.searchResults?.some(r => r.conversationId === result.conversationId)) {
        state._pendingResults.push(result);
      }
      
      // If we already have a timer, let it complete
      if (state._batchUpdateTimer) return;
      
      // Set a timer to batch update the UI
      const timer = setTimeout(() => {
        set((currentState) => {
          // Apply all pending results at once
          if (currentState._pendingResults.length === 0) return {};
          
          const newResults = currentState.searchResults || [];
          const updatedResults = [...newResults, ...currentState._pendingResults];
          
          return { 
            searchResults: updatedResults,
            _pendingResults: [],
            _batchUpdateTimer: null
          };
        });
      }, 200); // Update UI every 200ms at most
      
      // Store the timer reference
      set({ _batchUpdateTimer: timer });
    },

    markSearchComplete: () => set({ isSearching: false }),

    setSearchError: (error) => set({ searchError: error }),

    highlightMessage: (messageId) => set({ highlightedMessageId: messageId }),

    // Cycle to the next matching message in the current conversation
    cycleToNextMatch: (convId) => {
      const state = get();
      const searchResult = state.searchResults?.find(result => result.conversationId === convId);
      
      if (!searchResult || searchResult.matchingMessageIds.length === 0) return;
      
      // Calculate the next index, wrapping around if needed
      const currentIndex = state.currentMatchIndex;
      const totalMatches = searchResult.matchingMessageIds.length;
      const nextIndex = (currentIndex + 1) % totalMatches;
      
      // Update the highlighted message and current index
      set({
        highlightedMessageId: searchResult.matchingMessageIds[nextIndex],
        currentMatchIndex: nextIndex
      });
    },

    // Cycle to the previous matching message in the current conversation
    cycleToPrevMatch: (convId) => {
      const state = get();
      const searchResult = state.searchResults?.find(result => result.conversationId === convId);
      
      if (!searchResult || searchResult.matchingMessageIds.length === 0) return;
      
      // Calculate the previous index, wrapping around if needed
      const currentIndex = state.currentMatchIndex;
      const totalMatches = searchResult.matchingMessageIds.length;
      const prevIndex = (currentIndex - 1 + totalMatches) % totalMatches;
      
      // Update the highlighted message and current index
      set({
        highlightedMessageId: searchResult.matchingMessageIds[prevIndex],
        currentMatchIndex: prevIndex
      });
    },

    // Get current match statistics for a conversation
    getMatchStats: (convId) => {
      const state = get();
      const searchResult = state.searchResults?.find(result => result.conversationId === convId);
      
      if (!searchResult || searchResult.matchingMessageIds.length === 0) return null;
      
      return {
        current: state.currentMatchIndex + 1, // 1-based for display
        total: searchResult.matchingMessageIds.length
      };
    }
  };
});