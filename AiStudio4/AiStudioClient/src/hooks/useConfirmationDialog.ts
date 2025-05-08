// AiStudioClient/src/hooks/useConfirmationDialog.ts
import { useModalStore, ModalRegistry } from '@/stores/useModalStore';

type ConfirmationOptions = Omit<ModalRegistry['confirmation'], 'onConfirm' | 'onCancel'>;

export function useConfirmationDialog() {
  const { openModal, openNestedModal, closeModal } = useModalStore();

  const confirm = (options: ConfirmationOptions) => {
    return new Promise<boolean>((resolve) => {
      const modalProps: ModalRegistry['confirmation'] = {
        ...options,
        onConfirm: () => {
          // closeModal(); // Modal closes itself via onOpenChange or button click
          resolve(true);
        },
        onCancel: () => {
          // closeModal(); // Modal closes itself via onOpenChange or button click
          resolve(false);
        },
      };
      openModal('confirmation', modalProps);
    });
  };

  const confirmNested = (options: ConfirmationOptions) => {
    return new Promise<boolean>((resolve) => {
      const modalProps: ModalRegistry['confirmation'] = {
        ...options,
        onConfirm: () => {
          // closeModal(); // Let the modal handle its closing
          resolve(true);
        },
        onCancel: () => {
          // closeModal(); // Let the modal handle its closing
          resolve(false);
        },
      };
      openNestedModal('confirmation', modalProps);
    });
  };

  return { confirm, confirmNested };
}