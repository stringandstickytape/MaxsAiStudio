// AiStudioClient\src\components\InputBar\AttachmentSection.tsx
import React from 'react';
import { AttachmentPreviewBar } from '@/components/AttachmentPreview';
import { useAttachmentStore } from '@/stores/useAttachmentStore';

interface AttachmentSectionProps {
    attachments?: any[]; // Make optional since we'll get from store
    removeAttachment?: (id: string) => void; // Make optional since we'll use store functions
    clearAttachments?: () => void; // Make optional since we'll use store functions
}

// Custom comparison function for AttachmentSection memoization
const areAttachmentSectionPropsEqual = (prevProps: AttachmentSectionProps, nextProps: AttachmentSectionProps) => {
  // Compare attachments array length and IDs
  const prevAttachments = prevProps.attachments || [];
  const nextAttachments = nextProps.attachments || [];
  
  if (prevAttachments.length !== nextAttachments.length) return false;
  
  for (let i = 0; i < prevAttachments.length; i++) {
    if (prevAttachments[i]?.id !== nextAttachments[i]?.id) return false;
  }
  
  return true;
};

export const AttachmentSection = React.memo(({
    attachments: propAttachments,
    removeAttachment: propRemoveAttachment,
    clearAttachments: propClearAttachments
}: AttachmentSectionProps) => {
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
}, areAttachmentSectionPropsEqual);