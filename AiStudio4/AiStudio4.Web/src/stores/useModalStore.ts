import { create } from 'zustand';

export type ModalId = 'settings' | 'systemPrompt' | 'userPrompt' | 'tool' | string; // Added string for potential future flexibility

interface ModalState {
  openModalId: ModalId | null;
  modalProps: Record<string, any>; // Props to pass to the specific modal
  openModal: (id: ModalId, props?: Record<string, any>) => void;
  closeModal: () => void;
}

export const useModalStore = create<ModalState>((set) => ({
  openModalId: null,
  modalProps: {},
  openModal: (id, props = {}) => set({ openModalId: id, modalProps: props }),
  closeModal: () => set({ openModalId: null, modalProps: {} }),
}));
