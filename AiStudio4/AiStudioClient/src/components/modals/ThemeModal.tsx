// AiStudioClient/src/components/modals/ThemeModal.tsx
import React from 'react';
import { useModalStore } from '@/stores/useModalStore';
import {
  UnifiedModalDialog,
  UnifiedModalHeader,
  UnifiedModalContent,
} from '@/components/ui/unified-modal-dialog';
import { ThemeManagement } from '@/components/settings/ThemeManagement'; // Assuming this holds the content

export function ThemeModal() {
  const { openModalId, closeModal } = useModalStore();
  const isOpen = openModalId === 'theme';

  if (!isOpen) return null;

  return (
    <UnifiedModalDialog
      open={isOpen}
      onOpenChange={(open) => !open && closeModal()}
      variant="library" // Or another appropriate variant
    >
      <UnifiedModalHeader>
        <h2 className="text-xl font-semibold">Theme Library</h2>
      </UnifiedModalHeader>
      <UnifiedModalContent>
        {/* Render the actual theme management content */} 
        <ThemeManagement />
      </UnifiedModalContent>
    </UnifiedModalDialog>
  );
}