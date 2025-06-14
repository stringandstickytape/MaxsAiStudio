// AiStudioClient/src/components/modals/ProvidersModal.tsx
import React, { useState, useEffect } from 'react';
import { useModalStore } from '@/stores/useModalStore';
import {
  UnifiedModalDialog,
  UnifiedModalHeader,
  UnifiedModalContent,
} from '@/components/ui/unified-modal-dialog';
import { ServiceProviderManagement } from '@/components/settings/ServiceProviderManagement';
import { useModelManagement } from '@/hooks/useResourceManagement';
import { ServiceProvider } from '@/types/settings';

export function ProvidersModal() {
  const { currentModal, modalStack, closeModal } = useModalStore();
  const isCurrentModal = currentModal?.id === 'providers';
  const isInStack = modalStack.some(modal => modal.id === 'providers');
  const isOpen = isCurrentModal || isInStack;
  
  // Get the modal data from either current or stack
  const modalData = isCurrentModal ? currentModal : modalStack.find(modal => modal.id === 'providers');
  
  const {
    providers,
    isLoading,
    error,
    fetchProviders,
  } = useModelManagement();
  
  const [providerToEdit, setProviderToEdit] = useState<ServiceProvider | null>(null);
  const [editDialogOpen, setEditDialogOpen] = useState(false);

  // Handle edit provider from props
  useEffect(() => {
    if (isOpen && currentModal?.props?.editProviderId) {
      const provider = providers.find((p) => p.guid === currentModal.props.editProviderId);
      if (provider) {
        setProviderToEdit(provider);
        setEditDialogOpen(true);
      }
    }
  }, [isOpen, currentModal?.props?.editProviderId, providers]);

  // Fetch data when modal opens
  useEffect(() => {
    if (isOpen) {
      fetchProviders();
    }
  }, [isOpen, fetchProviders]);
  
  if (!isOpen || !modalData) return null;
  
  // Get props from modal store
  const props = modalData.props || {};

  return (
    <UnifiedModalDialog
      open={isCurrentModal}
      onOpenChange={(open) => {
        if (!open && isCurrentModal) {
          closeModal();
        }
      }}
      variant="settings"
      size="xl"
    >
      <UnifiedModalHeader>
        <h2 className="text-xl font-semibold">Service Providers</h2>
      </UnifiedModalHeader>
      <UnifiedModalContent>
        {isLoading ? (
          <div className="flex-center py-8">
            <div className="loading-spinner h-8 w-8"></div>
          </div>
        ) : (
          <ServiceProviderManagement 
            providers={providers}
            providerToEdit={providerToEdit}
            setProviderToEdit={setProviderToEdit}
            editDialogOpen={editDialogOpen}
            setEditDialogOpen={setEditDialogOpen}
          />
        )}
      </UnifiedModalContent>
    </UnifiedModalDialog>
  );
}