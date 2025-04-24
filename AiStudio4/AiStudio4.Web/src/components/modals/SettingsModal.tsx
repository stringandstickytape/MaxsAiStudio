// AiStudio4.Web/src/components/modals/SettingsModal.tsx
import React from 'react';
import { useModalStore } from '@/stores/useModalStore';
import {
  UnifiedModalDialog,
  UnifiedModalHeader,
  UnifiedModalContent,
} from '@/components/ui/unified-modal-dialog';
import { SettingsPanel } from '@/components/SettingsPanel'; // Assuming this holds the content

export function SettingsModal() {
  const { openModalId, closeModal } = useModalStore();
  const isOpen = openModalId === 'settings';

  if (!isOpen) return null;

  return (
    <UnifiedModalDialog
      open={isOpen}
      onOpenChange={(open) => !open && closeModal()}
      variant="settings"
    >
      <UnifiedModalHeader>
        <h2 className="text-xl font-semibold">Settings</h2>
      </UnifiedModalHeader>
      <UnifiedModalContent>
        {/* Render the actual settings content */} 
        <SettingsPanel />
      </UnifiedModalContent>
      {/* Footer/actions might be part of SettingsPanel or added here */} 
    </UnifiedModalDialog>
  );
}