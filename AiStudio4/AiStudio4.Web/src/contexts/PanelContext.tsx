// src/contexts/PanelContext.tsx
import React, { createContext, useContext, useState, ReactNode } from 'react';
import { PanelState, PanelContextType } from '@/types/ui';

// Create the panel context with default values
const PanelContext = createContext<PanelContextType>({
  panels: {},
  togglePanel: () => {},
  togglePinned: () => {},
  closeAll: () => {},
  getPanelState: () => undefined,
  setSize: () => {},
  registerPanel: () => {}
});

// Hook for using the panel context
export const usePanels = () => useContext(PanelContext);

interface PanelProviderProps {
  children: ReactNode;
}

// Panel provider component
export const PanelProvider: React.FC<PanelProviderProps> = ({ children }) => {
  const [panels, setPanels] = useState<Record<string, PanelState>>({});

  // Register a new panel
  const registerPanel = (panel: PanelState) => {
    setPanels(prev => {
      // Only update if the panel doesn't exist or has changed
      if (!prev[panel.id] || 
          JSON.stringify(prev[panel.id]) !== JSON.stringify(panel)) {
        return {
          ...prev,
          [panel.id]: panel
        };
      }
      return prev;
    });
  };

  // Toggle a panel open/closed
  const togglePanel = (id: string) => {
    setPanels(prev => {
      const panel = prev[id];
      if (!panel) return prev;

      // If opening this panel, close all other non-pinned panels
      if (!panel.isOpen) {
        const updatedPanels = { ...prev };
        
        // Close other panels on the same side that aren't pinned
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
        
        // Update the target panel
        updatedPanels[id] = {
          ...panel,
          isOpen: !panel.isOpen
        };
        
        return updatedPanels;
      } else {
        // Just toggle this panel
        return {
          ...prev,
          [id]: {
            ...panel,
            isOpen: !panel.isOpen
          }
        };
      }
    });
  };

  // Toggle a panel's pinned state
  const togglePinned = (id: string) => {
    setPanels(prev => {
      const panel = prev[id];
      if (!panel) return prev;
      
      return {
        ...prev,
        [id]: {
          ...panel,
          isPinned: !panel.isPinned
        }
      };
    });
  };

  // Close all panels except the one specified
  const closeAll = (except?: string) => {
    setPanels(prev => {
      const updatedPanels = { ...prev };
      
      Object.keys(updatedPanels).forEach(key => {
        if (key !== except && !updatedPanels[key].isPinned) {
          updatedPanels[key] = {
            ...updatedPanels[key],
            isOpen: false
          };
        }
      });
      
      return updatedPanels;
    });
  };

  // Get a panel's current state
  const getPanelState = (id: string): PanelState | undefined => {
    return panels[id];
  };

  // Set a panel's size
  const setSize = (id: string, size: string) => {
    setPanels(prev => {
      const panel = prev[id];
      if (!panel) return prev;
      
      return {
        ...prev,
        [id]: {
          ...panel,
          size
        }
      };
    });
  };

  const contextValue: PanelContextType = {
    panels,
    togglePanel,
    togglePinned,
    closeAll,
    getPanelState,
    setSize,
    registerPanel
  };

  return (
    <PanelContext.Provider value={contextValue}>
      {children}
    </PanelContext.Provider>
  );
};
