// AiStudio4.Web/src/components/modals/ConfirmationModal.tsx
import React from 'react';
import {
  UnifiedModalDialog,
  UnifiedModalHeader,
  UnifiedModalContent,
  UnifiedModalFooter,
} from '@/components/ui/unified-modal-dialog';
import { Button } from '@/components/ui/button';
import { useModalStore, ModalRegistry } from '@/stores/useModalStore';

type ConfirmationProps = ModalRegistry['confirmation'];

export function ConfirmationModal() {
  const { openModalId, modalProps, closeModal } = useModalStore();
  const isOpen = openModalId === 'confirmation';
  // Ensure modalProps is correctly typed when the modal is open
  const props = isOpen ? (modalProps as ConfirmationProps) : null;

  if (!isOpen || !props) return null;

  const {
    title,
    description,
    onConfirm,
    onCancel,
    confirmLabel = 'Confirm',
    cancelLabel = 'Cancel',
    danger = false,
  } = props;

  const handleCancel = () => {
    // closeModal(); // Let onOpenChange handle closing
    onCancel?.();
  };

  const handleConfirm = () => {
    // closeModal(); // Let onOpenChange handle closing
    onConfirm();
  };

  return (
    <UnifiedModalDialog
      open={isOpen}
      // Use closeModal which handles nested logic correctly
      onOpenChange={(open) => !open && closeModal()}
      variant="confirmation"
    >
      <UnifiedModalHeader>
        <h2 className="text-xl font-semibold">{title}</h2>
      </UnifiedModalHeader>

      <UnifiedModalContent>
        <p className="text-muted-foreground">{description}</p>
      </UnifiedModalContent>

      <UnifiedModalFooter>
        <Button variant="outline" onClick={handleCancel}>
          {cancelLabel}
        </Button>
        <Button
          variant={danger ? 'destructive' : 'default'}
          onClick={handleConfirm}
        >
          {confirmLabel}
        </Button>
      </UnifiedModalFooter>
    </UnifiedModalDialog>
  );
}