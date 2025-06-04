// AiStudioClient/src/components/InputBar/InputBar.tsx
import React, { useState, useRef, useEffect, useCallback } from 'react';
import { v4 as uuidv4 } from 'uuid';
import { StatusMessage } from '@/components/StatusMessage';
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
import { useVoiceInputStore } from '@/stores/useVoiceInputStore'; // Added
import { useVoiceInput } from '@/hooks/useVoiceInput'; // Added
import { useToast } from "@/hooks/use-toast"; // Added

// Import subcomponents
import { SystemPromptSection } from './SystemPromptSection';
import { MessageInputArea, MessageInputAreaRef } from './MessageInputArea';
import { AttachmentSection } from './AttachmentSection';
import { ActionButtons } from './ActionButtons';
import { ToolsButton } from './ToolsButton';
import { MCPServersButton } from './MCPServersButton';
import { PrimaryModelButton } from './PrimaryModelButton';
import { SecondaryModelButton } from './SecondaryModelButton';
import { TemperatureControl } from './TemperatureControl';
import { TopPControl } from './TopPControl';

/*
 * InputBar.tsx
 * React component for user input, attachments, and controls in AI Studio Web.
 * Handles message sending, cancellation, and UI interactions.
 */

interface InputBarProps {
    selectedModel: string;

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

    // Voice Input Integration
    const { 
        isListening: isVoiceListening, // Renamed to avoid conflict if local isListening is ever needed
        error: voiceError, 
        startListening: startVoiceStoreListening, 
        stopListening: stopVoiceStoreListening 
    } = useVoiceInputStore();
    
    const { toast } = useToast();

    const handleFinalVoiceTranscript = useCallback((text: string) => {
        setInputText(prevText => (prevText ? prevText + ' ' : '') + text.trim());
        stopVoiceStoreListening(); // Signal store to stop, effect will handle mic
        // resetTranscript(); // resetTranscript is called by the effect before starting new capture
        textareaRef.current?.focusWithCursor();
    }, [setInputText, stopVoiceStoreListening, textareaRef]);

    const {
        isSupported: voiceIsSupported,
        startMicCapture,
        stopMicCapture,
        resetTranscript
    } = useVoiceInput({ onTranscriptFinalized: handleFinalVoiceTranscript });

    const handleToggleListening = () => {
        if (isVoiceListening) {
            stopVoiceStoreListening();
        } else {
            // resetTranscript(); // This will be called by the effect when isListening becomes true
            startVoiceStoreListening();
        }
    };

    // Effect to link store's isListening state to the hook's capture functions
    useEffect(() => {
        if (isVoiceListening) {
            if (voiceIsSupported) {
                resetTranscript(); // Clear any old transcript in the hook before starting
                startMicCapture(); // Start the actual browser API listening
            } else {
                useVoiceInputStore.getState().setError("Voice input not supported by your browser.");
                stopVoiceStoreListening(); // Reset store state if not supported
            }
        } else {
            stopMicCapture(); // Stop the actual browser API listening
        }
    }, [isVoiceListening, voiceIsSupported, startMicCapture, stopMicCapture, resetTranscript, stopVoiceStoreListening]);

    // Effect to display voice errors via toast
    useEffect(() => {
        if (voiceError) {
            toast({ title: "Voice Input Error", description: voiceError, variant: "destructive" });
            useVoiceInputStore.getState().setError(null); // Clear error after showing
        }
    }, [voiceError, toast]);

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

    const handleChatMessage = useCallback(async (message: string, messageAttachments?: Attachment[]) => {
        
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
                attachments: messageAttachments && messageAttachments.length > 0 ? messageAttachments : undefined
            });
            setCursorPosition(0);
        } catch (error) {
            console.error('Error sending message:', error);
        } finally {
            // Clear currentRequest when the entire operation completes (success or error)
            setCurrentRequest(undefined);
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

    const handleSend = async () => {
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
            await handleChatMessage(fullMessage, messageAttachments);
            useAttachmentStore.getState().clearStagedAttachments();
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
        <div className="InputBar h-full bg-gray-900 border-gray-700/50 shadow-2xl p-2 relative before:content-[''] before:absolute before:top-[-15px] before:left-0 before:right-0 before:h-[15px] before:bg-transparent backdrop-blur-sm"
            style={{
                backgroundColor: "var(--global-background-color, #1f2937)",
                color: "var(--global-text-color, #e2e8f0)",
                borderColor: "var(--global-border-color, #4a5568)",
                borderRadius: "var(--global-border-radius, 8px)",
                fontFamily: "var(--global-font-family, inherit)",
                fontSize: "var(--global-font-size, inherit)",
                ...(window?.theme?.InputBar?.style || {})
            }}
        >

            <div className="flex flex-col h-full">
                {/* Status Message */}
                <div className="flex-shrink-0 mb-2">
                    <StatusMessage />
                </div>
                
                {/* System Prompt Section */}
                <SystemPromptSection activeConvId={activeConvId} />
                
                {/* Middle Section (Textarea and Attachments) */}
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
                </div>

                {/* Action Buttons Bar with Controls */}
                <div className="flex-shrink-0 flex justify-between items-start mb-2">
                    {/* Left side: 3x2 grid for controls */}
                    <div
                        className="grid gap-2"
                        style={{
                            gridTemplateColumns: 'repeat(3, max-content)',
                            gridTemplateRows: 'repeat(2, auto)',
                            alignItems: 'center',
                        }}
                    >
                        {/* Row 1 */}
                        <PrimaryModelButton />
                        <ToolsButton
                            activeTools={activeTools}
                            removeActiveTool={removeActiveTool}
                            disabled={disabled}
                        />
                        <TemperatureControl />
                        {/* Row 2 */}
                        <SecondaryModelButton />
                        <MCPServersButton disabled={disabled} />
                        <TopPControl />
                    </div>

                    {/* Right side: Action Buttons */}
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
                        isListening={isVoiceListening}
                        onToggleListening={handleToggleListening}
                        addAttachments={addAttachments}
                        isLoading={isLoading}
                        isCancelling={isCancelling}
                        disabled={disabled}
                        inputText={inputText}
                        setInputText={setInputText}
                        messageSent={!!currentRequest}
                    />
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