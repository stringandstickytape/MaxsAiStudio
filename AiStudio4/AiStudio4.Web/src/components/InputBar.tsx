import React, { useState, KeyboardEvent, useCallback, useRef, useEffect, useMemo } from 'react';
import { Button } from '@/components/ui/button';
import { useModalStore } from '@/stores/useModalStore';
import { v4 as uuidv4 } from 'uuid';
import { Mic, Send, BookMarked, X, Wrench } from 'lucide-react';
import { ModelStatusBar } from '@/components/ModelStatusBar';
import { FileAttachment } from './FileAttachment';
import { Attachment } from '@/types/attachment';
import { AttachmentPreviewBar } from './AttachmentPreview';
import { Textarea } from '@/components/ui/textarea';
import { useToolStore } from '@/stores/useToolStore';
import { useSystemPromptStore } from '@/stores/useSystemPromptStore';
import { useToolsManagement } from '@/hooks/useToolsManagement';
import { useConvStore } from '@/stores/useConvStore';
import { handlePromptShortcut } from '@/commands/shortcutPromptExecutor';
import { useWebSocketStore } from '@/stores/useWebSocketStore';
import { listenToWebSocketEvent } from '@/services/websocket/websocketEvents';
import { useChatManagement } from '@/hooks/useChatManagement';
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from '@/components/ui/tooltip';
import { useMediaQuery } from '@/hooks/useMediaQuery';
import { useAttachmentManager } from '@/hooks/useAttachmentManager';
import { formatTextAttachments } from '@/utils/attachmentUtils';
import { SystemPromptComponent } from '@/components/SystemPrompt/SystemPromptComponent';
import { Server, Palette } from 'lucide-react'; // Added Server and Palette icons
import { webSocketService } from '@/services/websocket/WebSocketService';
import { useThemeStore } from '@/stores/useThemeStore';

interface InputBarProps {
    selectedModel: string;
    onVoiceInputClick?: () => void;
    inputValue?: string;
    onInputChange?: (value: string) => void;
    activeTools?: string[];
    onManageTools?: () => void;
    onAttachmentChange?: (attachments: Attachment[]) => void;
    disabled?: boolean;
}

declare global {
    interface Window {
        scrollChatToBottom?: () => void;
        getScrollButtonState?: () => boolean;
        appendToPrompt?: (text: string) => boolean;
        setPrompt?: (text: string) => boolean;
        applyRandomTheme?: () => string | undefined;
        theme?: any;
    }
}

