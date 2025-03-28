import React from 'react';
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { useModalStore } from '@/stores/useModalStore';
import { SettingsPanel } from '@/components/SettingsPanel';
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
            <DialogContent className="w-[900px] max-w-none h-[700px] max-h-[90vh] flex flex-col p-0 bg-gray-900 border-gray-700 text-gray-100">
                <DialogHeader className="px-6 pt-6 pb-2 border-b border-gray-700">
                    <DialogTitle>Settings</DialogTitle>
                </DialogHeader>
                <div className="flex-1 overflow-hidden">
                    {isOpen && <SettingsPanel />}
                </div>
            </DialogContent>
        </Dialog>
    );
};
export default SettingsDialog;

