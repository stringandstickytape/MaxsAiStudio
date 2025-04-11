// AiStudio4.Web/src/components/themes/ThemeDialog.tsx

import { useState, useEffect } from 'react';
import { Dialog, DialogContent } from '@/components/ui/dialog';
import { ThemeLibraryPanel } from './ThemeLibraryPanel';
import { useModalStore } from '@/stores/useModalStore';
import { Theme } from '@/types/theme';
import themeManagerInstance from '@/lib/ThemeManager';

export function ThemeDialog() {
  const { openModalId, modalProps, closeModal } = useModalStore();
  const isOpen = openModalId === 'theme';
  
  useEffect(() => {
    const handleOpen = () => useModalStore.getState().openModal('theme');
    window.addEventListener('open-theme-library', handleOpen);
    return () => window.removeEventListener('open-theme-library', handleOpen);
  }, []);

  const handleApplyTheme = (theme: Theme) => {
    themeManagerInstance.applyTheme(theme.themeJson);
    
    // If the modalProps include installTheme=true, we'll keep the dialog open
    // Otherwise, close the dialog after applying the theme
    if (!modalProps?.installTheme) {
      closeModal();
    }
  };

  return (
    <Dialog open={isOpen} onOpenChange={(open) => !open && closeModal()}>
      <DialogContent className="bg-gray-900 border-gray-700 text-gray-100 max-w-4xl h-[80vh] p-0">
        <div className="h-full overflow-hidden">
          <ThemeLibraryPanel 
            isOpen={isOpen} 
            onClose={closeModal} 
            onApplyTheme={handleApplyTheme}
          />
        </div>
      </DialogContent>
    </Dialog>
  );
}