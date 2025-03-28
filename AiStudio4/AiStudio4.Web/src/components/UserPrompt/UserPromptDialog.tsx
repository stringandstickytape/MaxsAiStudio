import { useModalStore } from '@/stores/useModalStore';
import { Dialog, DialogContent } from '@/components/ui/dialog';
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
            <DialogContent className="bg-gray-900 border-gray-700 text-gray-100 max-w-3xl h-[80vh] p-0">
                <div className="h-full">
                    <UserPromptLibrary
                        onInsertPrompt={(prompt) => {
                            if (prompt) {
                                window.setPrompt(prompt.content);
                                closeModal();
                            }
                        }}
                    />
                </div>
            </DialogContent>
        </Dialog>
    );
}
