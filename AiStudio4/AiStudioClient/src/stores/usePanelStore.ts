
import { create } from 'zustand';
import { PanelState } from '@/types/ui';

interface PanelStore {
  panels: Record<string, PanelState>;
  togglePanel: (id: string) => void;
  toggleSidebarCollapse: () => void;
  closeAll: (except?: string) => void;
  getPanelState: (id: string) => PanelState | undefined;
  setSize: (id: string, size: string) => void;
  registerPanel: (panel: PanelState) => void;
  saveState: () => boolean;
}

export const usePanelStore = create<PanelStore>((set, get) => ({
  panels: {},

  togglePanel: (id) => {
    set((state) => {
      const panel = state.panels[id];
      if (!panel) {
        return state;
      }

      // Special handling for sidebar - toggle between collapsed and expanded
      if (id === 'sidebar') {
        return {
          panels: {
            ...state.panels,
            sidebar: {
              ...panel,
              isOpen: true, // Always keep sidebar open
              isCollapsed: !panel.isCollapsed // Toggle collapsed state instead
            }
          }
        };
      }

      // Normal behavior for other panels
      const updatedPanels = { ...state.panels };

      updatedPanels[id] = {
        ...panel,
        isOpen: !panel.isOpen
      };
      
      if (!panel.isOpen) {
        Object.keys(updatedPanels).forEach((key) => {
          if (key !== id && updatedPanels[key].position === panel.position) {
            updatedPanels[key] = {
              ...updatedPanels[key],
              isOpen: false,
            };
          }
        });
      }

      return { panels: updatedPanels };
    });
  },

  // toggleSidebarCollapse is now handled by togglePanel for the sidebar
  toggleSidebarCollapse: () => 
    set((state) => {
      const sidebar = state.panels['sidebar'];
      if (!sidebar) return state;

      return {
        panels: {
          ...state.panels,
          sidebar: {
            ...sidebar,
            isOpen: true, // Always keep sidebar open
            isCollapsed: !sidebar.isCollapsed
          }
        }
      };
    }),

  closeAll: (except) =>
    set((state) => {
      const updatedPanels = { ...state.panels };

      Object.keys(updatedPanels).forEach((key) => {
        if (key !== except) {
          updatedPanels[key] = {
            ...updatedPanels[key],
            isOpen: false,
          };
        }
      });

      return { panels: updatedPanels };
    }),

  getPanelState: (id) => {
    return get().panels[id];
  },

  setSize: (id, size) =>
    set((state) => {
      const panel = state.panels[id];
      if (!panel) return state;

      return {
        panels: {
          ...state.panels,
          [id]: {
            ...panel,
            size,
          },
        },
      };
    }),

  registerPanel: (panel) => {
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
            },
          },
        };
      }

      
      const newPanel = {
        ...panel,
        isOpen: panel.id === 'sidebar' ? true : (savedState.isOpen !== undefined ? savedState.isOpen : panel.isOpen),
      };

      return {
        panels: {
          ...state.panels,
          [panel.id]: newPanel,
        },
      };
    });
  },
  
  saveState: () => {
    try {
      const { panels } = get();
      localStorage.setItem('panel-layout', JSON.stringify(panels));
      return true;
    } catch (error) {
      console.error('Failed to save panel layout:', error);
      return false;
    }
  }
}));