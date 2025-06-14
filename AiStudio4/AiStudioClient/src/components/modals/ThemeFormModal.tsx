// AiStudioClient/src/components/modals/ThemeFormModal.tsx
import React from 'react';
import {
  UnifiedModalDialog,
  UnifiedModalHeader,
  UnifiedModalContent,
} from '@/components/ui/unified-modal-dialog';
import { useModalStore, ModalRegistry } from '@/stores/useModalStore';
import { ThemeForm } from '../settings/ThemeForm';

type ThemeFormProps = ModalRegistry['themeForm'];

export function ThemeFormModal() {
  const { currentModal, closeModal } = useModalStore();
  const isOpen = currentModal?.id === 'themeForm';
  
  if (!isOpen || !currentModal) return null;
  
  const props = currentModal.props;
  const { mode, theme, onSubmit } = props;

  const title = mode === 'add' ? 'Add New Theme' : 'Edit Theme';

  return (
    <UnifiedModalDialog
      open={isOpen}
      onOpenChange={(open) => !open && closeModal()}
      size="xl"
      variant="form"
      style={{ backgroundColor: 'var(--global-background-color)' }}
    >
      <UnifiedModalHeader style={{ backgroundColor: 'var(--global-background-color)' }}>
        <h2 className="text-lg font-semibold" style={{ color: 'var(--global-text-color)' }}>{title}</h2>
      </UnifiedModalHeader>
      <UnifiedModalContent style={{ backgroundColor: 'var(--global-background-color)' }}>
        <ThemeForm
          onSubmit={onSubmit}
          isProcessing={false}
          initialValues={theme}
        />
      </UnifiedModalContent>
    </UnifiedModalDialog>
  );
}