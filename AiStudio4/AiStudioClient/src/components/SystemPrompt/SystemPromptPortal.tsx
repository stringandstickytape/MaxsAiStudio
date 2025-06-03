// AiStudioClient/src/components/SystemPrompt/SystemPromptPortal.tsx
import React, { RefObject } from 'react';
import { Button } from '@/components/ui/button';
import { Textarea } from '@/components/ui/textarea';
import { ChevronUp, Edit, MessageSquare } from 'lucide-react';
import { cn } from '@/lib/utils';
import { SystemPrompt } from '@/types/systemPrompt';

interface SystemPromptPortalProps {
  portalStyle: React.CSSProperties;
  portalContentRef: RefObject<HTMLDivElement>;
  currentPrompt: SystemPrompt | null;
  editMode: boolean;
  promptContent: string;
  setPromptContent: (v: string) => void;
  onSavePrompt: () => void;
  onCancelEdit: () => void;
  onToggleEdit: () => void;
  onClose: () => void;
  prompts: SystemPrompt[];
  onSelectPrompt: (prompt: SystemPrompt) => void;
  isProcessing: boolean;
  setIsProcessing: (b: boolean) => void;
  onManagePrompts: () => void;
  selectedPromptGuid: string | undefined;
  onMiddleClickPrompt: (prompt: SystemPrompt) => Promise<void>;
}

export function SystemPromptPortal({
  portalStyle,
  portalContentRef,
  currentPrompt,
  editMode,
  promptContent,
  setPromptContent,
  onSavePrompt,
  onCancelEdit,
  onToggleEdit,
  onClose,
  prompts,
  onSelectPrompt,
  isProcessing,
  setIsProcessing,
  onManagePrompts,
  selectedPromptGuid,
  onMiddleClickPrompt
}: SystemPromptPortalProps) {
  return (
    <div
      ref={portalContentRef}
      style={portalStyle}
      className="fixed z-50 p-4 border shadow-xl SystemPromptComponent"
    >
      <div className="flex justify-between items-center mb-2">
        <div className="flex items-center">
          <MessageSquare className="h-4 w-4 mr-2" />
          <span className="font-medium">
            {currentPrompt ? currentPrompt.title : 'System Prompt'}
          </span>
        </div>
        <div className="flex items-center gap-1">
          <Button variant="ghost" size="sm" onClick={onToggleEdit} className="h-7 flex items-center gap-1 px-2">
            <Edit className="h-4 w-4" />
            <span className="text-xs font-medium">Quick Edit</span>
          </Button>
          <Button variant="ghost" size="icon" onClick={onClose} className="h-7 w-7">
            <ChevronUp className="h-4 w-4" />
          </Button>
        </div>
      </div>
      {editMode ? (
        <>
          <Textarea
            value={promptContent}
            onChange={e => setPromptContent(e.target.value)}
            style={{ backgroundColor: 'var(--systemprompt-edit-bg, #2d3748)', color: 'var(--systemprompt-text-color, #e2e8f0)' }}
            className="min-h-[100px] max-h-[300px] h-[300px] overflow-y-auto mb-2"
            placeholder="Enter your system prompt here..."
          />
          <div className="flex justify-end gap-2">
            <Button size="sm" variant="outline" onClick={onCancelEdit} className="text-xs h-8 border-gray-600">
              Cancel
            </Button>
            <Button size="sm" onClick={onSavePrompt} className="text-xs h-8 text-white">
              Save Changes
            </Button>
          </div>
        </>
      ) : (
        <>
          <pre className="font-mono text-sm whitespace-pre-wrap max-h-[300px] overflow-y-auto mb-3">
            {currentPrompt?.content || 'No system prompt content'}
          </pre>
          <div className="flex flex-wrap gap-1 mb-3">
            {prompts.map(prompt => (
              <Button
                key={prompt.guid}
                variant="outline"
                size="sm"
                onClick={() => onSelectPrompt(prompt)}
                onMouseDown={async e => {
                  if (e.button === 1 && !isProcessing) {
                    e.preventDefault();
                    setIsProcessing(true);
                    try {
                      await onMiddleClickPrompt(prompt);
                    } finally {
                      setIsProcessing(false);
                    }
                  }
                }}
                disabled={isProcessing}
                style={{ backgroundColor: selectedPromptGuid === prompt.guid ? 'var(--systemprompt-accent-color, #3b82f6)33' : 'var(--systemprompt-bg, #2d3748)' }}
                className={cn(
                  'h-5 px-2 py-0 text-xs rounded-full border transition-colors flex-shrink-0',
                  selectedPromptGuid === prompt.guid
                    ? 'border-blue-700/30 hover:bg-blue-600/40 hover:text-blue-100'
                    : 'border-gray-700/20 hover:bg-gray-600/30'
                )}
              >
                {prompt.title}
              </Button>
            ))}
          </div>
          <div className="flex justify-end">
            <Button
              size="sm"
              variant="outline"
              onClick={onManagePrompts}
              className="text-xs h-8 border-gray-600 bg-gray-700 hover:bg-gray-600"
            >
              Manage Prompts
            </Button>
          </div>
        </>
      )}
    </div>
  );
}