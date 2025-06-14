// AiStudioClient/src/components/modals/UserPromptModal.tsx
import React from 'react';
import { useModalStore, ModalRegistry } from '@/stores/useModalStore';
import {
  UnifiedModalDialog,
  UnifiedModalHeader,
  UnifiedModalContent,
} from '@/components/ui/unified-modal-dialog';
import { UserPromptLibrary } from '@/components/UserPrompt/UserPromptLibrary'; // Assuming this holds the content
import { windowEventService, WindowEvents } from '@/services/windowEvents';
import { useInputBarStore } from '@/stores/useInputBarStore';

type UserPromptProps = ModalRegistry['userPrompt'];

export function UserPromptModal() {
  const { currentModal, closeModal } = useModalStore();
  const isOpen = currentModal?.id === 'userPrompt';
  
  if (!isOpen || !currentModal) return null;
  
  // TypeScript now knows currentModal.props is UserPromptProps
  const props = currentModal.props;

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
        {/* Render the actual user prompt library content */}        <UserPromptLibrary
          initialEditPromptId={props.editPromptId}
          initialShowEditor={props.createNew}
          onEditorClosed={closeModal}
          onInsertPrompt={(prompt) => {
            if (prompt?.content) {
              // Use the input bar store directly to set the prompt
              useInputBarStore.getState().setInputText(prompt.content);
              closeModal();
            }
          }}
        />
      </UnifiedModalContent>
    </UnifiedModalDialog>
  );
}