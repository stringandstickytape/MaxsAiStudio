// AiStudioClient\src\components\ConvView\MessageEditor.tsx
import { Textarea } from '@/components/ui/textarea';
import { Check, X } from 'lucide-react';

interface MessageEditorProps {
  editContent: string;
  setEditContent: (content: string) => void;
  onSave: () => void;
  onCancel: () => void;
}

export const MessageEditor = ({ 
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
};