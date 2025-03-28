import { useModalStore } from '@/stores/useModalStore';
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { UserPromptLibrary } from './UserPromptLibrary';
export function UserPromptDialog() {
    const { openModalId, closeModal } = useModalStore();
    const isOpen = openModalId === 'userPrompt';
    const handleOpenChange = (open: boolean) => {
        if (!open) {
            closeModal();
        }
    };
    return (
        <Dialog open={isOpen} onOpenChange={handleOpenChange}>
            <DialogContent className="w-[900px] max-w-none h-[700px] max-h-[90vh] flex flex-col p-0">
                <DialogHeader className="px-6 pt-6 pb-2">
                    <DialogTitle>User Prompts</DialogTitle>
                </DialogHeader>
                <div className="flex-1 overflow-hidden">
                    {isOpen && (
                        <UserPromptLibrary
                            onInsertPrompt={(prompt) => {
                                if (prompt) {
                                    window.setPrompt(prompt.content);
                                    closeModal();
                                }
                            }}
                        />
                    )}
                </div>
            </DialogContent>
        </Dialog>
    );
}

