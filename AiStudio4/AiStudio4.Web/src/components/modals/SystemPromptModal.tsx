// AiStudio4.Web/src/components/modals/SystemPromptModal.tsx
import React from 'react';
import { useModalStore, ModalRegistry } from '@/stores/useModalStore';
import {
  UnifiedModalDialog,
  UnifiedModalHeader,
  UnifiedModalContent,
} from '@/components/ui/unified-modal-dialog';
import { SystemPromptLibrary } from '@/components/SystemPrompt/SystemPromptLibrary'; // Assuming this holds the content

type SystemPromptProps = ModalRegistry['systemPrompt'];

export function SystemPromptModal() {
  const { openModalId, modalProps, closeModal } = useModalStore();
  const isOpen = openModalId === 'systemPrompt';
  const props = isOpen ? (modalProps as SystemPromptProps) : null;

  if (!isOpen || !props) return null;

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
        {/* Render the actual system prompt library content */} 
        <SystemPromptLibrary
          convId={props.convId}
          initialEditPromptId={props.editPromptId}
          initialShowEditor={props.createNew}
          onApplyPrompt={(prompt) => {
            // Logic to apply prompt (needs implementation/callback)
            console.log('Apply system prompt:', prompt);
            closeModal();
          }}
          onEditorClosed={closeModal} // Close modal when editor part closes
        />
      </UnifiedModalContent>
    </UnifiedModalDialog>
  );
}