export function InputBar({
    selectedModel,
    onVoiceInputClick,
    inputValue,
    onInputChange,
    activeTools: activeToolsFromProps,
    onManageTools,
    onAttachmentChange,
    disabled = false,
}: InputBarProps) {
    const textareaRef = useRef<HTMLTextAreaElement>(null);
    const toolsContainerRef = useRef<HTMLDivElement>(null);



    const [cursorPosition, setCursorPosition] = useState<number | null>(null);
    const [visibleToolCount, setVisibleToolCount] = useState(3);
    const [localInputText, setLocalInputText] = useState('');


    const {
        attachments,
        error: attachmentError,
        addAttachment,
        addAttachments,
        removeAttachment,
        clearAttachments
    } = useAttachmentManager({
        maxCount: 5,
        maxSize: 10 * 1024 * 1024
    });

    const inputText = inputValue ?? localInputText;
    const setInputText = onInputChange || setLocalInputText;
    const { activeTools: activeToolsFromStore, removeActiveTool } = useToolStore();
    const activeTools = activeToolsFromProps || activeToolsFromStore;

    const handleRemoveTool = (toolId: string) => {
        removeActiveTool(toolId);
    };

    const { convPrompts, defaultPromptId, prompts } = useSystemPromptStore();
    const { activeConvId, slctdMsgId, convs, createConv } = useConvStore();
    const { sendMessage, cancelMessage, isLoading } = useChatManagement();
    const { isCancelling, currentRequest, setIsCancelling, setCurrentRequest, isConnected } = useWebSocketStore();
    const { tools } = useToolsManagement();

    const isXs = useMediaQuery('(max-width: 640px)');
    const isSm = useMediaQuery('(max-width: 768px)');
    const isMd = useMediaQuery('(max-width: 1024px)');




    useEffect(() => {
        if (onAttachmentChange) {
            onAttachmentChange(attachments);
        }
    }, [attachments, onAttachmentChange]);



    const handleTextAreaInput = (e: React.ChangeEvent<HTMLTextAreaElement>) => {
        const value = e.target.value;
        setInputText(value);
        setCursorPosition(e.target.selectionStart);

        if (value.startsWith('/') && value.length > 1 && !value.includes(' ') &&
            e.nativeEvent instanceof InputEvent && e.nativeEvent.data === ' ' &&
            handlePromptShortcut(value)) {
            setTimeout(() => {
                const length = textareaRef.current?.value.length;
                textareaRef.current?.setSelectionRange(length, length);
                setCursorPosition(length);
            }, 0);
        }
    };

    const handleTextAreaClick = (e: React.MouseEvent<HTMLTextAreaElement>) => {
        setCursorPosition(e.currentTarget.selectionStart);
    };

    const handleTextAreaKeyUp = (e: React.KeyboardEvent<HTMLTextAreaElement>) => {
        setCursorPosition(e.currentTarget.selectionStart);
    };


    useEffect(() => {
        setVisibleToolCount(isXs ? 1 : isSm ? 2 : isMd ? 3 : 4);

        const observer = new ResizeObserver(() => {
            if (!toolsContainerRef.current) return;
            const containerWidth = toolsContainerRef.current.clientWidth;
            const estimatedToolCapacity = Math.floor(containerWidth / 120);

            let count = Math.max(1, estimatedToolCapacity);
            count = isXs ? Math.min(count, 1) :
                isSm ? Math.min(count, 2) :
                    isMd ? Math.min(count, 3) :
                        Math.min(count, 4);

            count !== visibleToolCount && setVisibleToolCount(count);
        });

        toolsContainerRef.current && observer.observe(toolsContainerRef.current);
        return () => observer.disconnect();
    }, [isXs, isSm, isMd, visibleToolCount]);

    const handleChatMessage = useCallback(async (message: string) => {
        console.log('Sending message with active tools:', activeTools);
        try {
            let convId = activeConvId;
            let parentMessageId = null;
            let systemPromptId = null;
            let systemPromptContent = null;

            if (!convId) {
                convId = `conv_${uuidv4()}`;
                const messageId = `msg_${uuidv4()}`;
                createConv({
                    id: convId,
                    rootMessage: {
                        id: messageId,
                        content: '',
                        source: 'system',
                        timestamp: Date.now(),
                    },
                });
                parentMessageId = messageId;
            } else {
                parentMessageId = slctdMsgId ||
                    (convs[convId]?.messages.length > 0 ?
                        convs[convId].messages[convs[convId].messages.length - 1].id :
                        null);
            }

            if (convId) {
                systemPromptId = convPrompts[convId] || defaultPromptId;
                const prompt = prompts.find(p => p.guid === systemPromptId);
                systemPromptContent = prompt?.content;
            }

            const messageId = `msg_${uuidv4()}`;
            setCurrentRequest({ convId, messageId });
            await sendMessage({
                convId,
                parentMessageId,
                message,
                model: selectedModel,
                toolIds: activeTools,
                systemPromptId,
                systemPromptContent,
                messageId,
                attachments: attachments.length > 0 ? attachments : undefined
            });

            setInputText('');
            clearAttachments();
            setCursorPosition(0);
        } catch (error) {
            console.error('Error sending message:', error);
        }
    }, [
        selectedModel,
        setInputText,
        activeTools,
        convPrompts,
        defaultPromptId,
        prompts,
        sendMessage,
        activeConvId,
        slctdMsgId,
        convs,
        createConv,
        attachments,
        clearAttachments,
        setCurrentRequest // Added setCurrentRequest as dependency
    ]);

    const handleSend = () => {
        console.log("hs1");
        if (isCancelling) return;
        console.log("hs2", isLoading, currentRequest);

        // If we're trying to cancel a current request
        if (isLoading && currentRequest) {
            setIsCancelling(true);
            cancelMessage({
                convId: currentRequest.convId,
                messageId: currentRequest.messageId
            });
            return;
        }

        // Check if WebSocket is disconnected
        if (!isLoading && !useWebSocketStore.getState().isConnected) {
            console.log("WebSocket disconnected, attempting to reconnect");
            webSocketService.connect();
            return; // Don't send the message yet, user will need to press send again
        }

        // Normal sending flow
        if (!isLoading) {
            console.log("hs3");
            // Enable auto-scrolling when sending a message
            window.scrollChatToBottom && window.scrollChatToBottom();

            const textAttachments = attachments.filter(att => att.textContent);
            const textFileContent = formatTextAttachments(textAttachments);
            const fullMessage = (inputText ? inputText : "continue") + textFileContent;
            console.log("HandleSend -> ChatMessage");
            handleChatMessage(fullMessage);
        }
    };

    // Handle cancellation state cleanup
    useEffect(() => {
        if (!isLoading && isCancelling) {
            setIsCancelling(false);
        }
    }, [isLoading, isCancelling, setIsCancelling]);

    const handleKeyDown = (e: KeyboardEvent<HTMLTextAreaElement>) => {
        if (e.key === 'Enter' && e.ctrlKey) {
            e.preventDefault();
            handleSend();
            return;
        }

        if ((e.key === ' ' || e.key === 'Tab') &&
            inputText.startsWith('/') &&
            !inputText.includes(' ') &&
            handlePromptShortcut(inputText)) {
            e.preventDefault();
        }
    };

    useEffect(() => {
        const focusTextarea = (length: number | null) => { // Allow null for length
            if (!textareaRef.current) return;
            textareaRef.current.focus();
            setTimeout(() => {
                if (textareaRef.current) {
                    const len = length ?? textareaRef.current.value.length;
                    textareaRef.current.setSelectionRange(len, len);
                    setCursorPosition(len);
                }
            }, 0);
        };

        const handleAppendToPrompt = (event: CustomEvent<{ text: string }>) => {
            setInputText(text => text + event.detail.text);
            focusTextarea(null);
        };

        const handleSetPrompt = (event: CustomEvent<{ text: string }>) => {
            setInputText(event.detail.text);
            focusTextarea(null);
        };

        window.addEventListener('append-to-prompt', handleAppendToPrompt as EventListener);
        window.addEventListener('set-prompt', handleSetPrompt as EventListener);

        return () => {
            window.removeEventListener('append-to-prompt', handleAppendToPrompt as EventListener);
            window.removeEventListener('set-prompt', handleSetPrompt as EventListener);
        };
    }, [setInputText]); // Added setInputText dependency

    const handlePrimaryModelClick = () =>
        window.dispatchEvent(new CustomEvent('select-primary-model'));


    const handleSecondaryModelClick = () =>
        window.dispatchEvent(new CustomEvent('select-secondary-model'));

    return (
        <div className="InputBar h-[280px] bg-gray-900 border-gray-700/50 shadow-2xl p-3 relative before:content-[''] before:absolute before:top-[-15px] before:left-0 before:right-0 before:h-[15px] before:bg-transparent backdrop-blur-sm"
            style={{
                backgroundColor: "var(--inputbar-bg, #1f2937)",
                color: "var(--inputbar-text-color, #e2e8f0)",
                borderColor: "var(--inputbar-border-color, #4a5568)",
                borderRadius: "var(--inputbar-border-radius, 8px)",
                fontFamily: "var(--inputbar-font-family, inherit)",
                fontSize: "var(--inputbar-font-size, 0.875rem)",
                boxShadow: "var(--inputbar-box-shadow, 0 4px 12px rgba(0,0,0,0.3))",
                ...(window?.theme?.InputBar?.style || {})
            }}
        >
            <div className="flex flex-col h-full">
                {/* System Prompt - Moved to be first child */}
                <div className="mb-2 rounded-lg flex-shrink-0">
                    <SystemPromptComponent
                        convId={activeConvId || undefined}
                        onOpenLibrary={() => window.dispatchEvent(new CustomEvent('open-system-prompt-library'))}
                    />
                </div>
                {/* Middle Section (Textarea, Attachments, Buttons) - Now second child */}
                <div className="flex-1 flex gap-2 overflow-hidden mb-2"> {/* Added mb-2 for spacing */}
                    {/* Textarea Column - Made flex-col */}
                    <div className="relative flex-1 flex flex-col">
                        {/* Textarea - Made flex-1 to grow */}
                        <Textarea
                            ref={textareaRef}
                            className="flex-1 w-full p-4 border rounded-xl resize-none focus:outline-none shadow-inner transition-all duration-200 placeholder:text-gray-400 input-ghost" // Changed h-full to flex-1
                            value={inputText}
                            onChange={handleTextAreaInput}
                            onClick={handleTextAreaClick}
                            onKeyUp={handleTextAreaKeyUp}
                            onKeyDown={handleKeyDown}
                            placeholder="Type your message here... (Ctrl+Enter to send)"
                            disabled={isLoading || disabled} // Reflect outer disabled state
                            showLineCount={true}
                            style={{
                                backgroundColor: 'var(--inputbar-edit-bg, #2d3748)',
                                color: 'var(--inputbar-edit-text-color, #e2e8f0)',
                                fontFamily: 'var(--inputbar-font-family, inherit)',
                                fontSize: 'var(--inputbar-font-size, 0.875rem)',
                                ...(window?.theme?.InputBar?.editAreaStyle || {})
                            }}
                        />
                    </div>


                    {attachments.length > 0 && (
                        <div className="w-14 flex-shrink-0 overflow-auto">
                            <AttachmentPreviewBar
                                attachments={attachments}
                                onRemove={removeAttachment}
                                onClear={clearAttachments}
                                className="h-full"
                                iconsOnly={true}
                                compact={true}
                            />
                        </div>
                    )}

                    <div className="flex flex-col gap-2 justify-end">
                        <FileAttachment
                            onFilesSelected={addAttachments}
                            disabled={isLoading || disabled} // Reflect outer disabled state
                            maxFiles={5}
                            style={{
                                backgroundColor: 'var(--inputbar-button-bg)',
                                borderColor: 'var(--inputbar-button-border)',
                                color: 'var(--inputbar-button-icon-color)',
                                opacity: (isLoading || disabled) ? 'var(--inputbar-button-disabled-opacity)' : 1,
                                ...(window?.theme?.InputBar?.buttonStyle || {})
                            }}
                        />

                        <Button
                            variant="outline"
                            size="icon"
                            onClick={() => useModalStore.getState().openModal('userPrompt')}
                            aria-label="User prompts"
                            disabled={isLoading || disabled} // Reflect outer disabled state
                            style={{
                                backgroundColor: 'var(--inputbar-button-bg)',
                                borderColor: 'var(--inputbar-button-border)',
                                color: 'var(--inputbar-button-icon-color)',
                                opacity: (isLoading || disabled) ? 'var(--inputbar-button-disabled-opacity)' : 1,
                                ...(window?.theme?.InputBar?.buttonStyle || {})
                            }}
                        >
                            <BookMarked className="h-5 w-5" />
                        </Button>

                        {onVoiceInputClick && (
                            <Button
                                variant="outline"
                                size="icon"
                                onClick={onVoiceInputClick}
                                aria-label="Voice input"
                                disabled={isLoading || disabled} // Reflect outer disabled state
                                style={{
                                    backgroundColor: 'var(--inputbar-button-bg)',
                                    borderColor: 'var(--inputbar-button-border)',
                                    color: 'var(--inputbar-button-icon-color)',
                                    opacity: (isLoading || disabled) ? 'var(--inputbar-button-disabled-opacity)' : 1,
                                    ...(window?.theme?.InputBar?.buttonStyle || {})
                                }}
                            >
                                <Mic className="h-5 w-5" />
                            </Button>
                        )}

                        <div className="flex flex-col gap-2">
                            <Button
                                variant="outline"
                                size="icon"
                                onClick={handleSend}
                                aria-label={isLoading ? 'Cancel' : useWebSocketStore.getState().isConnected ? 'Send message' : 'Reconnect and Send'}
                                disabled={isCancelling || disabled} // Reflect outer disabled state
                                title={!useWebSocketStore.getState().isConnected && !isLoading ? 'WebSocket disconnected. Click to reconnect.' : ''}
                                style={{
                                    backgroundColor: isLoading ? '#dc2626' : 'var(--inputbar-button-bg)',
                                    borderColor: !useWebSocketStore.getState().isConnected && !isLoading ? 'red' : 'var(--inputbar-button-border)',
                                    color: 'var(--inputbar-button-icon-color)',
                                    opacity: (isCancelling || disabled) ? 'var(--inputbar-button-disabled-opacity)' : 1,
                                    ...(window?.theme?.InputBar?.buttonStyle || {})
                                }}
                            >
                                {isLoading ? (
                                    <X className="h-5 w-5" />
                                ) : (
                                    <Send className="h-5 w-5" />
                                )}
                            </Button>
                        </div>
                    </div>
                </div>

                {/* Bottom Bar: Model Status, Tools */}
                <div className="pt-2 border-t border-gray-700/30 flex-shrink-0 flex items-center flex-wrap gap-y-1.5"> {/* Added flex-wrap and gap-y */}
                    {/* Model Status */}
                    <div className="flex items-center mr-3 pr-3 border-r border-gray-700/50">
                        <ModelStatusBar
                            onPrimaryClick={handlePrimaryModelClick}
                            onSecondaryClick={handleSecondaryModelClick}
                        />
                    </div>

                    {/* Tools & Servers Wrapper */}
                    <div className="flex items-center gap-4"> {/* New wrapper for Tools and Servers */}
                        {/* Tools Section (border removed, spacing adjusted) */}
                        <div className="flex items-center gap-2">
                            <Button
                                variant="ghost"
                                size="sm"
                                onClick={onManageTools || (() => window.dispatchEvent(new CustomEvent('open-tool-library')))}
                                onMouseDown={(e) => {
                                    if (e.button === 1) { // Middle mouse button
                                        e.preventDefault(); // Prevent default middle-click behavior (e.g., autoscroll)
                                        activeTools.forEach(toolId => removeActiveTool(toolId));
                                    }
                                }}
                            className="h-5 px-2 py-0 text-xs rounded-full bg-gray-600/10 border border-gray-700/20 text-gray-300 hover:bg-gray-600/30 hover:text-gray-100 transition-colors flex-shrink-0 relative"
                                disabled={disabled} // Reflect outer disabled state
                            >
                                <Wrench className="h-3 w-3 mr-1" />
                                <span>Tools</span>
                                {activeTools.length > 0 && (
                                    <span className="ml-1 inline-flex items-center justify-center px-1.5 py-0.5 text-xs font-bold leading-none text-white bg-blue-500 rounded-full" title="Middle-click to clear all">
                                        {activeTools.length}
                                    </span>
                                )}
                            </Button>

                        
                    </div>

                    {/* Theme Button */}
                    <div className="ml-4">
                        <Button
                            variant="ghost"
                            size="sm"
                            onClick={() => {
                                // Get the current theme from window.theme
                                if (window.theme) {
                                    // Add the current theme to the theme store
                                    useThemeStore.getState().addTheme({
                                        name: 'Saved Theme ' + new Date().toLocaleTimeString(),
                                        description: 'Theme saved from UI',
                                        themeJson: window.theme
                                    });
                                    console.log('Theme saved to store');
                                }
                            }}
                            className="h-5 px-2 py-0 text-xs rounded-full bg-gray-600/10 border border-gray-700/20 text-gray-300 hover:bg-gray-600/30 hover:text-gray-100 transition-colors flex-shrink-0"
                            disabled={disabled}
                        >
                            <Palette className="h-3 w-3 mr-1" />
                            <span>Use Theme</span>
                        </Button>
                    </div>

                    </div> {/* Close Tools & Servers Wrapper */}

                </div>
            </div>
        </div>
    );
}

