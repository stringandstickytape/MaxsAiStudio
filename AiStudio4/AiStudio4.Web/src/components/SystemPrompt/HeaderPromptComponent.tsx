// src/components/SystemPrompt/HeaderPromptComponent.tsx
import { useState, useEffect, useRef } from 'react';
import { Button } from '@/components/ui/button';
import { Textarea } from '@/components/ui/textarea';
import { ChevronDown, ChevronUp, MessageSquare, Settings } from 'lucide-react';
import { cn } from '@/lib/utils';
import { SystemPrompt } from '@/types/systemPrompt';
import { usePanelStore } from '@/stores/usePanelStore';
import { useSystemPromptStore } from '@/stores/useSystemPromptStore';
import { useSystemPromptManagement } from '@/hooks/useSystemPromptManagement';
import { useConversationStore } from '@/stores/useConversationStore';

interface HeaderPromptComponentProps {
    conversationId?: string;
    onOpenLibrary?: () => void;
}

export function HeaderPromptComponent({ conversationId, onOpenLibrary }: HeaderPromptComponentProps) {
    // Get the active conversation ID from Zustand when not provided as prop
    const { activeConversationId: storeConversationId } = useConversationStore();
    // Use Zustand stores
    const { togglePanel } = usePanelStore();
    const { 
        prompts, 
        defaultPromptId, 
        conversationPrompts, 
        setConversationPrompt 
    } = useSystemPromptStore();

    // Use the management hook for API operations
    const { 
        updateSystemPrompt,
        setConversationSystemPrompt,
        isLoading: loading
    } = useSystemPromptManagement();

    const [expanded, setExpanded] = useState(false);
    const [editMode, setEditMode] = useState(false);
    const [promptContent, setPromptContent] = useState('');
    const [currentPrompt, setCurrentPrompt] = useState<SystemPrompt | null>(null);
    const promptRef = useRef<HTMLDivElement>(null);

    // Determine the current prompt based on conversation or default
    useEffect(() => {
        let promptToUse: SystemPrompt | null = null;

        const effectiveConversationId = conversationId || storeConversationId;
        if (effectiveConversationId && conversationPrompts[effectiveConversationId]) {
            // Find the prompt assigned to this conversation
            promptToUse = prompts.find(p => p.guid === conversationPrompts[effectiveConversationId]) || null;
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
    }, [prompts, conversationId, storeConversationId, conversationPrompts, defaultPromptId]);

    // Handle clicks outside the component
    useEffect(() => {
        function handleClickOutside(event: MouseEvent) {
            if (promptRef.current && !promptRef.current.contains(event.target as Node) && expanded) {
                setExpanded(false);
                setEditMode(false);
            }
        }

        document.addEventListener('mousedown', handleClickOutside);
        return () => {
            document.removeEventListener('mousedown', handleClickOutside);
        };
    }, [expanded]);

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
            togglePanel('systemPrompts');
        }
    };

    const getPromptDisplayText = () => {
        if (!currentPrompt) return 'No system prompt set';

        // Truncate the content for display
        const truncatedContent = currentPrompt.content.length > 60
            ? `${currentPrompt.content.substring(0, 60)}...`
            : currentPrompt.content;

        const effectiveConversationId = conversationId || storeConversationId;
        if (effectiveConversationId && conversationPrompts[effectiveConversationId] === currentPrompt.guid) {
            return truncatedContent;
        } else {
            return `${truncatedContent} (Default)`;
        }
    };

    const handleSavePrompt = async () => {
        if (!currentPrompt) return;

        try {
            // Create updated prompt object
            const updatedPrompt = {
                ...currentPrompt,
                content: promptContent,
                modifiedDate: new Date().toISOString()
            };

            // Update the prompt using the management hook
            await updateSystemPrompt(updatedPrompt);

            const effectiveConversationId = conversationId || storeConversationId;
            if (effectiveConversationId && !conversationPrompts[effectiveConversationId]) {
                await setConversationSystemPrompt({
                    conversationId: effectiveConversationId,
                    promptId: currentPrompt.guid
                });

                // Also update the local state in Zustand
                setConversationPrompt(effectiveConversationId, currentPrompt.guid);
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
        <div className="relative w-full max-w-2xl mx-auto" ref={promptRef}>
            <div
                className={cn(
                    "border border-gray-700/50 rounded-lg transition-all duration-300",
                    expanded ? "bg-gray-800/60" : "bg-gray-800/40 hover:bg-gray-800/60 cursor-pointer"
                )}
            >
                {/* Collapsed view - just show the prompt title/summary when expanded too */}
                <div
                    className="px-3 py-2 flex items-center justify-between"
                    onClick={!expanded ? toggleExpand : undefined}
                >
                    <div className="flex items-center">
                        <MessageSquare className="h-4 w-4 text-gray-400 mr-2" />
                        <span className="text-gray-300 text-sm truncate">{getPromptDisplayText()}</span>
                    </div>
                    {expanded ? (
                        <ChevronUp className="h-4 w-4 text-gray-400 cursor-pointer" onClick={toggleExpand} />
                    ) : (
                        <ChevronDown className="h-4 w-4 text-gray-400" />
                    )}
                </div>

                {/* Expanded view appears as a dropdown */}
                {expanded && (
                    <div className="absolute left-0 right-0 top-full mt-1 p-3 bg-gray-800/95 border border-gray-700/50 rounded-lg shadow-lg z-50">
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
    );
}