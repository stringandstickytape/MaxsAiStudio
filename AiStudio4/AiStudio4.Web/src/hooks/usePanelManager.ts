// src/hooks/usePanelManager.ts
import { useEffect } from 'react';
import { usePanels } from '@/contexts/PanelContext';
import { PanelState } from '@/types/ui';

// Hook for managing a specific panel
export function usePanelManager(panelConfig: Omit<PanelState, 'isOpen' | 'isPinned'> & {
  defaultOpen?: boolean;
  defaultPinned?: boolean;
}) {
  const { 
    panels, 
    togglePanel, 
    togglePinned, 
    getPanelState,
    registerPanel 
  } = usePanels();

  const { id, defaultOpen = false, defaultPinned = false, ...rest } = panelConfig;
  
  // Register this panel with the context
  useEffect(() => {
    const existingPanel = getPanelState(id);
    
    registerPanel({
      id,
      isOpen: existingPanel ? existingPanel.isOpen : defaultOpen,
      isPinned: existingPanel ? existingPanel.isPinned : defaultPinned,
      ...rest
    });
  }, [id]);

  const panelState = getPanelState(id);

  const open = () => {
    if (panelState && !panelState.isOpen) {
      togglePanel(id);
    }
  };

  const close = () => {
    if (panelState && panelState.isOpen) {
      togglePanel(id);
    }
  };

  const toggle = () => {
    togglePanel(id);
  };

  const pin = () => {
    if (panelState && !panelState.isPinned) {
      togglePinned(id);
    }
  };

  const unpin = () => {
    if (panelState && panelState.isPinned) {
      togglePinned(id);
    }
  };

  const togglePin = () => {
    togglePinned(id);
  };

  return {
    isOpen: panelState?.isOpen || false,
    isPinned: panelState?.isPinned || false,
    open,
    close,
    toggle,
    pin,
    unpin,
    togglePin
  };
}