window.appendToPrompt = text => {
    window.dispatchEvent(new CustomEvent('append-to-prompt', { detail: { text } }));
    console.log(`Appended to prompt: "${text}"`);
    return true;
};

// Expose extensive themeable properties for ThemeManager
export const themeableProps = {
  backgroundColor: {
    cssVar: '--inputbar-bg',
    description: 'Input bar background color',
    default: '#1f2937',
  },
  textColor: {
    cssVar: '--inputbar-text-color',
    description: 'Input bar text color',
    default: '#e2e8f0',
  },
  borderColor: {
    cssVar: '--inputbar-border-color',
    description: 'Input bar border color',
    default: '#4a5568',
  },
  borderRadius: {
    cssVar: '--inputbar-border-radius',
    description: 'Input bar border radius',
    default: '8px',
  },
  fontFamily: {
    cssVar: '--inputbar-font-family',
    description: 'Input bar font family',
    default: 'inherit',
  },
  fontSize: {
    cssVar: '--inputbar-font-size',
    description: 'Input bar font size',
    default: '0.875rem',
  },
  boxShadow: {
    cssVar: '--inputbar-box-shadow',
    description: 'Input bar box shadow',
    default: '0 4px 12px rgba(0,0,0,0.3)',
  },
  popupBackground: {
    cssVar: '--inputbar-popup-bg',
    description: 'Popup background color',
    default: '#1a202c',
  },
  popupBorderColor: {
    cssVar: '--inputbar-popup-border-color',
    description: 'Popup border color',
    default: '#4a5568',
  },
  editBackground: {
    cssVar: '--inputbar-edit-bg',
    description: 'Textarea background color',
    default: '#2d3748',
  },
  editTextColor: {
    cssVar: '--inputbar-edit-text-color',
    description: 'Textarea text color',
    default: '#e2e8f0',
  },
  pillActiveBg: {
    cssVar: '--inputbar-pill-active-bg',
    description: 'Active pill background color',
    default: '#2563eb33',
  },
  pillInactiveBg: {
    cssVar: '--inputbar-pill-inactive-bg',
    description: 'Inactive pill background color',
    default: '#4a556822',
  },

  // Button theming for InputBar buttons (send, voice, user prompt, attach)
  buttonBackground: {
    cssVar: '--inputbar-button-bg',
    description: 'Input bar button background color',
    default: '#2d3748',
  },
  buttonHoverBackground: {
    cssVar: '--inputbar-button-hover-bg',
    description: 'Input bar button hover background color',
    default: '#4a5568',
  },
  buttonBorderColor: {
    cssVar: '--inputbar-button-border',
    description: 'Input bar button border color',
    default: '#4a5568',
  },
  buttonIconColor: {
    cssVar: '--inputbar-button-icon-color',
    description: 'Input bar button icon color',
    default: '#e2e8f0',
  },
  buttonDisabledOpacity: {
    cssVar: '--inputbar-button-disabled-opacity',
    description: 'Input bar button disabled opacity',
    default: '0.5',
  },

  style: {
    description: 'Arbitrary CSS style for InputBar root',
    default: {},
  },
  popupStyle: {
    description: 'Arbitrary CSS style for popups',
    default: {},
  },
  pillStyle: {
    description: 'Arbitrary CSS style for pills/buttons',
    default: {},
  },
  editAreaStyle: {
    description: 'Arbitrary CSS style for textarea',
    default: {},
  },
  buttonStyle: {
    description: 'Arbitrary CSS style override for InputBar buttons',
    default: {},
  },
};

window.setPrompt = text => {
    window.dispatchEvent(new CustomEvent('set-prompt', { detail: { text } }));
    console.log(`Set prompt to: "${text}"`);
    return true;
};