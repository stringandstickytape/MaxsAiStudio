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
  
  togglePanel: (id) => {
    set((state) => {
      const panel = state.panels[id];
      if (!panel) {
        return state;
      }
      
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
    });
  },
  
  togglePinned: (id) => {
    set((state) => {
      const panel = state.panels[id];
      if (!panel) {
        console.warn(`togglePinned: Panel with id ${id} not found`);
        return state;
      }
      
     
      return {
        panels: {
          ...state.panels,
          [id]: {
            ...panel,
            isPinned: !panel.isPinned
          }
        }
      };
    });
  },
  
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
  
  registerPanel: (panel) => {
    // Check localStorage for saved panel state
    let savedState: Partial<PanelState> = {};
    try {
      const savedLayout = localStorage.getItem('panel-layout');
      if (savedLayout) {
        const parsedLayout = JSON.parse(savedLayout);
        if (parsedLayout[panel.id]) {
          savedState = parsedLayout[panel.id];
        }
      }
    } catch (e) {
      console.warn('Failed to parse saved panel state:', e);
    }
    
    return set((state) => {

    const existingPanel = state.panels[panel.id];
    if (existingPanel) {
      return {
        panels: {
          ...state.panels,
          [panel.id]: {
            ...panel,
            isOpen: existingPanel.isOpen,
            isPinned: existingPanel.isPinned
          }
        }
      };
    }
    
    return {
      panels: {
        ...state.panels,
        [panel.id]: panel
      }
    };
  });
  }
}));
