// AiStudio4.Web\src\components\SystemPrompt\SystemPromptComponent.tsx
import { useState, useEffect, useRef } from 'react';
import { createPortal } from 'react-dom';
import { Button } from '@/components/ui/button';
import { Textarea } from '@/components/ui/textarea';
import { useUserPromptManagement } from '@/hooks/useUserPromptManagement';

// Simplified themeable properties for ThemeManager
export const themeableProps = {
  backgroundColor: {
    cssVar: '--systemprompt-bg',
    description: 'System prompt background color',
    default: '#2d3748',
  },
  textColor: {
    cssVar: '--systemprompt-text-color',
    description: 'System prompt text color',
    default: '#e2e8f0',
  },
  borderColor: {
    cssVar: '--systemprompt-border-color',
    description: 'Border color',
    default: '#4a5568',
  },
  accentColor: {
    cssVar: '--systemprompt-accent-color',
    description: 'Accent color for highlights and active elements',
    default: '#3b82f6',
  },
  // Additional properties needed for this component's unique features
  popupBackground: {
    cssVar: '--systemprompt-popup-bg',
    description: 'Popup background color',
    default: '#1a202c',
  },
  popupTextColor: {
    cssVar: '--systemprompt-popup-text-color',
    description: 'Popup text color',
    default: '#e2e8f0',
  },
  editBackground: {
    cssVar: '--systemprompt-edit-bg',
    description: 'Edit textarea background',
    default: '#2d3748',
  },
  style: {
    description: 'Arbitrary CSS style for SystemPromptComponent root',
    default: {},
  },
};

import { ChevronDown, ChevronUp, MessageSquare, Settings, Edit } from 'lucide-react';
import { cn } from '@/lib/utils';
import { SystemPrompt } from '@/types/systemPrompt';
import { usePanelStore } from '@/stores/usePanelStore';
import { useSystemPromptStore } from '@/stores/useSystemPromptStore';
import { useSystemPromptManagement } from '@/hooks/useResourceManagement';
import { useConvStore } from '@/stores/useConvStore';
import { useToolStore } from '@/stores/useToolStore';

interface SystemPromptComponentProps {
    convId?: string;
    onOpenLibrary?: () => void;
}

