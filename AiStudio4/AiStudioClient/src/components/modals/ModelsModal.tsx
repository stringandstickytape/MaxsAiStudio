// AiStudioClient/src/components/modals/ModelsModal.tsx
import React, { useState, useEffect } from 'react';
import { useModalStore } from '@/stores/useModalStore';
import {
  UnifiedModalDialog,
  UnifiedModalHeader,
  UnifiedModalContent,
} from '@/components/ui/unified-modal-dialog';
import { ModelManagement } from '@/components/settings/ModelManagement';
import { useModelManagement } from '@/hooks/useResourceManagement';
import { Model } from '@/types/settings';

export function ModelsModal() {
  const { currentModal, closeModal } = useModalStore();
  const isOpen = currentModal?.id === 'models';
  
  const {
    models,
    providers,
    isLoading,
    error,
    fetchModels,
    fetchProviders,
  } = useModelManagement();
  
  const [modelToEdit, setModelToEdit] = useState<Model | null>(null);
  const [editDialogOpen, setEditDialogOpen] = useState(false);

  // Handle edit model from props
  useEffect(() => {
    if (isOpen && currentModal?.props?.editModelId) {
      const model = models.find((m) => m.guid === currentModal.props.editModelId);
      if (model) {
        setModelToEdit(model);
        setEditDialogOpen(true);
      }
    }
  }, [isOpen, currentModal?.props?.editModelId, models]);

  // Fetch data when modal opens
  useEffect(() => {
    if (isOpen) {
      fetchModels();
      fetchProviders();
    }
  }, [isOpen, fetchModels, fetchProviders]);
  
  if (!isOpen || !currentModal) return null;
  
  // Get props from modal store
  const props = currentModal.props || {};

  return (
    <UnifiedModalDialog
      open={isOpen}
      onOpenChange={(open) => !open && closeModal()}
      variant="settings"
      size="xl"
    >
      <UnifiedModalHeader>
        <h2 className="text-xl font-semibold">Models</h2>
      </UnifiedModalHeader>
      <UnifiedModalContent>
        {isLoading ? (
          <div className="flex-center py-8">
            <div className="loading-spinner h-8 w-8"></div>
          </div>
        ) : (
          <ModelManagement 
            providers={providers}
            modelToEdit={modelToEdit}
            setModelToEdit={setModelToEdit}
            editDialogOpen={editDialogOpen}
            setEditDialogOpen={setEditDialogOpen}
          />
        )}
      </UnifiedModalContent>
    </UnifiedModalDialog>
  );
}