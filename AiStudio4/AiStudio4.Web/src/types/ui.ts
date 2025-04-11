export type PanelPosition = 'left' | 'right' | 'top' | 'bottom';

export interface PanelState {
  id: string;
  isOpen: boolean;
  position: PanelPosition;
  size: string;
  zIndex: number;
  title: string;
}
export type ModalId = 'settings' | 'systemPrompt' | 'userPrompt' | 'tool' | 'theme' | string;