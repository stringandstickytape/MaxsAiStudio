// src/components/SystemPrompt/HeaderPromptComponent.tsx
import { useState, useEffect } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { RootState } from '@/store/store';
import { Button } from '@/components/ui/button';
import { Textarea } from '@/components/ui/textarea';
import { ChevronDown, ChevronUp, MessageSquare, Settings } from 'lucide-react';
import { cn } from '@/lib/utils';
import { SystemPrompt } from '@/types/systemPrompt';
import { fetchSystemPrompts, toggleLibrary, setConversationSystemPrompt, updateSystemPrompt } from '@/store/systemPromptSlice';

interface HeaderPromptComponentProps {
  conversationId?: string;
  onOpenLibrary?: () => void;
}

export function HeaderPromptComponent({ conversationId, onOpenLibrary }: HeaderPromptComponentProps) {
  const dispatch = useDispatch();
  const { prompts, defaultPromptId, conversationPrompts, loading } = useSelector((state: RootState) => state.systemPrompts);
  
  const [expanded, setExpanded] = useState(false);
  const [editMode, setEditMode] = useState(false);
  const [promptContent, setPromptContent] = useState('');
  const [currentPrompt, setCurrentPrompt] = useState<SystemPrompt | null>(null);
  
  // Load prompts when component mounts
  useEffect(() => {
    dispatch(fetchSystemPrompts());
  }, [dispatch]);
  
  // Determine the current prompt based on conversation or default
  useEffect(() => {
    let promptToUse: SystemPrompt | null = null;
    
    if (conversationId && conversationPrompts[conversationId]) {
      // Find the prompt assigned to this conversation
      promptToUse = prompts.find(p => p.guid === conversationPrompts[conversationId]) || null;
    }
    
    // If no conversation prompt is set, use the default
    if (!promptToUse && defaultPromptId) {
      promptToUse = prompts.find(p => p.guid === defaultPromptId) || null;
    }
    
    // If still no prompt, use the first one marked as default
    if (!promptToUse) {
      promptToUse = prompts.find(p => p.isDefault) || null;
    }
    
    // If absolutely no prompts, set null
    setCurrentPrompt(promptToUse);
    if (promptToUse) {
      setPromptContent(promptToUse.content);
    }
  }, [prompts, conversationId, conversationPrompts, defaultPromptId]);
  
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
      dispatch(toggleLibrary(true));
    }
  };
  
  const getPromptDisplayText = () => {
    if (!currentPrompt) return 'No system prompt set';
    
    // Truncate the content for display
    const truncatedContent = currentPrompt.content.length > 60
      ? `${currentPrompt.content.substring(0, 60)}...`
      : currentPrompt.content;
    
    // Show different text based on whether this is a conversation-specific or default prompt
    if (conversationId && conversationPrompts[conversationId] === currentPrompt.guid) {
      return truncatedContent;
    } else {
      return `${truncatedContent} (Default)`;
    }
  };
  
  const handleSavePrompt = () => {
    if (!currentPrompt) return;
    
    // Create updated prompt object
    const updatedPrompt = {
      ...currentPrompt,
      content: promptContent,
      modifiedDate: new Date().toISOString()
    };
    
    // Update the prompt
    dispatch(updateSystemPrompt(updatedPrompt));
    
    // If this is for a specific conversation, make sure it's set
    if (conversationId && !conversationPrompts[conversationId]) {
      dispatch(setConversationSystemPrompt({ conversationId, promptId: currentPrompt.guid }));
    }
    
    setEditMode(false);
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
    <div className="relative">
      <div 
        className={cn(
          "border border-gray-700/50 rounded-lg transition-all duration-300 overflow-hidden",
          expanded ? "bg-gray-800/60" : "bg-gray-800/40 hover:bg-gray-800/60 cursor-pointer"
        )}
      >
        {/* Collapsed view - just show the prompt title/summary */}
        {!expanded && (
          <div 
            className="px-3 py-2 flex items-center justify-between"
            onClick={toggleExpand}
          >
            <div className="flex items-center">
              <MessageSquare className="h-4 w-4 text-gray-400 mr-2" />
              <span className="text-gray-300 text-sm truncate">{getPromptDisplayText()}</span>
            </div>
            <ChevronDown className="h-4 w-4 text-gray-400" />
          </div>
        )}
        
        {/* Expanded view - show prompt content and editing options */}
        {expanded && (
          <div className="p-3">
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
                  className="min-h-[100px] bg-gray-700 border-gray-600 text-gray-100 font-mono text-sm"
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
                <pre className="p-2 bg-gray-700/30 rounded border border-gray-700/50 text-gray-200 font-mono text-sm whitespace-pre-wrap max-h-[200px] overflow-y-auto">
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
  );
}
