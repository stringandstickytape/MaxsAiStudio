// AiStudio4.Web/src/components/modals/ToolModal.tsx
import React from 'react';
import {
  UnifiedModalDialog,
  UnifiedModalHeader,
  UnifiedModalContent,
} from '@/components/ui/unified-modal-dialog';
import { useModalStore, ModalRegistry } from '@/stores/useModalStore';
import { ToolPanel } from '../tools/ToolPanel';

type ToolModalProps = ModalRegistry['tool'];

export function ToolModal() {
  const { openModalId, modalProps, closeModal } = useModalStore();
  const isOpen = openModalId === 'tool';
  const props = isOpen ? (modalProps as ToolModalProps) : null;

  if (!isOpen) return null;

  return (
    <UnifiedModalDialog
      open={isOpen}
      onOpenChange={(open) => !open && closeModal()}
      variant="library" // Use library variant as a base
      size="4xl" // Corresponds to max-w-4xl
      height="xl" // Corresponds roughly to 80vh
      className="p-0" // Remove default padding as ToolPanel handles it
    >
      <UnifiedModalHeader>
        <h2 className="text-xl font-semibold">Tools</h2>
      </UnifiedModalHeader>
      <UnifiedModalContent className="p-0">
        <ToolPanel 
          isOpen={isOpen}
          onClose={() => closeModal()}
          isModal={true}
        />
      </UnifiedModalContent>
    </UnifiedModalDialog>
  );
}