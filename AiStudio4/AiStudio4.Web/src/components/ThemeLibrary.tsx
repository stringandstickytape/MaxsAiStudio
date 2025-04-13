// AiStudio4.Web/src/components/ThemeLibrary.tsx

import React from 'react';
import { Dialog, DialogContent } from '@/components/ui/dialog';
import { ThemeManagement } from '@/components/settings/ThemeManagement';

interface ThemeLibraryProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
}

export const ThemeLibrary: React.FC<ThemeLibraryProps> = ({ open, onOpenChange }) => {
  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-4xl bg-gray-800 border-gray-700 text-gray-100 flex flex-col max-h-[90vh] overflow-hidden">
        <div className="flex-1 min-h-0 overflow-y-auto p-1">
          <ThemeManagement />
        </div>
      </DialogContent>
    </Dialog>
  );
};
