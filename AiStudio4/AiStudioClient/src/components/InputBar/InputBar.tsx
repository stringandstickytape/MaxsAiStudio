// AiStudio4/AiStudioClient/src/components/InputBar/InputBar.tsx
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
import { windowEventService, WindowEvents } from '@/services/windowEvents';
import { useChatManagement } from '@/hooks/useChatManagement';
import { useVoiceInputStore } from '@/stores/useVoiceInputStore';
import { useVoiceInput } from '@/hooks/useVoiceInput';
import { useToast } from "@/hooks/use-toast";
import { useInputBarStore, useInputText } from '@/stores/useInputBarStore';

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

interface InputBarProps {
    onManageTools?: () => void;
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
    onManageTools,
    disabled = false,
}: InputBarProps) {
    const textareaRef = useRef<MessageInputAreaRef>(null);
    const inputText = useInputText();

    const {
        stagedAttachments: attachments,
        addStagedAttachment: addAttachment,
    } = useAttachmentStore();
    const { activeTools, removeActiveTool } = useToolStore();

    const { fetchServers } = useMcpServerStore();
    const { convPrompts, defaultPromptId, prompts } = useSystemPromptStore();
    const { activeConvId, slctdMsgId, convs, createConv } = useConvStore();
    const { sendMessage, cancelMessage, isLoading } = useChatManagement();
    const { isCancelling, currentRequest, setIsCancelling, setCurrentRequest } = useWebSocketStore();
    const { isListening: isVoiceListening, error: voiceError, startListening: startVoiceStoreListening, stopListening: stopVoiceStoreListening } = useVoiceInputStore();
    const { toast } = useToast();

    const handleFinalVoiceTranscript = useCallback((text: string) => {
        useInputBarStore.getState().appendToInputText(text.trim());
        stopVoiceStoreListening();
        textareaRef.current?.focusWithCursor();
    }, [stopVoiceStoreListening]);

    const { isSupported: voiceIsSupported, startMicCapture, stopMicCapture, resetTranscript } = useVoiceInput({ onTranscriptFinalized: handleFinalVoiceTranscript });

    const handleToggleListening = () => {
        if (isVoiceListening) {
            stopVoiceStoreListening();
        } else {
            startVoiceStoreListening();
        }
    };

    useEffect(() => {
        if (isVoiceListening) {
            if (voiceIsSupported) {
                resetTranscript();
                startMicCapture();
            } else {
                useVoiceInputStore.getState().setError("Voice input not supported by your browser.");
                stopVoiceStoreListening();
            }
        } else {
            stopMicCapture();
        }
    }, [isVoiceListening, voiceIsSupported, startMicCapture, stopMicCapture, resetTranscript, stopVoiceStoreListening]);

    useEffect(() => {
        if (voiceError) {
            toast({ title: "Voice Input Error", description: voiceError, variant: "destructive" });
            useVoiceInputStore.getState().setError(null);
        }
    }, [voiceError, toast]);


    useEffect(() => {
        fetchServers();
    }, [fetchServers]);

    const handleChatMessage = useCallback(async (message: string, messageAttachments?: Attachment[]) => {
        try {
            // Get latest state directly from stores AT EXECUTION TIME
            let { activeConvId: currentConvId, createConv: createConvAction } = useConvStore.getState();
            const { convPrompts, defaultPromptId, prompts } = useSystemPromptStore.getState();

            let convId = currentConvId;
            let systemPromptId = null;
            let systemPromptContent = null;

            if (!convId) {
                convId = `conv_${uuidv4()}`;
                const messageId = `msg_${uuidv4()}`;
                createConvAction({
                    id: convId,
                    rootMessage: { id: messageId, content: '', source: 'system', timestamp: Date.now() },
                });
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
                message,
                systemPromptId,
                systemPromptContent,
                messageId,
                attachments: messageAttachments && messageAttachments.length > 0 ? messageAttachments : undefined
            });

        } catch (error) {
            console.error('Error sending message:', error);
        } finally {
            setCurrentRequest(undefined);
        }
    }, [sendMessage, setCurrentRequest]);

    const handleSend = async () => {
        if (isCancelling) return;

        if (!isLoading && !useWebSocketStore.getState().isConnected) {
            webSocketService.connect();
            return;
        }

        if (!isLoading) {
            windowEventService.emit(WindowEvents.STREAM_ALLOW);

            // Get the current input text directly from the store to avoid stale closure
            const currentInputText = useInputBarStore.getState().inputText;

            const textAttachments = useAttachmentStore.getState().stagedAttachments.filter(att => att.textContent);

            const textFileContent = formatTextAttachments(textAttachments);
            const fullMessage = (currentInputText ? currentInputText : "continue") + textFileContent;

            const messageAttachments = useAttachmentStore.getState().getStagedAttachments().filter(att => !(att.textContent));

            await handleChatMessage(fullMessage, messageAttachments);
            useAttachmentStore.getState().clearStagedAttachments();
            useInputBarStore.getState().clearInputText();
        }
    };

    // Handle cancellation state cleanup
    useEffect(() => {
        if (!isLoading && isCancelling) {
            setIsCancelling(false);
        }
    }, [isLoading, isCancelling, setIsCancelling]);


    return (
        <div className="InputBar bg-gray-900 border-gray-700/50 shadow-2xl p-2 relative before:content-[''] before:absolute before:top-[-15px] before:left-0 before:right-0 before:h-[15px] before:bg-transparent backdrop-blur-sm"
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
            {/* ... JSX remains the same ... */}
            <div className="flex flex-col">
                <div className="flex-shrink-0 mb-2">
                    <StatusMessage />
                </div>
                <SystemPromptSection activeConvId={activeConvId} />
                <div className="flex flex-col gap-2 mb-2">
                    <MessageInputArea
                        ref={textareaRef}
                        onSend={handleSend}
                        isLoading={isLoading}
                        disabled={disabled}
                        onCursorPositionChange={() => { }}
                        onAttachFile={addAttachment}
                    />
                    <AttachmentSection />
                </div>
                <div className="flex justify-between items-start mb-2">
                    <div
                        className="grid gap-2 mr-4"
                        style={{
                            gridTemplateColumns: 'repeat(3, minmax(0, max-content))',
                            gridTemplateRows: 'repeat(2, auto)',
                            alignItems: 'center',
                            flexShrink: 1,
                            minWidth: 0,
                        }}
                    >
                        <PrimaryModelButton />
                        <ToolsButton
                            activeTools={activeTools}
                            removeActiveTool={removeActiveTool}
                            disabled={disabled}
                        />
                        <TemperatureControl />
                        <SecondaryModelButton />
                        <MCPServersButton disabled={disabled} />
                        <TopPControl />
                    </div>
                    <ActionButtons
                        onSend={handleSend}
                        onCancel={() => {
                            if (isLoading && currentRequest) {
                                setIsCancelling(true);
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
                        isLoading={isLoading}
                        isCancelling={isCancelling}
                        disabled={disabled}
                        inputText={inputText}
                        messageSent={!!currentRequest}
                    />
                </div>
            </div>
        </div>
    );
}


export const themeableProps = {};