// AiStudioClient/src/components/modals/ProviderFormModal.tsx
import React from 'react';
import {
  UnifiedModalDialog,
  UnifiedModalHeader,
  UnifiedModalContent,
} from '@/components/ui/unified-modal-dialog';
import { useModalStore, ModalRegistry } from '@/stores/useModalStore';
import { ServiceProviderForm } from '../settings/ServiceProviderForm';

type ProviderFormProps = ModalRegistry['providerForm'];

export function ProviderFormModal() {
  const { currentModal, closeModal } = useModalStore();
  const isOpen = currentModal?.id === 'providerForm';
  
  if (!isOpen || !currentModal) return null;
  
  const props = currentModal.props;
  const { mode, provider, onSubmit } = props;

  const title = mode === 'add' ? 'Add New Provider' : 'Edit Provider';

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
        <ServiceProviderForm
          onSubmit={onSubmit}
          isProcessing={false}
          initialValues={provider}
        />
      </UnifiedModalContent>
    </UnifiedModalDialog>
  );
}