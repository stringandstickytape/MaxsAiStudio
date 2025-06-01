// AiStudioClient/src/components/modals/ToolModal.tsx
import React from 'react';
import {
  UnifiedModalDialog,
  UnifiedModalHeader,
  UnifiedModalContent,
  UnifiedModalFooter,
} from '@/components/ui/unified-modal-dialog';
import { useModalStore, ModalRegistry } from '@/stores/useModalStore';
import { ToolPanel } from '../tools/ToolPanel';
import { Button } from '@/components/ui/button';

type ToolModalProps = ModalRegistry['tool'];

// Define themeable properties for the component
export const themeableProps = {};

export function ToolModal() {
  const { openModalId, modalProps, closeModal } = useModalStore();
  const isOpen = openModalId === 'tool';
  const props = isOpen ? (modalProps as ToolModalProps) : null;

  if (!isOpen) return null;

  return (
    <UnifiedModalDialog
      open={isOpen}
      onOpenChange={(open) => !open && closeModal()}
      variant="library" // Use library variant as a base
      size="3xl" // Changed from 4xl to 3xl to match ServerModal
      height="lg" // Changed from xl to lg to match ServerModal
      className="ToolLibraryModal p-0" // Added class name for consistency
      style={{
        backgroundColor: 'var(--global-background-color)',
        color: 'var(--global-text-color)',
        fontFamily: 'var(--global-font-family)',
        fontSize: 'var(--global-font-size)'
      }}
    >
      <UnifiedModalHeader style={{ backgroundColor: 'var(--global-background-color)' }}>
        <div className="flex justify-between items-center w-full" style={{ backgroundColor: 'var(--global-background-color)', color: 'var(--global-text-color)' }}>
          <h2 className="text-title" style={{ color: 'var(--global-text-color)' }}>Tools Library</h2>
        </div>
      </UnifiedModalHeader>
      <UnifiedModalContent className="p-0" style={{ 
        backgroundColor: 'var(--global-background-color)',
        display: 'flex',
        flexDirection: 'column',
        overflow: 'hidden',
        height: '100%'
      }}>
        <div style={{ flexGrow: 1, overflowY: 'auto', minHeight: 0 }}>
          <ToolPanel 
            isOpen={isOpen}
            onClose={() => closeModal()}
            isModal={true}
          />
        </div>
      </UnifiedModalContent>
      <UnifiedModalFooter style={{ backgroundColor: 'var(--global-background-color)' }}>
        <Button 
          variant="outline" 
          size="sm" 
          onClick={() => closeModal()}
          style={{
            backgroundColor: 'var(--global-background-color)',
            borderColor: 'var(--global-border-color)',
            color: 'var(--global-text-color)'
          }}
        >
          Close
        </Button>
      </UnifiedModalFooter>
    </UnifiedModalDialog>
  );
}