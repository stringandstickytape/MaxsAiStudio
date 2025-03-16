
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
