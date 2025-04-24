// AiStudio4.Web/src/components/modals/ModalManager.tsx

import React from 'react';
import { useModalStore, ModalId } from '@/stores/useModalStore';
import {
  UnifiedModalDialog,
  UnifiedModalHeader,
  UnifiedModalContent,
  UnifiedModalFooter,
} from '@/components/ui/unified-modal-dialog';

// Import existing content components or create placeholders
import { SettingsPanel } from '@/components/SettingsPanel';
import { SystemPromptLibrary } from '@/components/SystemPrompt/SystemPromptLibrary';
import { UserPromptLibrary } from '@/components/UserPrompt/UserPromptLibrary';
import { ThemeManagement } from '@/components/settings/ThemeManagement';
import { Button } from '@/components/ui/button';

// Example placeholder for confirmation dialog content
interface ConfirmationContentProps {
  title: string;
  description: string;
  onConfirm: () => void;
  onCancel: () => void;
  confirmLabel?: string;
  cancelLabel?: string;
  danger?: boolean;
}
const ConfirmationContent: React.FC<ConfirmationContentProps> = ({
  title,
  description,
  onConfirm,
  onCancel,
  confirmLabel = 'Confirm',
  cancelLabel = 'Cancel',
  danger = false,
}) => {
  return (
    <>
      <UnifiedModalHeader>
        <h2 className="text-xl font-semibold">{title}</h2>
      </UnifiedModalHeader>
      <UnifiedModalContent>
        <p className="text-muted-foreground">{description}</p>
      </UnifiedModalContent>
      <UnifiedModalFooter>
        <Button variant="outline" onClick={onCancel}>
          {cancelLabel}
        </Button>
        <Button
          variant={danger ? 'destructive' : 'default'}
          onClick={onConfirm}
        >
          {confirmLabel}
        </Button>
      </UnifiedModalFooter>
    </>
  );
};


export function ModalManager() {
  const { openModalId, modalProps, closeModal } = useModalStore();

  const handleOpenChange = (open: boolean) => {
    if (!open) {
      closeModal();
    }
  };

  // Helper to render modals consistently
  const renderModal = (id: ModalId, variant: ModalVariant, content: React.ReactNode) => {
    return (
      <UnifiedModalDialog
        open={openModalId === id}
        onOpenChange={handleOpenChange}
        variant={variant}
        {...modalProps} // Pass any specific props from the store
      >
        {content}
      </UnifiedModalDialog>
    );
  };

  return (
    <>
      {/* Settings Modal */} 
      {renderModal('settings', 'settings',
        <>
          <UnifiedModalHeader>
            <h2 className="text-xl font-semibold">Settings</h2>
          </UnifiedModalHeader>
          <UnifiedModalContent>
            {openModalId === 'settings' && <SettingsPanel />} 
          </UnifiedModalContent>
          {/* Settings might have its own footer or actions within SettingsPanel */}
        </>
      )}

      {/* System Prompt Modal */} 
      {renderModal('systemPrompt', 'library',
        <>
          <UnifiedModalHeader>
            <h2 className="text-xl font-semibold">System Prompts</h2>
          </UnifiedModalHeader>
          <UnifiedModalContent>
            {openModalId === 'systemPrompt' && (
              <SystemPromptLibrary
                // Pass necessary props, potentially from modalProps
                convId={modalProps?.convId}
                initialEditPromptId={modalProps?.editPromptId}
                initialShowEditor={modalProps?.createNew}
                onApplyPrompt={(prompt) => {
                  // Logic from SystemPromptDialog
                  console.log('Apply system prompt:', prompt);
                  closeModal();
                }}
                onEditorClosed={closeModal}
              />
            )}
          </UnifiedModalContent>
        </>
      )}

      {/* User Prompt Modal */} 
      {renderModal('userPrompt', 'library',
        <>
          <UnifiedModalHeader>
            <h2 className="text-xl font-semibold">User Prompts</h2>
          </UnifiedModalHeader>
          <UnifiedModalContent>
            {openModalId === 'userPrompt' && (
              <UserPromptLibrary
                onInsertPrompt={(prompt) => {
                  if (prompt) {
                    window.setPrompt(prompt.content);
                    closeModal();
                  }
                }}
              />
            )}
          </UnifiedModalContent>
        </>
      )}

      {/* Theme Library Modal */} 
      {renderModal('theme', 'library', // Using 'library' variant for now
        <>
          <UnifiedModalHeader>
            <h2 className="text-xl font-semibold">Theme Library</h2>
          </UnifiedModalHeader>
          <UnifiedModalContent>
            {openModalId === 'theme' && <ThemeManagement />} 
          </UnifiedModalContent>
        </>
      )}

      {/* Confirmation Modal Example */} 
      {openModalId === 'confirmation' && (
        <UnifiedModalDialog
          open={true}
          onOpenChange={handleOpenChange}
          variant="confirmation"
          {...modalProps} // Pass props like title, description, onConfirm, etc.
        >
          <ConfirmationContent
            title={modalProps?.title ?? 'Confirm Action'}
            description={modalProps?.description ?? 'Are you sure?'}
            onConfirm={() => {
              modalProps?.onConfirm?.();
              closeModal();
            }}
            onCancel={() => {
              modalProps?.onCancel?.();
              closeModal();
            }}
            confirmLabel={modalProps?.confirmLabel}
            cancelLabel={modalProps?.cancelLabel}
            danger={modalProps?.danger}
          />
        </UnifiedModalDialog>
      )}

      {/* Add other modals managed by useModalStore here */} 
    </>
  );
}