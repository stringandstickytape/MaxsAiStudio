// AiStudio4.Web/src/components/modals/ModelsModal.tsx
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
import { commandEvents } from '@/commands/settingsCommands';
import { windowEventService, WindowEvents } from '@/services/windowEvents';

export function ModelsModal() {
  const { openModalId, closeModal, modalProps } = useModalStore();
  const isOpen = openModalId === 'models';
  
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

  // Listen for edit-model events
  useEffect(() => {
    const handleEditModel = (modelGuid: string) => {
      const model = models.find((m) => m.guid === modelGuid);
      if (model) {
        setModelToEdit(model);
        setEditDialogOpen(true);
      }
    };

    const unsubscribe = windowEventService.on(WindowEvents.COMMAND_EDIT_MODEL, handleEditModel);
    return () => unsubscribe();
  }, [models]);

  // Fetch data when modal opens
  useEffect(() => {
    if (isOpen) {
      fetchModels();
      fetchProviders();
    }
  }, [isOpen, fetchModels, fetchProviders]);

  if (!isOpen) return null;

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