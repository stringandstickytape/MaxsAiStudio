// AiStudio4.Web/src/components/modals/ToolModal.tsx
import React from 'react';
import { useModalStore } from '@/stores/useModalStore';
import {
  UnifiedModalDialog,
  UnifiedModalHeader,
  UnifiedModalContent,
} from '@/components/ui/unified-modal-dialog';

// Placeholder content - Replace with actual Tool Management UI
const ToolManagementContent = () => (
  <div>Tool Management Interface Placeholder</div>
);

export function ToolModal() {
  const { openModalId, closeModal } = useModalStore();
  const isOpen = openModalId === 'tool';

  if (!isOpen) return null;

  return (
    <UnifiedModalDialog
      open={isOpen}
      onOpenChange={(open) => !open && closeModal()}
      variant="library" // Or another appropriate variant
    >
      <UnifiedModalHeader>
        <h2 className="text-xl font-semibold">Tools</h2>
      </UnifiedModalHeader>
      <UnifiedModalContent>
        {/* Render the actual tool management content */} 
        <ToolManagementContent />
      </UnifiedModalContent>
    </UnifiedModalDialog>
  );
}