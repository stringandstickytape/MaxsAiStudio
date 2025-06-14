// AiStudioClient/src/components/modals/AppearanceModal.tsx
import React from 'react';
import { useModalStore } from '@/stores/useModalStore';
import {
  UnifiedModalDialog,
  UnifiedModalHeader,
  UnifiedModalContent,
} from '@/components/ui/unified-modal-dialog';
import { AppearanceTab } from '@/components/settings/AppearanceTab';

export function AppearanceModal() {
  const { currentModal, closeModal } = useModalStore();
  const isOpen = currentModal?.id === 'appearance';
  
  if (!isOpen || !currentModal) return null;
  
  // TypeScript now knows currentModal.props is AppearanceModalProps
  const props = currentModal.props;

  return (
    <UnifiedModalDialog
      open={isOpen}
      onOpenChange={(open) => !open && closeModal()}
      variant="settings"
      size="lg"
    >
      <UnifiedModalHeader>
        <h2 className="text-xl font-semibold">Appearance Settings</h2>
      </UnifiedModalHeader>
      <UnifiedModalContent>
        <AppearanceTab />
      </UnifiedModalContent>
    </UnifiedModalDialog>
  );
}