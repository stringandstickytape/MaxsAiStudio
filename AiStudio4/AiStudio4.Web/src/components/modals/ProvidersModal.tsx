// AiStudio4.Web/src/components/modals/ProvidersModal.tsx
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
import { windowEventService, WindowEvents } from '@/services/windowEvents';

export function ProvidersModal() {
  const { openModalId, closeModal, modalProps } = useModalStore();
  const isOpen = openModalId === 'providers';
  
  const {
    providers,
    isLoading,
    error,
    fetchProviders,
  } = useModelManagement();

  const [providerToEdit, setProviderToEdit] = useState<ServiceProvider | null>(null);
  const [editDialogOpen, setEditDialogOpen] = useState(false);

  // Listen for edit-provider events
  useEffect(() => {
    const handleEditProvider = (providerGuid: string) => {
      const provider = providers.find((p) => p.guid === providerGuid);
      if (provider) {
        setProviderToEdit(provider);
        setEditDialogOpen(true);
      }
    };

    const unsubscribe = windowEventService.on(WindowEvents.COMMAND_EDIT_PROVIDER, handleEditProvider);
    return () => unsubscribe();
  }, [providers]);

  // Fetch data when modal opens
  useEffect(() => {
    if (isOpen) {
      fetchProviders();
    }
  }, [isOpen, fetchProviders]);

  if (!isOpen) return null;

  return (
    <UnifiedModalDialog
      open={isOpen}
      onOpenChange={(open) => !open && closeModal()}
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