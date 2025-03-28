import React from 'react';
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { useModalStore } from '@/stores/useModalStore';
import { SettingsPanel }  from '@/components/SettingsPanel'; // Assuming SettingsPanel holds the content

const SettingsDialog: React.FC = () => {
  const { openModalId, closeModal } = useModalStore();

  const isOpen = openModalId === 'settings';

  const handleOpenChange = (open: boolean) => {
    if (!open) {
      closeModal();
    }
  };

  return (
    <Dialog open={isOpen} onOpenChange={handleOpenChange}>
          <DialogContent className="w-[900px] max-w-none h-[700px] max-h-[90vh] flex flex-col p-0">
        <DialogHeader>
          <DialogTitle>Settings</DialogTitle>
        </DialogHeader>
        {/* Render the existing SettingsPanel content inside the modal */}
        {isOpen && <SettingsPanel />} 
      </DialogContent>
    </Dialog>
  );
};

export default SettingsDialog;
