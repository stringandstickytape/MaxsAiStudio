// AiStudio4.Web\src\components\InputBar\AttachmentSection.tsx
import React from 'react';
import { AttachmentPreviewBar } from '@/components/AttachmentPreview';
import { Attachment } from '@/types/attachment';

interface AttachmentSectionProps {
    attachments: Attachment[];
    removeAttachment: (id: string) => void;
    clearAttachments: () => void;
}

export function AttachmentSection({
    attachments,
    removeAttachment,
    clearAttachments
}: AttachmentSectionProps) {
    if (attachments.length === 0) {
        return null;
    }
    
    return (
        <div className="w-14 flex-shrink-0 overflow-auto">
            <AttachmentPreviewBar
                attachments={attachments}
                onRemove={removeAttachment}
                onClear={clearAttachments}
                className="h-full"
                iconsOnly={true}
                compact={true}
            />
        </div>
    );
}