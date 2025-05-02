// AiStudioClient/src/components/InputBar/InputBar.tsx
import React, { useState, useRef, useEffect, useCallback } from 'react';
import { v4 as uuidv4 } from 'uuid';
import { useModalStore } from '@/stores/useModalStore';
import { useToolStore } from '@/stores/useToolStore';
import { useSystemPromptStore } from '@/stores/useSystemPromptStore';
import { useToolsManagement } from '@/hooks/useToolsManagement';
import { useConvStore } from '@/stores/useConvStore';
import { useWebSocketStore } from '@/stores/useWebSocketStore';
import { useMediaQuery } from '@/hooks/useMediaQuery';
import { useAttachmentManager } from '@/hooks/useAttachmentManager';
import { formatTextAttachments } from '@/utils/attachmentUtils';
import { webSocketService } from '@/services/websocket/WebSocketService';
import { useMcpServerStore } from '@/stores/useMcpServerStore';
import { useJumpToEndStore } from '@/stores/useJumpToEndStore';
import { windowEventService, WindowEvents } from '@/services/windowEvents';
import { useChatManagement } from '@/hooks/useChatManagement';

// Import subcomponents
import { SystemPromptSection } from './SystemPromptSection';
import { MessageInputArea, MessageInputAreaRef } from './MessageInputArea';
import { AttachmentSection } from './AttachmentSection';
import { ActionButtons } from './ActionButtons';
import { ToolsSection } from './ToolsSection';
import { ModelStatusSection } from './ModelStatusSection';
import { StatusSection } from './StatusSection';

