// AiStudioClient\src\components\ConvView\MessageEditor.tsx
import { Textarea } from '@/components/ui/textarea';
import { Check, X } from 'lucide-react';
import React from 'react';

interface MessageEditorProps {
  editContent: string;
  setEditContent: (content: string) => void;
  onSave: () => void;
  onCancel: () => void;
}

// Custom comparison function for MessageEditor memoization
const areEditorPropsEqual = (prevProps: MessageEditorProps, nextProps: MessageEditorProps) => {
  // Compare the editContent string - this is the main prop that changes
  if (prevProps.editContent !== nextProps.editContent) return false;
  
  // Note: We don't compare callback functions as they're expected to be stable
  // If the parent doesn't memoize them properly, this optimization is still beneficial
  
  return true;
};

export const MessageEditor = React.memo(({ 
  editContent, 
  setEditContent, 
  onSave, 
  onCancel 
}: MessageEditorProps) => {
  return (
    <div className="w-full">
      <Textarea
        value={editContent}
        onChange={(e) => setEditContent(e.target.value)}
        className="ConvView w-full h-40 mb-2 font-mono text-sm"
        style={{
          backgroundColor: 'var(--convview-bg, #374151)',
          borderColor: 'var(--convview-border-color, #4b5563)',
          color: 'var(--convview-text-color, #ffffff)'
        }}
      />
      <div className="ConvView flex justify-end gap-2">
        <button
          onClick={onSave}
          className="ConvView p-1.5 rounded-full"
          style={{
            backgroundColor: 'var(--convview-accent-color, #2563eb)',
            ':hover': {
              backgroundColor: 'var(--convview-accent-color, #1d4ed8)'
            }
          }}
          title="Save edits"
        >
          <Check size={16} />
        </button>
        <button
          onClick={onCancel}
          className="ConvView p-1.5 rounded-full"
          style={{
            backgroundColor: 'var(--convview-bg, #374151)',
            ':hover': {
              backgroundColor: 'var(--convview-bg, #4b5563)'
            }
          }}
          title="Cancel editing"
        >
          <X size={16} />
        </button>
      </div>
    </div>
  );
}, areEditorPropsEqual);