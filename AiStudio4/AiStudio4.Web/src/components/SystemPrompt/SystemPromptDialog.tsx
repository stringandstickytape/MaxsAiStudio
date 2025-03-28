import { useModalStore } from '@/stores/useModalStore';
import { Dialog, DialogContent } from '@/components/ui/dialog';
import { SystemPromptLibrary } from './SystemPromptLibrary';
import { useConvStore } from '@/stores/useConvStore';
import { useSystemPromptStore } from '@/stores/useSystemPromptStore';
import { useSystemPromptManagement } from '@/hooks/useResourceManagement';
export function SystemPromptDialog() {
    const { openModalId, closeModal } = useModalStore();
    const isOpen = openModalId === 'systemPrompt';

    const { activeConvId } = useConvStore();
    const { setConvPrompt } = useSystemPromptStore();
    const { setConvSystemPrompt } = useSystemPromptManagement();
    const handleOpenChange = (open: boolean) => {
        if (!open) {
            closeModal();
        }
    };
    return (
        <Dialog open={isOpen} onOpenChange={handleOpenChange}>
            <DialogContent className="bg-gray-900 border-gray-700 text-gray-100 max-w-3xl h-[80vh] p-0">
                <div className="h-full">
                    <SystemPromptLibrary
                        convId={activeConvId || undefined}
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
                            closeModal();
                        }}
                    />
                </div>
            </DialogContent>
        </Dialog>
    );
}

