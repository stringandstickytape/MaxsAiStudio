import { useModalStore } from '@/stores/useModalStore';
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { SystemPromptLibrary } from './SystemPromptLibrary';
import { useConvStore } from '@/stores/useConvStore';
import { useSystemPromptStore } from '@/stores/useSystemPromptStore';
import { useSystemPromptManagement } from '@/hooks/useResourceManagement';
export function SystemPromptDialog() {
    const { openModalId, closeModal, modalProps } = useModalStore();
    const isOpen = openModalId === 'systemPrompt';

    const { activeConvId } = useConvStore();
    const { setConvPrompt } = useSystemPromptStore();
    const { setConvSystemPrompt } = useSystemPromptManagement();
    const handleOpenChange = (open: boolean) => {
        if (!open) {
            closeModal();
        }
    };
    const handleEditorClose = () => {
        closeModal();
    };

    const editPromptId = modalProps?.editPromptId as string | undefined;
    const createNew = modalProps?.createNew as boolean | undefined;
    return (
        <Dialog open={isOpen} onOpenChange={handleOpenChange}>
            <DialogContent className="w-[900px] max-w-none h-[700px] max-h-[90vh] flex flex-col p-0">
        <DialogHeader className="px-6 pt-6 pb-2 border-b border-gray-700">
                    <DialogTitle>System Prompts</DialogTitle>
                </DialogHeader>
                <div className="flex-1 overflow-hidden">
                    {isOpen && (
                        <SystemPromptLibrary
                            convId={activeConvId || undefined}
                            initialEditPromptId={editPromptId} // Pass the ID from modal props
                            initialShowEditor={createNew} // Show editor immediately if createNew is true
                            onApplyPrompt={async (prompt) => {
                                const convId = activeConvId;
                                const promptId = prompt?.guid || prompt?.Guid;
                                if (convId && promptId) {
                                    try {
                                        await setConvSystemPrompt({ convId, promptId });
                                        setConvPrompt(convId, promptId);
                                        console.log(`SystemPromptDialog: Set conv ${convId} system prompt to ${promptId}`);
                                    } catch (error) {
                                        console.error('SystemPromptDialog: Failed to set conv system prompt:', error);
                                    }
                                } else {
                                    console.error('Cannot apply prompt - missing required data:', {
                                        convId,
                                        promptId
                                    });
                                }
                                closeModal(); // Close dialog on apply
                            }}
                            onEditorClosed={handleEditorClose} // Pass the close handler
                        />
                    )}
                </div>
            </DialogContent>
        </Dialog>
    );
}

