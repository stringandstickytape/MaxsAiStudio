import React from 'react';
import {
  UnifiedModalDialog,
  UnifiedModalHeader,
  UnifiedModalContent,
  UnifiedModalFooter,
} from '@/components/ui/unified-modal-dialog';
import { useModalStore } from '@/stores/useModalStore';
import { MarkdownPane } from '../MarkdownPane';
import { Button } from '../ui/button';
import { ScrollArea } from '../ui/scroll-area';

export function InfoModal() {
  const { currentModal, closeModal } = useModalStore();
  const isOpen = currentModal?.id === 'info';

  if (!isOpen || !currentModal) return null;
  
  const { title, content } = currentModal.props as { title: string, content: string };

  return (
    <UnifiedModalDialog
      open={isOpen}
      onOpenChange={(open) => !open && closeModal()}
      variant="settings"
      size="lg"
      height="md"
    >
      <UnifiedModalHeader>
        <h2 className="text-xl font-semibold">{title}</h2>
      </UnifiedModalHeader>
      <UnifiedModalContent>
        <ScrollArea className="h-full pr-4">
            <MarkdownPane message={content} />
        </ScrollArea>
      </UnifiedModalContent>
      <UnifiedModalFooter>
        <Button onClick={closeModal}>Close</Button>
      </UnifiedModalFooter>
    </UnifiedModalDialog>
  );
}