// AiStudio4.Web/src/stores/useModalStore.ts
import { create } from 'zustand';

// Define all possible modal types and their props
export interface ModalRegistry {
  settings: {};
  systemPrompt: { convId?: string; editPromptId?: string; createNew?: boolean };
  userPrompt: {};
  tool: {}; // Placeholder for tool modal props
  theme: {};
  confirmation: {
    title: string;
    description: string;
    onConfirm: () => void;
    onCancel?: () => void;
    confirmLabel?: string;
    cancelLabel?: string;
    danger?: boolean;
  };
  form: {
    title: string;
    description?: string;
    onSubmit: (data: any) => void;
    onCancel?: () => void;
    initialData?: any;
    submitLabel?: string;
    cancelLabel?: string;
    children?: React.ReactNode; // Added to support passing form fields
  };
  // Add other modal types here
}

export type ModalId = keyof ModalRegistry;

type ModalState = {
  openModalId: ModalId | null;
  modalProps: any; // Will be typed based on the modal ID
  modalStack: { id: ModalId; props: any }[]; // For nested modals
  openModal: <T extends ModalId>(id: T, props: ModalRegistry[T]) => void;
  closeModal: () => void;
  closeAllModals: () => void;
  openNestedModal: <T extends ModalId>(id: T, props: ModalRegistry[T]) => void;
  closeNestedModal: () => void;
};

export const useModalStore = create<ModalState>((set, get) => ({
  openModalId: null,
  modalProps: {},
  modalStack: [],

  openModal: (id, props) => set({ openModalId: id, modalProps: props, modalStack: [] }), // Reset stack when opening a base modal

  closeModal: () => {
    const { modalStack } = get();
    if (modalStack.length > 0) {
      // If closing a nested modal, revert to the previous one
      const lastModal = modalStack[modalStack.length - 1];
      set({
        openModalId: lastModal.id,
        modalProps: lastModal.props,
        modalStack: modalStack.slice(0, -1),
      });
    } else {
      // If closing the base modal, clear everything
      set({ openModalId: null, modalProps: {} });
    }
  },

  closeAllModals: () => set({ openModalId: null, modalProps: {}, modalStack: [] }),

  openNestedModal: (id, props) => {
    const { openModalId, modalProps } = get();
    if (openModalId) {
      set((state) => ({
        modalStack: [...state.modalStack, { id: openModalId, props: modalProps }],
        openModalId: id,
        modalProps: props,
      }));
    } else {
      // If no modal is open, just open this as a base modal
      set({ openModalId: id, modalProps: props, modalStack: [] });
    }
  },

  closeNestedModal: () => {
    // This logic is now handled by closeModal
    get().closeModal();
  },
}));