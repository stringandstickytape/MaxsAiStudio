// src/components/SystemPrompt/SystemPromptComponent.tsx
import { useState, useEffect, useRef } from 'react';
import { Button } from '@/components/ui/button';
import { Textarea } from '@/components/ui/textarea';
import { ChevronDown, ChevronUp, MessageSquare, Settings } from 'lucide-react';
import { cn } from '@/lib/utils';
import { SystemPrompt } from '@/types/systemPrompt';
import { usePanelStore } from '@/stores/usePanelStore';
import { useSystemPromptStore } from '@/stores/useSystemPromptStore';
import { useSystemPromptManagement } from '@/hooks/useResourceManagement';
import { useConvStore } from '@/stores/useConvStore';

interface SystemPromptComponentProps {
  convId?: string;
  onOpenLibrary?: () => void;
}

export function SystemPromptComponent({ convId, onOpenLibrary }: SystemPromptComponentProps) {
  const { activeConvId: storeConvId } = useConvStore();
  const { togglePanel } = usePanelStore();
  const { prompts, defaultPromptId, convPrompts, setConvPrompt } = useSystemPromptStore();

  const { updateSystemPrompt, setConvSystemPrompt, isLoading: loading } = useSystemPromptManagement();

  const [expanded, setExpanded] = useState(false);
  const [isHovered, setIsHovered] = useState(false);
  const [editMode, setEditMode] = useState(false);
  const [promptContent, setPromptContent] = useState('');
  const [currentPrompt, setCurrentPrompt] = useState<SystemPrompt | null>(null);
  const promptRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    let promptToUse: SystemPrompt | null = null;

    const effectiveConvId = convId || storeConvId;
    if (effectiveConvId && convPrompts[effectiveConvId]) {
      promptToUse = prompts.find((p) => p.guid === convPrompts[effectiveConvId]) || null;
    }

    if (!promptToUse && defaultPromptId) {
      promptToUse = prompts.find((p) => p.guid === defaultPromptId) || null;
    }

    if (!promptToUse) {
      promptToUse = prompts.find((p) => p.isDefault) || null;
    }

    setCurrentPrompt(promptToUse);
    if (promptToUse) {
      setPromptContent(promptToUse.content);
    }
  }, [prompts, convId, storeConvId, convPrompts, defaultPromptId]);

  useEffect(() => {
    function handleClickOutside(event: MouseEvent) {
      if (promptRef.current && !promptRef.current.contains(event.target as Node)) {
        if (expanded) {
          setExpanded(false);
          setEditMode(false);
        }
        setIsHovered(false);
      }
    }

    document.addEventListener('mousedown', handleClickOutside);
    return () => {
      document.removeEventListener('mousedown', handleClickOutside);
    };
  }, [expanded]);

  const handleMouseEnter = () => {
    setIsHovered(true);
  };

  const handleMouseLeave = () => {
    if (!expanded) {
      setIsHovered(false);
    }
  };

  const toggleExpand = () => {
    setExpanded(!expanded);
    setEditMode(false);
  };

  const toggleEdit = () => {
    setEditMode(!editMode);
    if (!expanded) {
      setExpanded(true);
    }
  };

  const handleOpenLibrary = () => {
    if (onOpenLibrary) {
      onOpenLibrary();
    } else {
      // Use global event to trigger opening modal dialog
      window.dispatchEvent(new CustomEvent('open-system-prompt-library'));
    }
  };

  const getPromptDisplayText = () => {
    if (!currentPrompt) return 'System Prompt: None set';

    const truncatedContent =
      currentPrompt.content.length > 60 ? `${currentPrompt.content.substring(0, 60)}...` : currentPrompt.content;

    const effectiveConvId = convId || storeConvId;
    if (effectiveConvId && convPrompts[effectiveConvId] === currentPrompt.guid) {
      return `System Prompt: ${truncatedContent}`;
    } else {
      return `System Prompt: ${truncatedContent} (Default)`;
    }
  };

  const handleSavePrompt = async () => {
    if (!currentPrompt) return;

    try {
      const updatedPrompt = {
        ...currentPrompt,
        content: promptContent,
        modifiedDate: new Date().toISOString(),
      };

      await updateSystemPrompt(updatedPrompt);

      const effectiveConvId = convId || storeConvId;
      if (effectiveConvId && !convPrompts[effectiveConvId]) {
        await setConvSystemPrompt({
          convId: effectiveConvId,
          promptId: currentPrompt.guid,
        });

        setConvPrompt(effectiveConvId, currentPrompt.guid);
      }

      setEditMode(false);
    } catch (error) {
      console.error('Failed to update prompt:', error);
    }
  };

  if (loading && !currentPrompt) {
    return (
      <div className="flex items-center justify-center h-8 text-gray-400 text-sm">
        <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-blue-500 mr-2"></div>
        Loading prompts...
      </div>
    );
  }

  return (
    <div 
      className="relative w-full min-h-[32px] bg-gray-800/30 rounded" 
      ref={promptRef}
      onMouseEnter={handleMouseEnter}
      onMouseLeave={handleMouseLeave}
    >
      {/* Simple text label - always rendered but fades out when hovered */}
      <div 
        className={cn(
          "absolute inset-0 px-3 py-2 cursor-pointer transition-opacity duration-200 ease-in-out",
          (isHovered || expanded) ? "opacity-0 pointer-events-none" : "opacity-100"
        )}
        onClick={() => setIsHovered(true)}
      >
        <span className="text-gray-300 text-sm truncate">{getPromptDisplayText()}</span>
      </div>
      
      {/* Dropdown UI - always rendered but hidden until hovered */}
      <div
        className={cn(
          "transition-opacity duration-200 ease-in-out",
          (isHovered || expanded) ? "opacity-100" : "opacity-0 pointer-events-none"
        )}
      >
        <div
          className={cn(
            'border border-gray-700/50 rounded-lg transition-all duration-200',
            expanded ? 'bg-gray-800/60' : 'bg-gray-800/40 hover:bg-gray-800/60 cursor-pointer',
          )}
        >
          <div className="px-3 py-2 flex items-center justify-between" onClick={!expanded ? toggleExpand : undefined}>
            <div className="flex items-center">
              <MessageSquare className="h-4 w-4 text-gray-400 mr-2" />
              <span className="text-gray-300 text-sm truncate">{currentPrompt ? currentPrompt.title : 'System Prompt'}</span>
            </div>
            {expanded ? (
              <ChevronUp className="h-4 w-4 text-gray-400 cursor-pointer" onClick={toggleExpand} />
            ) : (
              <ChevronDown className="h-4 w-4 text-gray-400" />
            )}
          </div>

          {expanded && (
            <div className="absolute left-0 right-0 top-full mt-1 p-3 bg-gray-900/95 border border-gray-700/50 rounded-lg shadow-lg z-50 max-w-2xl mx-auto">
              <div className="flex justify-between items-center mb-2">
                <div className="flex items-center">
                  <MessageSquare className="h-4 w-4 text-gray-400 mr-2" />
                  <span className="text-gray-200 font-medium">
                    {currentPrompt ? currentPrompt.title : 'System Prompt'}
                  </span>
                </div>
                <div className="flex items-center gap-1">
                  <Button
                    variant="ghost"
                    size="icon"
                    onClick={toggleEdit}
                    className="h-7 w-7 text-gray-400 hover:text-gray-100"
                  >
                    <Settings className="h-4 w-4" />
                  </Button>
                  <Button
                    variant="ghost"
                    size="icon"
                    onClick={toggleExpand}
                    className="h-7 w-7 text-gray-400 hover:text-gray-100"
                  >
                    <ChevronUp className="h-4 w-4" />
                  </Button>
                </div>
              </div>

              {editMode ? (
                <>
                  <Textarea
                    value={promptContent}
                    onChange={(e) => setPromptContent(e.target.value)}
                    className="min-h-[100px] max-h-[300px] h-[300px] overflow-y-auto bg-gray-700 border-gray-600 text-gray-100 font-mono text-sm"
                    placeholder="Enter your system prompt here..."
                  />

                  <div className="flex justify-end gap-2 mt-3">
                    <Button
                      size="sm"
                      variant="outline"
                      onClick={() => {
                        if (currentPrompt) setPromptContent(currentPrompt.content);
                        setEditMode(false);
                      }}
                      className="text-xs h-8 bg-gray-700 hover:bg-gray-600 text-gray-200 border-gray-600"
                    >
                      Cancel
                    </Button>
                    <Button
                      size="sm"
                      onClick={handleSavePrompt}
                      className="text-xs h-8 bg-blue-600 hover:bg-blue-700 text-white"
                    >
                      Save Changes
                    </Button>
                  </div>
                </>
              ) : (
                <>
                  <pre className="p-2 bg-gray-700/30 rounded border border-gray-700/50 text-gray-200 font-mono text-sm whitespace-pre-wrap max-h-[300px] overflow-y-auto">
                    {currentPrompt?.content || 'No system prompt content'}
                  </pre>

                  <div className="flex justify-end mt-3">
                    <Button
                      size="sm"
                      variant="outline"
                      onClick={handleOpenLibrary}
                      className="text-xs h-8 bg-gray-700 hover:bg-gray-600 text-gray-200 border-gray-600"
                    >
                      Manage Prompts
                    </Button>
                  </div>
                </>
              )}
            </div>
          )}
        </div>
      </div>
    </div>
  );
}

