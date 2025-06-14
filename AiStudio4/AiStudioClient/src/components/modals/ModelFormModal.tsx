// AiStudioClient/src/components/modals/ModelFormModal.tsx
import React from 'react';
import {
  UnifiedModalDialog,
  UnifiedModalHeader,
  UnifiedModalContent,
} from '@/components/ui/unified-modal-dialog';
import { useModalStore, ModalRegistry } from '@/stores/useModalStore';
import { ModelForm } from '../settings/ModelForm';

type ModelFormProps = ModalRegistry['modelForm'];

export function ModelFormModal() {
  const { currentModal, closeModal } = useModalStore();
  const isOpen = currentModal?.id === 'modelForm';
  
  if (!isOpen || !currentModal) return null;
  
  const props = currentModal.props;
  const { mode, model, providers, onSubmit } = props;

  const title = mode === 'add' ? 'Add New Model' : 'Edit Model';

  return (
    <UnifiedModalDialog
      open={isOpen}
      onOpenChange={(open) => !open && closeModal()}
      size="xl"
      variant="settings"
    >
      <UnifiedModalHeader>
        <h2 className="text-gray-100 text-xl font-semibold">{title}</h2>
      </UnifiedModalHeader>
      <UnifiedModalContent>
        <ModelForm
          providers={providers}
          onSubmit={onSubmit}
          isProcessing={false}
          initialValues={model}
        />
      </UnifiedModalContent>
    </UnifiedModalDialog>
  );
}