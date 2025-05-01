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

type ToolEditorModalProps = {
  tool: Tool | null;
  categories: any[];
  onClose: () => void;
};

export function ToolEditorModal() {
  const { openModalId, modalProps, closeModal } = useModalStore();
  const isOpen = openModalId === 'toolEditor';
  const props = isOpen ? (modalProps as ToolEditorModalProps) : null;

  if (!isOpen || !props) return null;

  return (
    <UnifiedModalDialog
      open={isOpen}
      onOpenChange={(open) => !open && closeModal()}
      variant="form"
      size="2xl"
    >
      <UnifiedModalHeader>
        <h2 className="text-xl font-semibold">{props.tool ? 'Edit Tool' : 'Create Tool'}</h2>
      </UnifiedModalHeader>
      <UnifiedModalContent>
        <ToolEditor 
          tool={props.tool} 
          onClose={() => closeModal()} 
          categories={props.categories} 
        />
      </UnifiedModalContent>
    </UnifiedModalDialog>
  );
}