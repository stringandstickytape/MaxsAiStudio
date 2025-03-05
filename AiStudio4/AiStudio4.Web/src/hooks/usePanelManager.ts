// src/hooks/usePanelManager.ts
import { usePanelStore } from '@/stores/usePanelStore';
import { PanelState } from '@/types/ui';
import React from 'react';

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
  } = usePanelStore();

  const { id, defaultOpen = false, defaultPinned = false, ...rest } = panelConfig;
  
  // Register this panel with the store if it doesn't exist yet
  React.useEffect(() => {
    const existingPanel = getPanelState(id);
    
    if (!existingPanel) {
      registerPanel({
        id,
        isOpen: defaultOpen,
        isPinned: defaultPinned,
        ...rest
      });
    }
  }, [id, registerPanel, getPanelState, defaultOpen, defaultPinned]);

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