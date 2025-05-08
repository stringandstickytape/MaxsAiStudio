// AiStudioClient/src/components/servers/DeleteServerDialog.tsx
import React, { useState } from 'react';
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from '@/components/ui/alert-dialog';
import { useMcpServerStore } from '@/stores/useMcpServerStore';
import { AlertCircle } from 'lucide-react';
import { Alert, AlertDescription } from '@/components/ui/alert';

interface DeleteServerDialogProps {
  isOpen: boolean;
  onClose: () => void;
  serverId: string;
  serverName: string;
}

export function DeleteServerDialog({ isOpen, onClose, serverId, serverName }: DeleteServerDialogProps) {
  const { deleteServer } = useMcpServerStore();
  const [isDeleting, setIsDeleting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleDelete = async () => {
    setIsDeleting(true);
    setError(null);
    
    try {
      const success = await deleteServer(serverId);
      if (success) {
        onClose();
      } else {
        setError('Failed to delete server. Please try again.');
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'An unknown error occurred');
    } finally {
      setIsDeleting(false);
    }
  };

  return (
    <AlertDialog open={isOpen} onOpenChange={(open) => !open && onClose()}>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>Delete Server</AlertDialogTitle>
          <AlertDialogDescription>
            Are you sure you want to delete the server "{serverName}"? This action cannot be undone.
          </AlertDialogDescription>
        </AlertDialogHeader>
        
        {error && (
          <Alert variant="destructive" className="mb-4">
            <AlertCircle className="h-4 w-4" />
            <AlertDescription>{error}</AlertDescription>
          </Alert>
        )}
        
        <AlertDialogFooter>
          <AlertDialogCancel disabled={isDeleting}>Cancel</AlertDialogCancel>
          <AlertDialogAction 
            onClick={handleDelete} 
            disabled={isDeleting}
            className="bg-red-600 hover:bg-red-700"
          >
            {isDeleting ? 'Deleting...' : 'Delete'}
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  );
}