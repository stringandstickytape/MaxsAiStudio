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

export interface UIState {
  isDarkMode: boolean;
  sidebarWidth: string;
  sidebarCollapsed: boolean;
  menuItems: MenuItem[];
}

export interface MenuItem {
  id: string;
  label: string;
  icon?: string;
  route?: string;
  action?: () => void;
  children?: MenuItem[];
}