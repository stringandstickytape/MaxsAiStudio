import React, { useState, useRef, useEffect } from 'react';
import { Check, X } from 'lucide-react';
import { Textarea } from '@/components/ui/textarea';

interface BlockEditorProps {
  initialContent: string;
  onSave: (newContent: string) => void;
  onCancel: () => void;
}

export const BlockEditor: React.FC<BlockEditorProps> = ({ initialContent, onSave, onCancel }) => {
  const [content, setContent] = useState(initialContent);
  const textareaRef = useRef<HTMLTextAreaElement>(null);

  useEffect(() => {
    if (textareaRef.current) {
      textareaRef.current.focus();
      textareaRef.current.select();
    }
  }, []);

  const handleSave = () => {
    onSave(content);
  };

  const handleCancel = () => {
    onCancel();
  };

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Escape') {
      handleCancel();
    } else if (e.key === 'Enter' && (e.ctrlKey || e.metaKey)) {
      e.preventDefault();
      handleSave();
    }
  };

  return (
    <div className="block-editor flex flex-col gap-2">
      <Textarea
        ref={textareaRef}
        value={content}
        onChange={(e) => setContent(e.target.value)}
        onKeyDown={handleKeyDown}
        className="min-h-[100px] resize-y"
        style={{
          backgroundColor: 'var(--global-input-background, #374151)',
          color: 'var(--global-input-text-color, #ffffff)',
          borderColor: 'var(--global-input-border-color, #6b7280)',
        }}
      />
      <div className="flex items-center gap-2">
        <button
          onClick={handleSave}
          className="flex items-center gap-1 px-3 py-1.5 rounded-md text-sm font-medium transition-colors"
          style={{
            backgroundColor: 'var(--global-primary-color, #2563eb)',
            color: 'var(--global-primary-text-color, #ffffff)',
          }}
          title="Save (Ctrl+Enter)"
        >
          <Check size={14} />
          Save
        </button>
        <button
          onClick={handleCancel}
          className="flex items-center gap-1 px-3 py-1.5 rounded-md text-sm font-medium transition-colors"
          style={{
            backgroundColor: 'var(--global-secondary-color, #6b7280)',
            color: 'var(--global-secondary-text-color, #ffffff)',
          }}
          title="Cancel (Escape)"
        >
          <X size={14} />
          Cancel
        </button>
        <span className="text-xs text-gray-500 ml-auto">
          Ctrl+Enter to save, Escape to cancel
        </span>
      </div>
    </div>
  );
};