// AiStudioClient\src\components\SystemPrompt\SystemPromptComponent.tsx
import { useState, useEffect, useRef } from 'react';
import { createPortal } from 'react-dom';
import { useUserPromptManagement } from '@/hooks/useUserPromptManagement';
import { ChevronDown, ChevronUp } from 'lucide-react';
import { cn } from '@/lib/utils';
import { SystemPrompt } from '@/types/systemPrompt';
import { usePanelStore } from '@/stores/usePanelStore';
import { useSystemPromptStore } from '@/stores/useSystemPromptStore';
import { useSystemPromptManagement } from '@/hooks/useResourceManagement';
import { useConvStore } from '@/stores/useConvStore';
import { useToolStore } from '@/stores/useToolStore';
import { useSystemPromptSelection } from '@/hooks/useSystemPromptSelection';
import { SystemPromptCollapsedDisplay } from './SystemPromptCollapsedDisplay';
import { SystemPromptPortal } from './SystemPromptPortal';

// Simplified themeable properties for ThemeManager
export const themeableProps = {};

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
    const { selectSystemPrompt } = useSystemPromptSelection();

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
            const PADDING = 8;
            let top;
            if (spaceAbove > portalRect.height + PADDING || spaceAbove >= spaceBelow) {
                top = triggerRect.top - portalRect.height - PADDING;
            } else {
                top = triggerRect.bottom + PADDING;
            }
            setPortalStyle({
                position: 'fixed',
                top: `${top}px`,
                left: `${triggerRect.left}px`,
                minWidth: `${triggerRect.width}px`,
                maxWidth: 'max(50vw, 400px)',
            });
        }
    };

    useEffect(() => {
        if (expanded) {
            const timer = setTimeout(updatePosition, 0);
            window.addEventListener('resize', updatePosition);
            window.addEventListener('scroll', updatePosition, true);
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
            setPortalStyle({});
        }
    }, [expanded]);

    useEffect(() => {
        if (expanded && currentPrompt) {
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

    const handleMouseEnter = () => setIsHovered(true);
    const handleMouseLeave = () => { if (!expanded) setIsHovered(false); };
    const toggleExpand = () => { if (expanded) { setExpanded(false); setEditMode(false); setPortalReady(false); } else { setExpanded(true); setEditMode(false); } };
    const toggleEdit = () => { setEditMode(!editMode); if (!expanded) setExpanded(true); };
    const handleOpenLibrary = () => { if (onOpenLibrary) { onOpenLibrary(); } else { window.dispatchEvent(new CustomEvent('open-system-prompt-library')); } };
    const getPromptDisplayText = () => {
        if (!currentPrompt) return 'System Prompt: None set';
        const truncatedContent = currentPrompt.content.length > 60 ? `${currentPrompt.content.substring(0, 60)}...` : currentPrompt.content;
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
            const updatedPrompt = { ...currentPrompt, content: promptContent, modifiedDate: new Date().toISOString() };
            await updateSystemPrompt(updatedPrompt);
            const effectiveConvId = convId || storeConvId;
            if (effectiveConvId && !convPrompts[effectiveConvId]) {
                await setConvSystemPrompt({ convId: effectiveConvId, promptId: currentPrompt.guid });
                setConvPrompt(effectiveConvId, currentPrompt.guid);
            }
            setEditMode(false);
        } catch (error) {
            console.error('Failed to update prompt:', error);
        }
    };
    const handleSelectPrompt = async (prompt: SystemPrompt) => {
        const effectiveConvId = convId || storeConvId;
        if (!effectiveConvId) return;
        try {
            await selectSystemPrompt(prompt, { convId: effectiveConvId });
            setExpanded(false);
            setEditMode(false);
        } catch (error) {
            console.error(`Failed to set conversation prompt ${effectiveConvId} to ${prompt.guid}:`, error);
        }
    };
    const handleMiddleClickPrompt = async (prompt: SystemPrompt) => {
        const effectiveConvId = convId || storeConvId;
        if (!effectiveConvId) return;
        try {
            await selectSystemPrompt(prompt, { convId: effectiveConvId, setAsDefault: true });
            setExpanded(false);
            setEditMode(false);
        } catch (err) {
            console.error('[SystemPromptComponent] Error handling middle-click:', err);
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
                backgroundColor: 'var(--global-background-color)',
                color: 'var(--global-text-color)',
                borderColor: 'var(--global-border-color)',
                fontFamily: 'var(--global-font-family)',
                fontSize: 'var(--global-font-size)',
                borderRadius: 'var(--global-border-radius)',
                boxShadow: 'var(--global-box-shadow)',
                ...(window?.theme?.SystemPromptComponent?.style || {})
            }}
        >
            <SystemPromptCollapsedDisplay
                isVisible={!(isHovered || expanded)}
                onClick={() => setIsHovered(true)}
                title={currentPrompt ? currentPrompt.title : 'System Prompt'}
            />
            <div
                className={cn(
                    'transition-opacity duration-200 ease-in-out',
                    (isHovered || expanded) ? 'opacity-100' : 'opacity-0 pointer-events-none'
                )}
            >
                <div
                    className={cn('transition-all duration-200', expanded ? '' : 'cursor-pointer')}
                >
                    <div className="flex items-center justify-between" onClick={!expanded ? toggleExpand : undefined}>
                        <div className="flex items-center">
                            <span className="text-sm truncate">{getPromptDisplayText()}</span>
                        </div>
                        {expanded ? (
                            <ChevronUp className="h-4 w-4 cursor-pointer" onClick={toggleExpand} />
                        ) : (
                            <ChevronDown className="h-4 w-4" />
                        )}
                    </div>
                    {expanded && createPortal(
                        <SystemPromptPortal
                            portalStyle={{
                                ...portalStyle,
                                backgroundColor: 'var(--global-background-color)',
                                color: 'var(--global-text-color)',
                                borderColor: 'var(--global-border-color)',
                                borderRadius: 'var(--global-border-radius)',
                                boxShadow: 'var(--global-box-shadow)',
                                ...(window?.theme?.SystemPromptComponent?.style || {})
                            }}
                            portalContentRef={portalContentRef}
                            currentPrompt={currentPrompt}
                            editMode={editMode}
                            promptContent={promptContent}
                            setPromptContent={setPromptContent}
                            onSavePrompt={handleSavePrompt}
                            onCancelEdit={() => { if (currentPrompt) setPromptContent(currentPrompt.content); setEditMode(false); }}
                            onToggleEdit={toggleEdit}
                            onClose={toggleExpand}
                            prompts={prompts}
                            onSelectPrompt={handleSelectPrompt}
                            isProcessing={isProcessing}
                            setIsProcessing={setIsProcessing}
                            onManagePrompts={handleOpenLibrary}
                            selectedPromptGuid={currentPrompt?.guid}
                            onMiddleClickPrompt={handleMiddleClickPrompt}
                        />,
                        document.body
                    )}
                </div>
            </div>
        </div>
    );
}