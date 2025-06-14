// AiStudioClient/src/stores/useModalStore.ts
import { create } from 'zustand';

// Define all possible modal types and their props
export interface ModalRegistry {
  // Removed settings modal and added individual settings modals
  models: {};
  providers: {};
  appearance: {};
  systemPrompt: { convId?: string; editPromptId?: string; createNew?: boolean };
  userPrompt: { editPromptId?: string; createNew?: boolean };
  tool: {}; // Tool library modal props
  toolEditor: { // New tool editor modal props
    tool: any | null;
    categories: any[];
    onClose: () => void;
  };
  server: {}; // Server management modal props
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

// Type-safe modal data that preserves the relationship between ID and props
type ModalStateData = {
  [K in ModalId]: { id: K; props: ModalRegistry[K] }
}[ModalId];

type ModalState = {
  currentModal: ModalStateData | null;
  modalStack: ModalStateData[]; // For nested modals
  // Backward compatibility properties - maintained in sync with currentModal
  openModalId: ModalId | null;
  modalProps: any;
  openModal: <T extends ModalId>(id: T, props: ModalRegistry[T]) => void;
  closeModal: () => void;
  closeAllModals: () => void;
  openNestedModal: <T extends ModalId>(id: T, props: ModalRegistry[T]) => void;
  closeNestedModal: () => void;
};

export const useModalStore = create<ModalState>((set, get) => ({
  currentModal: null,
  modalStack: [],
  openModalId: null,
  modalProps: {},

  openModal: (id, props) => {
    const modalData = { id, props } as ModalStateData;
    set({ 
      currentModal: modalData, 
      openModalId: id,
      modalProps: props,
      modalStack: [] 
    });
  },

  closeModal: () => {
    const { modalStack } = get();
    if (modalStack.length > 0) {
      // If closing a nested modal, revert to the previous one
      const lastModal = modalStack[modalStack.length - 1];
      set({
        currentModal: lastModal,
        openModalId: lastModal.id,
        modalProps: lastModal.props,
        modalStack: modalStack.slice(0, -1),
      });
    } else {
      // If closing the base modal, clear everything
      set({ 
        currentModal: null,
        openModalId: null,
        modalProps: {}
      });
    }
  },

  closeAllModals: () => set({ 
    currentModal: null, 
    openModalId: null,
    modalProps: {},
    modalStack: [] 
  }),

  openNestedModal: (id, props) => {
    const { currentModal } = get();
    const modalData = { id, props } as ModalStateData;
    if (currentModal) {
      set((state) => ({
        modalStack: [...state.modalStack, currentModal],
        currentModal: modalData,
        openModalId: id,
        modalProps: props,
      }));
    } else {
      // If no modal is open, just open this as a base modal
      set({ 
        currentModal: modalData, 
        openModalId: id,
        modalProps: props,
        modalStack: [] 
      });
    }
  },

  closeNestedModal: () => {
    // This logic is now handled by closeModal
    get().closeModal();
  },
}));