// AiStudioClient/src/components/modals/ConfirmationModal.tsx
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
  const { currentModal, closeModal } = useModalStore();
  const isOpen = currentModal?.id === 'confirmation';
  
  if (!isOpen || !currentModal) return null;
  
  // TypeScript now knows currentModal.props is ConfirmationProps
  const props = currentModal.props;

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
    onCancel?.();
    closeModal(); // Explicitly close the modal
  };

  const handleConfirm = () => {
    onConfirm();
    closeModal(); // Explicitly close the modal
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