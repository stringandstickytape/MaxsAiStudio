﻿// AiStudioClient/src/components/InputBar/InputBar.tsx
import React, { useState, useRef, useEffect, useCallback } from 'react';
import { v4 as uuidv4 } from 'uuid';
import { useModalStore } from '@/stores/useModalStore';
import { useToolStore } from '@/stores/useToolStore';
import { useSystemPromptStore } from '@/stores/useSystemPromptStore';
import { useToolsManagement } from '@/hooks/useToolsManagement';
import { useConvStore } from '@/stores/useConvStore';
import { useWebSocketStore } from '@/stores/useWebSocketStore';
import { useMediaQuery } from '@/hooks/useMediaQuery';
import { useAttachmentStore } from '@/stores/useAttachmentStore';
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
import { TemperatureControl } from './TemperatureControl'; // Add this

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
        stagedAttachments: attachments,
        attachmentErrors,
        addStagedAttachment: addAttachment,
        addStagedAttachments: addAttachments,
        removeStagedAttachment: removeAttachment,
        clearStagedAttachments: clearAttachments
    } = useAttachmentStore();

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
    
    // Fetch MCP servers once on mount to populate enabled count badge
    useEffect(() => {
        fetchServers();
    }, [fetchServers]);

    useEffect(() => {
        setVisibleToolCount(isXs ? 1 : isSm ? 2 : isMd ? 3 : 4);
    }, [isXs, isSm, isMd]);

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
                attachments: attachments.length > 0 ? useAttachmentStore.getState().getStagedAttachments() : undefined
            });
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
            
            // Get attachments from store and pass them to handleChatMessage
            const messageAttachments = useAttachmentStore.getState().getStagedAttachments();
            handleChatMessage(fullMessage);
            setInputText('');
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
                backgroundColor: "var(--global-background-color, #1f2937)",
                color: "var(--global-text-color, #e2e8f0)",
                borderColor: "var(--global-border-color, #4a5568)",
                borderRadius: "var(--global-border-radius, 8px)",
                boxShadow: "var(--global-box-shadow, 0 4px 12px rgba(0,0,0,0.3))",
                fontFamily: "var(--global-font-family, inherit)",
                fontSize: "var(--global-font-size, inherit)",
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
                        onAttachFile={addAttachment}
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
                        onCancel={() => {
                            if (isLoading && currentRequest) {
                                setIsCancelling(true);
                                // Immediately clear stream and ignore tokens
                                windowEventService.emit(WindowEvents.STREAM_IGNORE);
                                (async () => {
                                    const result = await cancelMessage({
                                        convId: currentRequest.convId,
                                        messageId: currentRequest.messageId
                                    });
                                    if (result) {
                                        windowEventService.emit(WindowEvents.REQUEST_CANCELLED);
                                    } else {
                                        console.error('Cancellation failed.');
                                    }
                                })();
                            }
                        }}
                        onVoiceInputClick={onVoiceInputClick}
                        addAttachments={addAttachments}
                        isLoading={isLoading}
                        isCancelling={isCancelling}
                        disabled={disabled}
                        inputText={inputText}
                        setInputText={setInputText}
                        messageSent={!!currentRequest}
                    />
                </div>

                {/* Bottom Bar: Model Status, Tools, Servers, Temperature */}
                <div className="pt-2 border-t border-gray-700/30 flex-shrink-0 flex items-center flex-wrap gap-y-1.5 gap-x-4"> {/* Added gap-x-4 for spacing */}
                    {/* Model Status */}
                    <ModelStatusSection />

                    {/* Tools Section */}
                    <div ref={toolsContainerRef}> {/* Keep ref if used for tool count logic */}
                        <ToolsSection
                            activeTools={activeTools}
                            removeActiveTool={removeActiveTool}
                            disabled={disabled}
                        />
                    </div>

                    {/* NEW: Temperature Control Section */}
                    <TemperatureControl /> {/* Add this line */}

                    {/* Status Message Section - Placed at the end for right alignment */}
                    <div className="ml-auto"> {/* This will push StatusMessage to the right */}
                        <StatusSection isAtBottom={isAtBottom} disabled={disabled} />
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
};