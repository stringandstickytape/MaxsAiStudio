// src/stores/useToolStore.ts
import { create } from 'zustand';
import { Tool, ToolCategory } from '@/types/toolTypes';

interface ToolStore {
  // State
  tools: Tool[];
  categories: ToolCategory[];
  activeTools: string[];
  loading: boolean;
  error: string | null;

  // Actions
  setTools: (tools: Tool[]) => void;
  setCategories: (categories: ToolCategory[]) => void;
  setActiveTools: (toolIds: string[]) => void;
  addActiveTool: (toolId: string) => void;
  removeActiveTool: (toolId: string) => void;
  clearActiveTools: () => void;
  setLoading: (loading: boolean) => void;
  setError: (error: string | null) => void;
}

export const useToolStore = create<ToolStore>((set) => ({
  // Initial state
  tools: [],
  categories: [],
  activeTools: [],
  loading: false,
  error: null,

  // Actions
  setTools: (tools) => set({ tools }),

  setCategories: (categories) => set({ categories }),

  setActiveTools: (toolIds) => set({ activeTools: toolIds }),

  addActiveTool: (toolId) =>
    set((state) => ({
      activeTools: state.activeTools.includes(toolId) ? state.activeTools : [...state.activeTools, toolId],
    })),

  removeActiveTool: (toolId) =>
    set((state) => ({
      activeTools: state.activeTools.filter((id) => id !== toolId),
    })),

  clearActiveTools: () => set({ activeTools: [] }),

  setLoading: (loading) => set({ loading }),

  setError: (error) => set({ error }),
}));

// Debug helper for console
export const debugToolStore = () => {
  const state = useToolStore.getState();
  console.group('Tool Store Debug');
  console.log('Tools:', state.tools);
  console.log('Categories:', state.categories);
  console.log('Active Tools:', state.activeTools);
  console.log('Loading:', state.loading);
  console.log('Error:', state.error);
  console.groupEnd();
  return state;
};

// Export for console access
(window as any).debugToolStore = debugToolStore;
