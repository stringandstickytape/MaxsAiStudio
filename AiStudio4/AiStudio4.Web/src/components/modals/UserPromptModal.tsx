// AiStudio4.Web/src/components/modals/UserPromptModal.tsx
import React from 'react';
import { useModalStore } from '@/stores/useModalStore';
import {
  UnifiedModalDialog,
  UnifiedModalHeader,
  UnifiedModalContent,
} from '@/components/ui/unified-modal-dialog';
import { UserPromptLibrary } from '@/components/UserPrompt/UserPromptLibrary'; // Assuming this holds the content

export function UserPromptModal() {
  const { openModalId, closeModal } = useModalStore();
  const isOpen = openModalId === 'userPrompt';

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
          onInsertPrompt={(prompt) => {
            if (prompt?.content) {
              // Assuming window.setPrompt exists and is the correct way to insert
              window.setPrompt(prompt.content);
              closeModal();
            }
          }}
        />
      </UnifiedModalContent>
    </UnifiedModalDialog>
  );
}