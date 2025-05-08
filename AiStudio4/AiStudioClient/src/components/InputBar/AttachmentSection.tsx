// AiStudioClient\src\components\InputBar\AttachmentSection.tsx
import React from 'react';
import { AttachmentPreviewBar } from '@/components/AttachmentPreview';
import { useAttachmentStore } from '@/stores/useAttachmentStore';

interface AttachmentSectionProps {
    attachments?: any[]; // Make optional since we'll get from store
    removeAttachment?: (id: string) => void; // Make optional since we'll use store functions
    clearAttachments?: () => void; // Make optional since we'll use store functions
}

export function AttachmentSection({
    attachments: propAttachments,
    removeAttachment: propRemoveAttachment,
    clearAttachments: propClearAttachments
}: AttachmentSectionProps) {
    // Get values from the store
    const stagedAttachments = useAttachmentStore(state => state.stagedAttachments);
    const removeStagedAttachment = useAttachmentStore(state => state.removeStagedAttachment);
    const clearStagedAttachments = useAttachmentStore(state => state.clearStagedAttachments);
    
    // Use props if provided, otherwise use store values
    const attachments = propAttachments || stagedAttachments;
    const removeAttachment = propRemoveAttachment || removeStagedAttachment;
    const clearAttachments = propClearAttachments || clearStagedAttachments;
    
    if (attachments.length === 0) {
        return null;
    }
    
    return (
        <div className="w-64 flex-shrink-0 overflow-auto">
            <AttachmentPreviewBar
                attachments={attachments}
                onRemove={removeAttachment}
                onClear={clearAttachments}
                className="h-full"
            />
        </div>
    );
}