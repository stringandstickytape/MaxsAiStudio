// src/stores/usePanelStore.ts
import { create } from 'zustand';
import { PanelState } from '@/types/ui';

interface PanelStore {
  panels: Record<string, PanelState>;
  togglePanel: (id: string) => void;
  togglePinned: (id: string) => void;
  closeAll: (except?: string) => void;
  getPanelState: (id: string) => PanelState | undefined;
  setSize: (id: string, size: string) => void;
  registerPanel: (panel: PanelState) => void;
}

export const usePanelStore = create<PanelStore>((set, get) => ({
  panels: {},
  
  togglePanel: (id) => set((state) => {
    const panel = state.panels[id];
    if (!panel) return state;

    // Create a copy of panels first
    const updatedPanels = { ...state.panels };
    
    // Update the panel state
    updatedPanels[id] = {
      ...panel,
      isOpen: !panel.isOpen
    };
    
    // If opening this panel and it's not pinned, close other panels on the same side
    if (!panel.isOpen && !panel.isPinned) {
      Object.keys(updatedPanels).forEach(key => {
        if (key !== id && 
            updatedPanels[key].position === panel.position && 
            !updatedPanels[key].isPinned) {
          updatedPanels[key] = {
            ...updatedPanels[key],
            isOpen: false
          };
        }
      });
    }
    
    return { panels: updatedPanels };
  }),
  
  togglePinned: (id) => set((state) => {
    const panel = state.panels[id];
    if (!panel) return state;
    
    return {
      panels: {
        ...state.panels,
        [id]: {
          ...panel,
          isPinned: !panel.isPinned
        }
      }
    };
  }),
  
  closeAll: (except) => set((state) => {
    const updatedPanels = { ...state.panels };
    
    Object.keys(updatedPanels).forEach(key => {
      if (key !== except && !updatedPanels[key].isPinned) {
        updatedPanels[key] = {
          ...updatedPanels[key],
          isOpen: false
        };
      }
    });
    
    return { panels: updatedPanels };
  }),
  
  getPanelState: (id) => {
    return get().panels[id];
  },
  
  setSize: (id, size) => set((state) => {
    const panel = state.panels[id];
    if (!panel) return state;
    
    return {
      panels: {
        ...state.panels,
        [id]: {
          ...panel,
          size
        }
      }
    };
  }),
  
  registerPanel: (panel) => set((state) => {
    // Only update if the panel doesn't exist or has changed
    if (!state.panels[panel.id] || 
        JSON.stringify(state.panels[panel.id]) !== JSON.stringify(panel)) {
      return {
        panels: {
          ...state.panels,
          [panel.id]: panel
        }
      };
    }
    return state;
  })
}));

// Debug helper for console
export const debugPanels = () => {
  const state = usePanelStore.getState();
  console.group('Panel State Debug');
  console.log('Panels:', state.panels);
  
  // List open panels
  const openPanels = Object.entries(state.panels)
    .filter(([_, panel]) => panel.isOpen)
    .map(([id]) => id);
  console.log('Open Panels:', openPanels);
  
  // List pinned panels
  const pinnedPanels = Object.entries(state.panels)
    .filter(([_, panel]) => panel.isPinned)
    .map(([id]) => id);
  console.log('Pinned Panels:', pinnedPanels);
  
  console.groupEnd();
  return state;
};

// Export for console access
(window as any).debugPanels = debugPanels;