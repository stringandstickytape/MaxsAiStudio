// AiStudioClient/src/components/modals/ThemeModal.tsx
import React from 'react';
import { useModalStore } from '@/stores/useModalStore';
import {
  UnifiedModalDialog,
  UnifiedModalHeader,
  UnifiedModalContent,
  UnifiedModalFooter,
} from '@/components/ui/unified-modal-dialog';
import { Button } from '@/components/ui/button';
import { ThemeManagement } from '@/components/settings/ThemeManagement'; // Assuming this holds the content

// Define themeable properties for the component
export const themeableProps = {};

export function ThemeModal() {
  const { currentModal, closeModal } = useModalStore();
  const isOpen = currentModal?.id === 'theme';
  
  if (!isOpen || !currentModal) return null;
  
  // TypeScript now knows currentModal.props is ThemeModalProps
  const props = currentModal.props;

  return (
    <UnifiedModalDialog
      open={isOpen}
      onOpenChange={(open) => !open && closeModal()}
      variant="library" 
      size="3xl" // Match ServerModal size
      height="lg" // Match ServerModal height
      className="ThemeLibraryModal" 
      style={{
        backgroundColor: 'var(--global-background-color)',
        color: 'var(--global-text-color)',
        fontFamily: 'var(--global-font-family)',
        fontSize: 'var(--global-font-size)'
      }}
    >
      <UnifiedModalHeader style={{ backgroundColor: 'var(--global-background-color)' }}>
        <div className="flex justify-between items-center w-full" style={{ backgroundColor: 'var(--global-background-color)', color: 'var(--global-text-color)' }}>
          <h2 className="text-title" style={{ color: 'var(--global-text-color)' }}>Theme Library</h2>
        </div>
      </UnifiedModalHeader>
      <UnifiedModalContent style={{ 
        backgroundColor: 'var(--global-background-color)',
        display: 'flex',
        flexDirection: 'column',
        overflow: 'hidden',
        height: '100%'
      }}>
        {/* Scrollable wrapper for content */}
        <div style={{ flexGrow: 1, overflowY: 'auto', minHeight: 0 }}>
          <ThemeManagement />
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