/*
 * InputBar.tsx
 * React component for user input, attachments, and controls in AI Studio Web.
 * Handles message sending, cancellation, and UI interactions.
 */

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
        getScrollButtonState?: () => boolean;
        appendToPrompt?: (text: string) => boolean;
        setPrompt?: (text: string) => boolean;
        scrollConversationToBottom?: () => void;
        getScrollBottomState?: () => boolean;
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
    const textareaRef = useRef<MessageInputAreaRef>(null);
    const toolsContainerRef = useRef<HTMLDivElement>(null);

    const [cursorPosition, setCursorPosition] = useState<number | null>(null);
    const [visibleToolCount, setVisibleToolCount] = useState(3);
    const [localInputText, setLocalInputText] = useState('');
    const [isAtBottom, setIsAtBottom] = useState(true);

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

    // MCP server store for enabled count badge
    const { fetchServers } = useMcpServerStore();

    const { convPrompts, defaultPromptId, prompts } = useSystemPromptStore();
    const { activeConvId, slctdMsgId, convs, createConv } = useConvStore();
    const { sendMessage, cancelMessage, isLoading } = useChatManagement();
    const { isCancelling, currentRequest, setIsCancelling, setCurrentRequest, isConnected } = useWebSocketStore();

    const isXs = useMediaQuery('(max-width: 640px)');
    const isSm = useMediaQuery('(max-width: 768px)');
    const isMd = useMediaQuery('(max-width: 1024px)');

    useEffect(() => {
        if (onAttachmentChange) {
            onAttachmentChange(attachments);
        }
    }, [attachments, onAttachmentChange]);
    
    // Check scroll position periodically to update button visibility
    useEffect(() => {
        const checkScrollPosition = () => {
            if (window.getScrollBottomState) {
                setIsAtBottom(window.getScrollBottomState());
            }
        };
        
        // Check immediately
        checkScrollPosition();
        
        // Set up interval to check periodically
        const intervalId = setInterval(checkScrollPosition, 500);
        
        return () => clearInterval(intervalId);
    }, []);

    // Fetch MCP servers once on mount to populate enabled count badge
    useEffect(() => {
        fetchServers();
    }, [fetchServers]);

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
        setCurrentRequest
    ]);

    const handleSend = () => {
        
        if (isCancelling) return;
        

        // If we're trying to cancel a current request
        if (isLoading && currentRequest) {
            setIsCancelling(true);
            // --- Immediately clear stream and ignore tokens ---
            windowEventService.emit(WindowEvents.STREAM_IGNORE);
            (async () => {
                const result = await cancelMessage({
                    convId: currentRequest.convId,
                    messageId: currentRequest.messageId
                });
                if (result) {
                    windowEventService.emit(WindowEvents.REQUEST_CANCELLED);
                } else {
                    // Optionally show a toast or error message here
                    console.error('Cancellation failed.');
                }
            })();
            return;
        }

        // Check if WebSocket is disconnected
        if (!isLoading && !useWebSocketStore.getState().isConnected) {
            
            webSocketService.connect();
            return; // Don't send the message yet, user will need to press send again
        }

        // Normal sending flow
        if (!isLoading) {
            // --- Allow stream tokens again on new send ---
            windowEventService.emit(WindowEvents.STREAM_ALLOW);
            

            const textAttachments = attachments.filter(att => att.textContent);
            const textFileContent = formatTextAttachments(textAttachments);
            const fullMessage = (inputText ? inputText : "continue") + textFileContent;
            
            handleChatMessage(fullMessage);
        }
    };

    // Handle cancellation state cleanup
    useEffect(() => {
        if (!isLoading && isCancelling) {
            setIsCancelling(false);
        }
    }, [isLoading, isCancelling, setIsCancelling]);

    useEffect(() => {
        const handleAppendToPrompt = (data: { text: string }) => {
            setInputText(text => text + data.text);
            textareaRef.current?.focusWithCursor();
        };

        const handleSetPrompt = (data: { text: string }) => {
            setInputText(data.text);
            textareaRef.current?.focusWithCursor();
        };

        const unsubAppend = windowEventService.on(WindowEvents.APPEND_TO_PROMPT, handleAppendToPrompt);
        const unsubSet = windowEventService.on(WindowEvents.SET_PROMPT, handleSetPrompt);

        return () => {
            unsubAppend();
            unsubSet();
        };
    }, [setInputText]);

    return (
        <div className="InputBar h-[280px] bg-gray-900 border-gray-700/50 shadow-2xl p-3 relative before:content-[''] before:absolute before:top-[-15px] before:left-0 before:right-0 before:h-[15px] before:bg-transparent backdrop-blur-sm"
            style={{
                backgroundColor: "var(--inputbar-bg, var(--global-background-color, #1f2937))",
                color: "var(--inputbar-text-color, var(--global-text-color, #e2e8f0))",
                borderColor: "var(--inputbar-border-color, var(--global-border-color, #4a5568))",
                borderRadius: "var(--inputbar-border-radius, var(--global-border-radius, 8px))",
                boxShadow: "var(--inputbar-box-shadow, var(--global-box-shadow, 0 4px 12px rgba(0,0,0,0.3)))",
                fontFamily: "var(--inputbar-font-family, var(--global-font-family, inherit))",
                fontSize: "var(--inputbar-font-size, var(--global-font-size, inherit))",
                ...(window?.theme?.InputBar?.style || {})
            }}
        >
            {/* Scroll to Bottom Button */}
            <StatusSection isAtBottom={isAtBottom} disabled={disabled} />

            <div className="flex flex-col h-full">
                {/* System Prompt Section */}
                <SystemPromptSection activeConvId={activeConvId} />
                
                {/* Middle Section (Textarea, Attachments, Buttons) */}
                <div className="flex-1 flex gap-2 overflow-hidden mb-2">
                    {/* Textarea Column */}
                    <MessageInputArea
                        ref={textareaRef}
                        inputText={inputText}
                        setInputText={setInputText}
                        onSend={handleSend}
                        isLoading={isLoading}
                        disabled={disabled}
                        onCursorPositionChange={setCursorPosition}
                    />

                    {/* Attachments Section */}
                    <AttachmentSection
                        attachments={attachments}
                        removeAttachment={removeAttachment}
                        clearAttachments={clearAttachments}
                    />

                    {/* Action Buttons */}
                    <ActionButtons
                        onSend={handleSend}
                        onVoiceInputClick={onVoiceInputClick}
                        addAttachments={addAttachments}
                        isLoading={isLoading}
                        isCancelling={isCancelling}
                        disabled={disabled}
                    />
                </div>

                {/* Bottom Bar: Model Status, Tools, Servers */}
                <div className="pt-2 border-t border-gray-700/30 flex-shrink-0 flex items-center flex-wrap gap-y-1.5">
                    {/* Model Status */}
                    <ModelStatusSection />

                    {/* Tools Section */}
                    <div ref={toolsContainerRef}>
                        <ToolsSection
                            activeTools={activeTools}
                            removeActiveTool={removeActiveTool}
                            disabled={disabled}
                        />
                    </div>
                </div>
            </div>
        </div>
    );
}

window.appendToPrompt = text => {
    windowEventService.emit(WindowEvents.APPEND_TO_PROMPT, { text });
    return true;
};

window.setPrompt = text => {
    windowEventService.emit(WindowEvents.SET_PROMPT, { text });
    return true;
};

// Expose simplified themeable properties for ThemeManager
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
  accentColor: {
    cssVar: '--inputbar-accent-color',
    description: 'Input bar accent color for highlights and active elements',
    default: '#3b82f6',
  },
  // Additional properties needed for this component's unique features
  editBackground: {
    cssVar: '--inputbar-edit-bg',
    description: 'Textarea background color',
    default: '#2d3748',
  },
  buttonBackground: {
    cssVar: '--inputbar-button-bg',
    description: 'Input bar button background color',
    default: '#2d3748',
  },
  style: {
    description: 'Arbitrary CSS style for InputBar root',
    default: {},
  },
};