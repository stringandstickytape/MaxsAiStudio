// AiStudioClient\src\components\InputBar\AttachmentSection.tsx
import React from 'react';
import { AttachmentPreviewBar } from '@/components/AttachmentPreview';
import { useAttachmentStore } from '@/stores/useAttachmentStore';

interface AttachmentSectionProps {}


export const AttachmentSection = React.memo(() => {
    const { 
        stagedAttachments: attachments,
        removeStagedAttachment: removeAttachment,
        clearStagedAttachments: clearAttachments
    } = useAttachmentStore();
    
    if (attachments.length === 0) {
        return null;
    }
    
    return (
        <AttachmentPreviewBar
            attachments={attachments}
            onRemove={removeAttachment}
            onClear={clearAttachments}
        />
    );
});