// src/types/ui.ts
export type PanelPosition = 'left' | 'right' | 'top' | 'bottom';

export interface PanelState {
  id: string;
  isOpen: boolean;
  isPinned: boolean;
  position: PanelPosition;
  size: string;
  zIndex: number;
  title: string;
}

export interface PanelContextType {
  panels: Record<string, PanelState>;
  togglePanel: (id: string) => void;
  togglePinned: (id: string) => void;
  closeAll: (except?: string) => void;
  getPanelState: (id: string) => PanelState | undefined;
  setSize: (id: string, size: string) => void;
  registerPanel: (panel: PanelState) => void;
}
