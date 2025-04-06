// AiStudio4.Web\src\components\SystemPrompt\SystemPromptComponent.tsx
import { useState, useEffect, useRef } from 'react';
import { createPortal } from 'react-dom';
import { Button } from '@/components/ui/button';
import { Textarea } from '@/components/ui/textarea';
import { ChevronDown, ChevronUp, MessageSquare, Settings, Edit } from 'lucide-react';
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

  const handleSelectPrompt = async (prompt: SystemPrompt) => {
    const effectiveConvId = convId || storeConvId;
    if (!effectiveConvId) return; // Cannot set without a conversation

    try {
      await setConvSystemPrompt({ convId: effectiveConvId, promptId: prompt.guid });
      setConvPrompt(effectiveConvId, prompt.guid); // Update Zustand store immediately
    } catch (error) {
      console.error(`Failed to set conversation prompt ${effectiveConvId} to ${prompt.guid}:`, error);
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
      className="relative w-full" 
      ref={promptRef}
      onMouseEnter={handleMouseEnter}
      onMouseLeave={handleMouseLeave}
    >
      <div 
        className={cn(
          "absolute inset-0 cursor-pointer transition-opacity duration-200 ease-in-out", // Removed px-3 py-2
          (isHovered || expanded) ? "opacity-0 pointer-events-none" : "opacity-100"
        )}
        onClick={() => setIsHovered(true)}
      >
        <span className="text-gray-300 text-sm truncate block w-full">System Prompt: {currentPrompt ? currentPrompt.title : 'System Prompt'}</span>
      </div>
      
      
      <div
        className={cn(
          "transition-opacity duration-200 ease-in-out",
          (isHovered || expanded) ? "opacity-100" : "opacity-0 pointer-events-none"
        )}
      >

        <div // Removed border border-gray-700/50 rounded-lg
          className={cn(
            'transition-all duration-200',
            expanded ? '' : 'cursor-pointer',
          )}
        >
          <div className="flex items-center justify-between" onClick={!expanded ? toggleExpand : undefined}>
            <div className="flex items-center">
              <span className="text-gray-300 text-sm truncate">{getPromptDisplayText()}</span>
            </div>
            {expanded ? (
              <ChevronUp className="h-4 w-4 text-gray-400 cursor-pointer" onClick={toggleExpand} />
            ) : (
              <ChevronDown className="h-4 w-4 text-gray-400" />
            )}
          </div>

          {expanded && createPortal(
            <div className="absolute left-0 right-0 bottom-full z-50 max-w-2xl mx-auto bg-gray-800 p-4 rounded-md border border-gray-700/50">
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
                    <Edit className="h-4 w-4" />
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
                    className="min-h-[100px] max-h-[300px] h-[300px] overflow-y-auto bg-gray-700 border-gray-600 text-gray-100 font-mono text-sm mb-2"
                    placeholder="Enter your system prompt here..."
                  />

                  <div className="flex justify-end gap-2">
                    <Button
                      size="sm"
                      variant="outline"
                      onClick={() => {
                        if (currentPrompt) setPromptContent(currentPrompt.content);
                        setEditMode(false);
                      }}
                      className="text-xs h-8 text-gray-200 border-gray-600"
                    >
                      Cancel
                    </Button>
                    <Button
                      size="sm"
                      onClick={handleSavePrompt}
                      className="text-xs h-8 text-white"
                    >
                      Save Changes
                    </Button>
                  </div>
                </>
              ) : (
                <>
                  <pre className="text-gray-200 font-mono text-sm whitespace-pre-wrap max-h-[300px] overflow-y-auto mb-3">
                    {currentPrompt?.content || 'No system prompt content'}
                  </pre>
                  
                  {/* --- Prompt Pill Bar --- */}
                  <div className="flex flex-wrap gap-1 mb-3">
                    {prompts.map((prompt) => (
                      <Button
                        key={prompt.guid}
                        variant="outline"
                        size="sm"
                        onClick={() => handleSelectPrompt(prompt)}
                        className={cn(
                          "h-5 px-2 py-0 text-xs rounded-full border transition-colors flex-shrink-0",
                          currentPrompt?.guid === prompt.guid 
                            ? "bg-blue-600/20 border-blue-700/30 text-blue-200 hover:bg-blue-600/40 hover:text-blue-100"
                            : "bg-gray-600/10 border-gray-700/20 text-gray-300 hover:bg-gray-600/30 hover:text-gray-100"
                      )}
                      >
                        {prompt.title}
                      </Button>
                    ))}
                  </div>
                  {/* --- End Prompt Pill Bar --- */}

                  <div className="flex justify-end">
                    <Button
                      size="sm"
                      variant="outline"
                      onClick={handleOpenLibrary}
                      className="text-xs h-8 text-gray-200 border-gray-600 bg-gray-700 hover:bg-gray-600"
                    >
                      Manage Prompts
                    </Button>
                  </div>
                </>
              )}
            </div>,
            document.body
          )}
        </div>
      </div>
    </div>
  );
}