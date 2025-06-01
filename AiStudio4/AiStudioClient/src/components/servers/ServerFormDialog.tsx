// AiStudioClient/src/components/servers/ServerFormDialog.tsx
import React, { useState } from 'react';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { ServerForm } from './ServerForm';
import { McpServerDefinition, useMcpServerStore } from '@/stores/useMcpServerStore';
import { AlertCircle } from 'lucide-react';
import { Alert, AlertDescription } from '@/components/ui/alert';

interface ServerFormDialogProps {
  isOpen: boolean;
  onClose: () => void;
  server?: McpServerDefinition;
  mode: 'add' | 'edit';
}

export function ServerFormDialog({ isOpen, onClose, server, mode }: ServerFormDialogProps) {
  const { addServer, updateServer } = useMcpServerStore();
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleSubmit = async (formData: McpServerDefinition) => {
    setIsSubmitting(true);
    setError(null);
    
    try {
      if (mode === 'add') {
        const result = await addServer(formData);
        if (result) {
          onClose();
        } else {
          setError('Failed to add server. Please try again.');
        }
      } else {
        const result = await updateServer(formData);
        if (result) {
          onClose();
        } else {
          setError('Failed to update server. Please try again.');
        }
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'An unknown error occurred');
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <Dialog open={isOpen} onOpenChange={(open) => !open && onClose()}>
      <DialogContent 
        className="sm:max-w-3xl ServerFormDialog" 
        style={{
          // Theme styles
          backgroundColor: 'var(--global-background-color)',
          color: 'var(--global-text-color)',
          fontFamily: 'var(--global-font-family)',
          fontSize: 'var(--global-font-size)',
          borderRadius: 'var(--global-border-radius)',
          borderColor: 'var(--global-border-color)',
          boxShadow: 'var(--global-box-shadow)',
          
          // Layout styles for fixed height and internal scrolling
          height: '70vh', // Fixed height, adjust (e.g., 65vh, 75vh) if needed to match "lg" feel
          display: 'flex',
          flexDirection: 'column',
          overflow: 'hidden', // DialogContent itself should not scroll; inner div will
        }}
      >
        <DialogHeader style={{ flexShrink: 0 }}> {/* Header takes its space, doesn't shrink */}
          <DialogTitle>{mode === 'add' ? 'Add New Server' : 'Edit Server'}</DialogTitle>
        </DialogHeader>
        
        {error && (
          <Alert variant="destructive" className="mb-4" style={{ flexShrink: 0 }}> {/* Alert takes its space */}
            <AlertCircle className="h-4 w-4" />
            <AlertDescription>{error}</AlertDescription>
          </Alert>
        )}
        
        {/* Wrapper for ServerForm to make it the scrollable content area */}
        <div style={{ flexGrow: 1, overflowY: 'auto', minHeight: 0 }}>
          <ServerForm
            server={server}
            onSubmit={handleSubmit}
            onCancel={onClose}
            isSubmitting={isSubmitting}
          />
        </div>
      </DialogContent>
    </Dialog>
  );
}