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

            const updatedPanels = { ...state.panels };

            // If we're closing the panel, also ensure it's not pinned
            const isClosing = panel.isOpen;

            updatedPanels[id] = {
                ...panel,
                isOpen: !panel.isOpen,
                // If we're closing, also unpin the panel
                isPinned: isClosing ? false : panel.isPinned
            };

            // When closing one panel, close other unpinned panels in the same position
            if (!panel.isOpen && !panel.isPinned) {
                Object.keys(updatedPanels).forEach((key) => {
                    if (key !== id && updatedPanels[key].position === panel.position && !updatedPanels[key].isPinned) {
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

    togglePinned: (id) => {
        set((state) => {
            const panel = state.panels[id];
            if (!panel) {
                return state;
            }

            // When toggling pin status, ensure the panel stays visible
            const newIsPinned = !panel.isPinned;

            // If we're pinning, make sure the panel is open
            // If we're unpinning, don't change isOpen state initially
            return {
                panels: {
                    ...state.panels,
                    [id]: {
                        ...panel,
                        isPinned: newIsPinned,
                        isOpen: newIsPinned ? true : panel.isOpen,
                    },
                },
            };
        });

        // Save panel layout after pinning change for persistence
        try {
            const { panels } = usePanelStore.getState();
            localStorage.setItem('panel-layout', JSON.stringify(panels));
        } catch (error) {
            console.error('Failed to save panel layout after pinning change:', error);
        }
  },

  closeAll: (except) =>
    set((state) => {
      const updatedPanels = { ...state.panels };

      Object.keys(updatedPanels).forEach((key) => {
        if (key !== except && !updatedPanels[key].isPinned) {
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
                          isPinned: existingPanel.isPinned,
                      },
                  },
              };
          }

          // For new panels, apply saved state if available
          const newPanel = {
              ...panel,
              ...(savedState.isOpen !== undefined ? { isOpen: savedState.isOpen } : {}),
              ...(savedState.isPinned !== undefined ? { isPinned: savedState.isPinned } : {}),
          };
      
      console.log(`Registering new panel ${panel.id} with state:`, {
        isOpen: newPanel.isOpen,
        isPinned: newPanel.isPinned
      });

      return {
        panels: {
          ...state.panels,
          [panel.id]: newPanel,
        },
      };
    });
  },
  
}));