export function SystemPromptComponent({ convId, onOpenLibrary }: SystemPromptComponentProps) {
    const { activeConvId: storeConvId } = useConvStore();
    const { togglePanel } = usePanelStore();
    const { prompts, defaultPromptId, convPrompts, setConvPrompt } = useSystemPromptStore();

    const { updateSystemPrompt, setConvSystemPrompt, setDefaultSystemPrompt, getAssociatedUserPrompt, isLoading: loading } = useSystemPromptManagement();
    const { prompts: userPrompts, insertUserPrompt } = useUserPromptManagement();

    const [expanded, setExpanded] = useState(false);
    const [isHovered, setIsHovered] = useState(false);
    const [editMode, setEditMode] = useState(false);
    const [promptContent, setPromptContent] = useState('');
    const [portalReady, setPortalReady] = useState(false);
    const [currentPrompt, setCurrentPrompt] = useState<SystemPrompt | null>(null);
    const [portalStyle, setPortalStyle] = useState<React.CSSProperties>({});
    const [isProcessing, setIsProcessing] = useState(false);
    const promptRef = useRef<HTMLDivElement>(null);
    const portalContentRef = useRef<HTMLDivElement>(null);

    // Calculate portal position
    const updatePosition = () => {
        if (promptRef.current && portalContentRef.current) {
            const triggerRect = promptRef.current.getBoundingClientRect();
            const portalRect = portalContentRef.current.getBoundingClientRect();
            const spaceAbove = triggerRect.top;
            const spaceBelow = window.innerHeight - triggerRect.bottom;
            const PADDING = 8; // Space between trigger and portal

            let top;
            if (spaceAbove > portalRect.height + PADDING || spaceAbove >= spaceBelow) {
                // Position above
                top = triggerRect.top - portalRect.height - PADDING;
            } else {
                // Position below
                top = triggerRect.bottom + PADDING;
            }

            setPortalStyle({
                position: 'fixed',
                top: `${top}px`,
                left: `${triggerRect.left}px`,
                minWidth: `${triggerRect.width}px`,
                maxWidth: 'max(50vw, 400px)', // Limit width, responsive
            });
        }
    };

    // Update position when expanded state changes
    useEffect(() => {
        if (expanded) {
            // Timeout ensures the portal content is rendered and measurable
            const timer = setTimeout(updatePosition, 0);
            window.addEventListener('resize', updatePosition);
            window.addEventListener('scroll', updatePosition, true); // Use capture phase for scroll

            // Create a MutationObserver to watch for content changes
            const observer = new MutationObserver(updatePosition);

            if (portalContentRef.current) {
                observer.observe(portalContentRef.current, {
                    childList: true,
                    subtree: true,
                    characterData: true,
                    attributes: true
                });
            }

            return () => {
                clearTimeout(timer);
                window.removeEventListener('resize', updatePosition);
                window.removeEventListener('scroll', updatePosition, true);
                observer.disconnect();
            };
        } else {
            setPortalStyle({}); // Clear style when closed
        }
    }, [expanded]);

    // Update position when current prompt changes
    useEffect(() => {
        if (expanded && currentPrompt) {
            // Small delay to ensure the DOM has updated
            setTimeout(updatePosition, 10);
        }
    }, [currentPrompt, expanded]);

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
            const target = event.target as Node;
            // This condition should prevent closing if the click is inside promptRef
            if (promptRef.current && !promptRef.current.contains(target) && portalContentRef.current && !portalContentRef.current.contains(target)) {
                if (expanded) {
                    setExpanded(false);
                    setEditMode(false);
                    setPortalReady(false);
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
        if (expanded) {
            setExpanded(false);
            setEditMode(false);
            setPortalReady(false);
        } else {
            setExpanded(true);
            setEditMode(false);
        }
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
            // Synchronize active tools
            useToolStore.getState().setActiveTools(Array.isArray(prompt.associatedTools) ? prompt.associatedTools : []);
            
            // Handle associated user prompt if one exists
            if (prompt.associatedUserPromptId && prompt.associatedUserPromptId !== 'none') {
                // Find the user prompt in the local store instead of making an API call
                const userPrompt = userPrompts.find(up => up.guid === prompt.associatedUserPromptId);
                if (userPrompt) {
                    console.log('Activating associated user prompt:', userPrompt.title);
                    insertUserPrompt(userPrompt);
                } else {
                    console.warn('Associated user prompt not found in local store:', prompt.associatedUserPromptId);
                }
            }

            // Close the popup after selecting a prompt
            setExpanded(false);
            setEditMode(false);
        } catch (error) {
            console.error(`Failed to set conversation prompt ${effectiveConvId} to ${prompt.guid}:`, error);
        }
    };

    if (loading && !currentPrompt) {
        return (
            <div className="flex items-center justify-center h-8 text-sm">
                <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-blue-500 mr-2"></div>
                Loading prompts...
            </div>
        );
    }

    return (
        <div
            className="relative w-full SystemPromptComponent"
            ref={promptRef}
            onMouseEnter={handleMouseEnter}
            onMouseLeave={handleMouseLeave}
            style={{
                backgroundColor: 'var(--systemprompt-bg, #2d3748)',
                color: 'var(--systemprompt-text-color, #e2e8f0)',
                borderColor: 'var(--systemprompt-border-color, #4a5568)',
                borderRadius: '8px',
                boxShadow: '0 4px 12px rgba(0,0,0,0.3)',
                ...(window?.theme?.SystemPromptComponent?.style || {})
            }}
        >
            <div
                className={cn(
                    "absolute inset-0 cursor-pointer transition-opacity duration-200 ease-in-out", // Removed px-3 py-2
                    (isHovered || expanded) ? "opacity-0 pointer-events-none" : "opacity-100"
                )}
                onClick={() => setIsHovered(true)}
            >
                <span className="text-sm truncate block w-full">System Prompt: {currentPrompt ? currentPrompt.title : 'System Prompt'}</span>
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
                            <span className="text-sm truncate">{getPromptDisplayText()}</span>
                        </div>
                        {expanded ? (
                            <ChevronUp className="h-4 w-4 cursor-pointer" onClick={toggleExpand} />
                        ) : (
                            <ChevronDown className="h-4 w-4 " />
                        )}
                    </div>

                    {expanded && createPortal(
                        <div
                            ref={portalContentRef}
                            style={{
                                ...portalStyle,
                                // Use direct theme values instead of CSS variables for portal content
                                // since portals are outside the component hierarchy and won't inherit CSS variables
                                backgroundColor: 'var(--systemprompt-popup-bg, #1a202c)',
                                color: 'var(--systemprompt-popup-text-color, #e2e8f0)',
                                borderColor: 'var(--systemprompt-border-color, #4a5568)',
                                borderRadius: '8px',
                                boxShadow: '0 4px 12px rgba(0,0,0,0.3)',
                                ...(window?.theme?.SystemPromptComponent?.style || {})
                            }}
                            className="fixed z-50 p-4 border shadow-xl SystemPromptComponent"
                        >
                            <div className="flex justify-between items-center mb-2">
                                <div className="flex items-center">
                                    <MessageSquare className="h-4 w-4  mr-2" />
                                    <span className="font-medium">
                                        {currentPrompt ? currentPrompt.title : 'System Prompt'}
                                    </span>
                                </div>
                                <div className="flex items-center gap-1">
                                    <Button
                                        variant="ghost"
                                        size="icon"
                                        onClick={toggleEdit}
                                        className="h-7 w-7  "
                                    >
                                        <Edit className="h-4 w-4" />
                                    </Button>
                                    <Button
                                        variant="ghost"
                                        size="icon"
                                        onClick={toggleExpand}
                                        className="h-7 w-7  "
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
                                        style={{
                                            backgroundColor: 'var(--systemprompt-edit-bg, #2d3748)',
                                            color: 'var(--systemprompt-text-color, #e2e8f0)',
                                            ...(window?.theme?.SystemPromptComponent?.style || {})
                                        }}
                                        className="min-h-[100px] max-h-[300px] h-[300px] overflow-y-auto mb-2"
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
                                            className="text-xs h-8 border-gray-600"
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
                                    <pre className="font-mono text-sm whitespace-pre-wrap max-h-[300px] overflow-y-auto mb-3">
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
                                                onMouseDown={async (e) => {
                                                    if (e.button === 1 && !isProcessing) { // Middle click
                                                        e.preventDefault();
                                                        setIsProcessing(true);
                                                        const effectiveConvId = convId || storeConvId;
                                                        if (effectiveConvId) {
                                                            console.debug('[SystemPromptComponent] Middle-click detected on prompt pill:', prompt.guid, prompt.title);
                                                            await setConvSystemPrompt({ convId: effectiveConvId, promptId: prompt.guid });
                                                            setConvPrompt(effectiveConvId, prompt.guid);
                                                        }
                                                        try {
                                                            // Add debug logs for setDefaultSystemPrompt existence and type before calling
                                                            console.debug('[SystemPromptComponent] setDefaultSystemPrompt:', setDefaultSystemPrompt, typeof setDefaultSystemPrompt);
                                                            if (!setDefaultSystemPrompt) {
                                                                console.error('[SystemPromptComponent] setDefaultSystemPrompt is undefined!');
                                                            } else if (typeof setDefaultSystemPrompt !== 'function') {
                                                                console.error('[SystemPromptComponent] setDefaultSystemPrompt is not a function:', setDefaultSystemPrompt);
                                                            } else {
                                                                console.debug('[SystemPromptComponent] Calling setDefaultSystemPrompt:', prompt.guid);
                                                                const result = await setDefaultSystemPrompt(prompt.guid);
                                                                console.debug('[SystemPromptComponent] setDefaultSystemPrompt result:', result);
                                                            }
                                                        } catch (err) {
                                                            // fallback: try direct
                                                            if (typeof setDefaultSystemPrompt === 'function') {
                                                                setDefaultSystemPrompt(prompt.guid);
                                                            }
                                                        }
                                                        setExpanded(false);
                                                        setEditMode(false);
                                                        setIsProcessing(false);
                                                    }
                                                }}
                                                disabled={isProcessing}
                                                style={{
                                                    backgroundColor: currentPrompt?.guid === prompt.guid ? 'var(--systemprompt-accent-color, #3b82f6)33' : 'var(--systemprompt-bg, #2d3748)',
                                                    ...(window?.theme?.SystemPromptComponent?.style || {})
                                                }}
                                                className={cn(
                                                    "h-5 px-2 py-0 text-xs rounded-full border transition-colors flex-shrink-0",
                                                    currentPrompt?.guid === prompt.guid
                                                        ? "border-blue-700/30 hover:bg-blue-600/40 hover:text-blue-100"
                                                        : "border-gray-700/20 hover:bg-gray-600/30 "
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
                                            className="text-xs h-8 border-gray-600 bg-gray-700 hover:bg-gray-600"
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