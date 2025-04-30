// AiStudio4.Web/src/components/modals/UserPromptModal.tsx
import React from 'react';
import { useModalStore, ModalRegistry } from '@/stores/useModalStore';
import {
  UnifiedModalDialog,
  UnifiedModalHeader,
  UnifiedModalContent,
} from '@/components/ui/unified-modal-dialog';
import { UserPromptLibrary } from '@/components/UserPrompt/UserPromptLibrary'; // Assuming this holds the content
import { windowEventService, WindowEvents } from '@/services/windowEvents';

type UserPromptProps = ModalRegistry['userPrompt'];

export function UserPromptModal() {
  const { openModalId, modalProps, closeModal } = useModalStore();
  const isOpen = openModalId === 'userPrompt';
  const props = isOpen ? (modalProps as UserPromptProps) : null;

  if (!isOpen) return null;

  return (
    <UnifiedModalDialog
      open={isOpen}
      onOpenChange={(open) => !open && closeModal()}
      variant="library"
    >
      <UnifiedModalHeader>
        <h2 className="text-xl font-semibold">User Prompts</h2>
      </UnifiedModalHeader>
      <UnifiedModalContent>
        {/* Render the actual user prompt library content */} 
        <UserPromptLibrary
          initialEditPromptId={props?.editPromptId}
          initialShowEditor={props?.createNew}
          onEditorClosed={closeModal}
          onInsertPrompt={(prompt) => {
            if (prompt?.content) {
              // Use the window event service to set the prompt
              windowEventService.emit(WindowEvents.SET_PROMPT, { text: prompt.content });
              closeModal();
            }
          }}
        />
      </UnifiedModalContent>
    </UnifiedModalDialog>
  );
}