// AiStudioClient/src/components/modals/ToolEditorModal.tsx
import React from 'react';
import {
  UnifiedModalDialog,
  UnifiedModalHeader,
  UnifiedModalContent,
} from '@/components/ui/unified-modal-dialog';
import { useModalStore, ModalRegistry } from '@/stores/useModalStore';
import { ToolEditor } from '../tools/ToolEditor';
import { Tool } from '@/types/toolTypes';
import { Model } from '@/types/settings';

// Define themeable properties for the component
export const themeableProps = {};

type ToolEditorModalProps = {
  tool: Tool | null;
  categories: any[];
  models: Model[];
  onClose: () => void;
};

export function ToolEditorModal() {
  const { currentModal, closeModal } = useModalStore();
  const isOpen = currentModal?.id === 'toolEditor';
  
  if (!isOpen || !currentModal) return null;
  
  // TypeScript now knows currentModal.props is ToolEditorModalProps
  const props = currentModal.props;

  return (
    <UnifiedModalDialog
      open={isOpen}
      onOpenChange={(open) => !open && closeModal()}
      variant="form"
      size="2xl"
      className="ToolEditorModal"
      style={{
        backgroundColor: 'var(--global-background-color)',
        color: 'var(--global-text-color)',
        borderRadius: 'var(--global-border-radius)',
        fontFamily: 'var(--global-font-family)',
        fontSize: 'var(--global-font-size)',
        boxShadow: 'var(--global-box-shadow)',
        border: `1px solid var(--global-border-color)`
      }}
    >
      <UnifiedModalHeader style={{
        backgroundColor: 'var(--global-background-color)',
        color: 'var(--global-text-color)',
        borderBottom: `1px solid var(--global-border-color)`
      }}>
        <h2 className="text-xl font-semibold">{props.tool ? 'Edit Tool' : 'Create Tool'}</h2>
      </UnifiedModalHeader>
      <UnifiedModalContent style={{
        backgroundColor: 'var(--global-background-color)',
        color: 'var(--global-text-color)'
      }}>
        <ToolEditor 
          tool={props.tool} 
          onClose={() => {}} 
          categories={props.categories}
          models={props.models}
        />
      </UnifiedModalContent>
    </UnifiedModalDialog>
  );
}