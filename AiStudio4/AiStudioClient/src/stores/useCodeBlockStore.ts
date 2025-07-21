import { create } from 'zustand';

interface CodeBlockState {
  // State
  collapsedBlocks: Record<string, boolean>;
  rawViewBlocks: Record<string, boolean>;
  
  // Actions
  toggleCollapse: (blockId: string) => void;
  toggleRawView: (blockId: string) => void;
  setCollapsed: (blockId: string, collapsed: boolean) => void;
  setRawView: (blockId: string, isRaw: boolean) => void;
  clearBlockStates: () => void;
  clearMessageBlocks: (messageId: string) => void;
  
  // Bulk actions
  expandAll: () => void;
  collapseAll: (blockIds: string[]) => void;
  
  // Getters
  isCollapsed: (blockId: string) => boolean;
  isRawView: (blockId: string) => boolean;
}

export const useCodeBlockStore = create<CodeBlockState>((set, get) => ({
  // Initial state
  collapsedBlocks: {},
  rawViewBlocks: {},
  
  // Actions
  toggleCollapse: (blockId: string) => {
    set((state) => ({
      collapsedBlocks: {
        ...state.collapsedBlocks,
        [blockId]: !(state.collapsedBlocks[blockId] ?? false) // default to expanded
      }
    }));
  },
  
  toggleRawView: (blockId: string) => {
    set((state) => ({
      rawViewBlocks: {
        ...state.rawViewBlocks,
        [blockId]: !(state.rawViewBlocks[blockId] ?? true) // toggle from default true
      }
    }));
  },
  
  setCollapsed: (blockId: string, collapsed: boolean) => {
    set((state) => ({
      collapsedBlocks: {
        ...state.collapsedBlocks,
        [blockId]: collapsed
      }
    }));
  },
  
  setRawView: (blockId: string, isRaw: boolean) => {
    set((state) => ({
      rawViewBlocks: {
        ...state.rawViewBlocks,
        [blockId]: isRaw
      }
    }));
  },
  
  clearBlockStates: () => {
    set({ collapsedBlocks: {}, rawViewBlocks: {} });
  },
  
  clearMessageBlocks: (messageId: string) => {
    set((state) => {
      const newCollapsedBlocks = { ...state.collapsedBlocks };
      const newRawViewBlocks = { ...state.rawViewBlocks };
      
      // Remove all blocks that start with the messageId
      Object.keys(newCollapsedBlocks).forEach(blockId => {
        if (blockId.startsWith(`${messageId}-`)) {
          delete newCollapsedBlocks[blockId];
        }
      });
      
      Object.keys(newRawViewBlocks).forEach(blockId => {
        if (blockId.startsWith(`${messageId}-`)) {
          delete newRawViewBlocks[blockId];
        }
      });
      
      return {
        collapsedBlocks: newCollapsedBlocks,
        rawViewBlocks: newRawViewBlocks
      };
    });
  },
  
  expandAll: () => {
    set({ collapsedBlocks: {} });
  },
  
  collapseAll: (blockIds: string[]) => {
    const collapsed: Record<string, boolean> = {};
    blockIds.forEach(id => { collapsed[id] = true; });
    set({ collapsedBlocks: collapsed });
  },
  
  // Getters
  isCollapsed: (blockId: string) => {
    return get().collapsedBlocks[blockId] ?? false; // default to expanded
  },
  
  isRawView: (blockId: string) => {
    return get().rawViewBlocks[blockId] ?? true; // default to raw view
  }
}));