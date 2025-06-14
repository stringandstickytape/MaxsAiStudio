// AiStudioClient/src/components/modals/SystemPromptModal.tsx
import React from 'react';
import { useToolStore } from '@/stores/useToolStore';
import { useModalStore, ModalRegistry } from '@/stores/useModalStore';
import {
  UnifiedModalDialog,
  UnifiedModalHeader,
  UnifiedModalContent,
} from '@/components/ui/unified-modal-dialog';
import { SystemPromptLibrary } from '@/components/SystemPrompt/SystemPromptLibrary'; // Assuming this holds the content

type SystemPromptProps = ModalRegistry['systemPrompt'];

export function SystemPromptModal() {
  const { currentModal, closeModal } = useModalStore();
  const isOpen = currentModal?.id === 'systemPrompt';
  
  if (!isOpen || !currentModal) return null;
  
  // TypeScript now knows currentModal.props is SystemPromptProps
  const props = currentModal.props;

  return (
    <UnifiedModalDialog
      open={isOpen}
      onOpenChange={(open) => !open && closeModal()}
      variant="library"
    >
      <UnifiedModalHeader>
        <h2 className="text-xl font-semibold">System Prompts</h2>
      </UnifiedModalHeader>
      <UnifiedModalContent>
        {/* Render the actual system prompt library content */}        <SystemPromptLibrary
          convId={props?.convId}
          initialEditPromptId={props?.editPromptId}
          initialShowEditor={props?.createNew}
          onApplyPrompt={(prompt) => {
            // Synchronize active tools on modal apply
            useToolStore.getState().setActiveTools(Array.isArray(prompt.associatedTools) ? prompt.associatedTools : []);
            closeModal();
          }}
          onEditorClosed={closeModal} // Close modal when editor part closes
        />
      </UnifiedModalContent>
    </UnifiedModalDialog>
  );
}