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
  debugPanelState: () => void;
  saveState: () => boolean;
}

// Add debugging helper function
const logPanelStateInfo = (message: string, panels: Record<string, PanelState>, storageData?: any) => {
  console.group(`%c📋 Panel State Debug: ${message}`, 'color: #4ade80; font-weight: bold;');
  
  // Create a formatted version of panel state for logging
  const formattedPanels = Object.entries(panels).map(([id, panel]) => ({
    id,
    isOpen: panel.isOpen,
    isPinned: panel.isPinned,
    position: panel.position
  }));
  
  console.log('Current panel state:', formattedPanels);
  
  if (storageData) {
    console.log('Storage data:', storageData);
    
    // Compare current state with storage
    const differences = Object.keys(panels).filter(id => {
      if (!storageData[id]) return false;
      return panels[id].isOpen !== storageData[id].isOpen || 
             panels[id].isPinned !== storageData[id].isPinned;
    });
    
    if (differences.length > 0) {
      console.warn('Differences between current state and storage:', 
        differences.map(id => ({
          id,
          current: { isOpen: panels[id].isOpen, isPinned: panels[id].isPinned },
          storage: { isOpen: storageData[id].isOpen, isPinned: storageData[id].isPinned }
        }))
      );
    }
  }
  
  console.groupEnd();
};

export const usePanelStore = create<PanelStore>((set, get) => ({
  panels: {},

    togglePanel: (id) => {
        set((state) => {
            const panel = state.panels[id];
            if (!panel) {
                console.warn(`Panel with id ${id} not found in togglePanel`);
                return state;
            }

            console.log(`%c📋 Panel Operation: ${panel.isOpen ? 'Closing' : 'Opening'} panel ${id}`,
                'color: #60a5fa; font-weight: bold');

            const updatedPanels = { ...state.panels };

            // If we're closing the panel, also ensure it's not pinned
            const isClosing = panel.isOpen;

            updatedPanels[id] = {
                ...panel,
                isOpen: !panel.isOpen,
                // If we're closing, also unpin the panel
                isPinned: isClosing ? false : panel.isPinned
            };

            console.log(`Panel ${id} new state:`, {
                isOpen: updatedPanels[id].isOpen,
                isPinned: updatedPanels[id].isPinned
            });

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
                console.warn(`togglePinned: Panel with id ${id} not found`);
                return state;
            }

            // When toggling pin status, ensure the panel stays visible
            const newIsPinned = !panel.isPinned;

            console.log(`%c📋 Panel Operation: ${newIsPinned ? 'Pinning' : 'Unpinning'} panel ${id}`,
                'color: #f59e0b; font-weight: bold');

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
            const panelToSave = panels[id];
            console.log(`Saving panel ${id} state to localStorage:`, {
                isOpen: panelToSave.isOpen,
                isPinned: panelToSave.isPinned
            });
            localStorage.setItem('panel-layout', JSON.stringify(panels));

            // Log the full storage state after saving
            logPanelStateInfo('After saving panel layout', panels, panels);
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
    console.log(`Registering panel: ${panel.id}`);
    
    let savedState: Partial<PanelState> = {};
    try {
      const savedLayout = localStorage.getItem('panel-layout');
      if (savedLayout) {
        const parsedLayout = JSON.parse(savedLayout);
        console.log(`Found saved layout in localStorage for panel ${panel.id}:`, 
          parsedLayout[panel.id] || 'No saved state for this panel');
        
        if (parsedLayout[panel.id]) {
          savedState = parsedLayout[panel.id];
          console.log(`Loaded saved state for panel ${panel.id}:`, { 
            isOpen: savedState.isOpen, 
            isPinned: savedState.isPinned 
          });
        }
      } else {
        console.log('No saved panel layout found in localStorage');
      }
    } catch (e) {
      console.warn('Failed to parse saved panel state:', e);
    }

    return set((state) => {
      const existingPanel = state.panels[panel.id];
      if (existingPanel) {
        console.log(`Panel ${panel.id} already exists, preserving state:`, {
          isOpen: existingPanel.isOpen,
          isPinned: existingPanel.isPinned
        });
        
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
  
  // Debug helper to expose panel state
  debugPanelState: () => {
    const { panels } = get();
    
    try {
      const savedLayout = localStorage.getItem('panel-layout');
      const storageData = savedLayout ? JSON.parse(savedLayout) : null;
      
      logPanelStateInfo('Current state vs localStorage', panels, storageData);
      
      // Create a global debug object for inspecting in browser console
      (window as any).PanelStateDebug = {
        currentState: panels,
        localStorage: storageData,
        inspect: () => logPanelStateInfo('Manual inspection', panels, storageData)
      };
      
      console.log("%c📋 Panel Debug Enabled", "color: #10b981; font-weight: bold;");
      console.log("Access window.PanelStateDebug in console to inspect panel state");
      console.log("Call window.PanelStateDebug.inspect() for a fresh comparison");
    } catch (e) {
      console.error('Error in debugPanelState:', e);
    }
  },
}));