// AiStudio4.Web/src/components/modals/ConfirmationDialog.tsx

import React from 'react';
import {
  UnifiedModalDialog,
  UnifiedModalHeader,
  UnifiedModalContent,
  UnifiedModalFooter
} from '@/components/ui/unified-modal-dialog';
import { Button } from '@/components/ui/button';

interface ConfirmationDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  title: string;
  description: React.ReactNode; // Allow React nodes for description
  confirmLabel?: string;
  cancelLabel?: string;
  onConfirm: () => void;
  onCancel?: () => void;
  danger?: boolean;
}

export function ConfirmationDialog({
  open,
  onOpenChange,
  title,
  description,
  confirmLabel = 'Confirm',
  cancelLabel = 'Cancel',
  onConfirm,
  onCancel,
  danger = false,
}: ConfirmationDialogProps) {

  const handleCancel = () => {
    onOpenChange(false);
    onCancel?.();
  };

  const handleConfirm = () => {
    // Note: onOpenChange(false) should ideally be called *after* the async onConfirm completes
    // But for simplicity here, we close immediately. Consider adding loading state if onConfirm is async.
    onConfirm();
    onOpenChange(false);
  };

  return (
    <UnifiedModalDialog
      open={open}
      onOpenChange={onOpenChange}
      variant="confirmation"
      preventClose={true} // Often good for confirmations
    >
      <UnifiedModalHeader>
        <h2 className="text-xl font-semibold">{title}</h2>
      </UnifiedModalHeader>

      <UnifiedModalContent>
        <div className="text-sm text-muted-foreground">{description}</div>
      </UnifiedModalContent>

      <UnifiedModalFooter>
        <Button variant="outline" onClick={handleCancel}>
          {cancelLabel}
        </Button>
        <Button
          variant={danger ? 'destructive' : 'default'}
          onClick={handleConfirm}
        >
          {confirmLabel}
        </Button>
      </UnifiedModalFooter>
    </UnifiedModalDialog>
  );
}