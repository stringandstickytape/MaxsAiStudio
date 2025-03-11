import React, { useState, KeyboardEvent, useCallback, useRef, useEffect } from 'react';
import { Button } from '@/components/ui/button';
import { v4 as uuidv4 } from 'uuid';
import { Mic, Send } from 'lucide-react';
import { FileAttachment, AttachedFileDisplay } from './FileAttachment';
import { useToolStore } from '@/stores/useToolStore';
import { useSystemPromptStore } from '@/stores/useSystemPromptStore';
import { useConversationStore } from '@/stores/useConversationStore';
import { useChatManagement } from '@/hooks/useChatManagement';
import { ToolSelector } from './tools/ToolSelector';

interface InputBarProps {
    selectedModel: string;
    onVoiceInputClick?: () => void;
    inputValue?: string;
    onInputChange?: (value: string) => void;
    activeTools?: string[];
    onManageTools?: () => void;
}

export function InputBar({
    selectedModel,
    onVoiceInputClick,
    inputValue,
    onInputChange,
    activeTools: activeToolsFromProps,
    onManageTools
}: InputBarProps) {
    const textareaRef = useRef<HTMLTextAreaElement>(null);



    const [localInputText, setLocalInputText] = useState('');

    const inputText = inputValue !== undefined ? inputValue : localInputText;
    const setInputText = onInputChange || setLocalInputText;

    const { activeTools: activeToolsFromStore } = useToolStore();

    const activeTools = activeToolsFromProps || activeToolsFromStore;

    const { conversationPrompts, defaultPromptId, prompts } = useSystemPromptStore();

    const {
        activeConversationId,
        selectedMessageId,
        conversations,
        createConversation,
        getConversation
    } = useConversationStore();

    const { sendMessage, isLoading } = useChatManagement();

    const [cursorPosition, setCursorPosition] = useState<number | null>(null);

    const handleTextAreaInput = (e: React.ChangeEvent<HTMLTextAreaElement>) => {
        setInputText(e.target.value);
        setCursorPosition(e.target.selectionStart);
    };

    const handleTextAreaClick = (e: React.MouseEvent<HTMLTextAreaElement>) => {
        setCursorPosition(e.currentTarget.selectionStart);
    };

    const handleTextAreaKeyUp = (e: React.KeyboardEvent<HTMLTextAreaElement>) => {
        setCursorPosition(e.currentTarget.selectionStart);
    };

    const handleAttachFile = (file: File, content: string) => {
        const fileName = file.name;


        const textToInsert = `\`\`\`${fileName}\n${content}\n\`\`\`\n`;

        const pos = cursorPosition !== null ? cursorPosition : inputText.length;

        const newText = inputText.substring(0, pos) +
            textToInsert +
            inputText.substring(pos);

        setInputText(newText);

        setTimeout(() => {
            if (textareaRef.current) {
                textareaRef.current.focus();
                const newPosition = pos + textToInsert.length;
                textareaRef.current.setSelectionRange(newPosition, newPosition);
                setCursorPosition(newPosition);
            }
        }, 0);
    };

    const handleChatMessage = useCallback(async (message: string) => {
        console.log('Sending message with active tools:', activeTools);
        try {
            let conversationId = activeConversationId;
            let parentMessageId = null;

            let systemPromptId = null;
            let systemPromptContent = null;

            if (!conversationId) {
                conversationId = `conv_${uuidv4()}`;
                const messageId = `msg_${uuidv4()}`;

                createConversation({
                    id: conversationId,
                    rootMessage: {
                        id: messageId,
                        content: '',
                        source: 'system',
                        timestamp: Date.now()
                    }
                });

                parentMessageId = messageId;
            } else {
                parentMessageId = selectedMessageId;

                if (!parentMessageId) {
                    const conversation = conversations[conversationId];
                    if (conversation && conversation.messages.length > 0) {
                        parentMessageId = conversation.messages[conversation.messages.length - 1].id;
                    }
                }
            }

            if (conversationId) {
                systemPromptId = conversationPrompts[conversationId] || defaultPromptId;

                if (systemPromptId) {
                    const prompt = prompts.find(p => p.guid === systemPromptId);
                    if (prompt) {
                        systemPromptContent = prompt.content;
                    }
                }
            }

            await sendMessage({
                conversationId,
                parentMessageId,
                message,
                model: selectedModel,
                toolIds: activeTools,
                systemPromptId,
                systemPromptContent
            });

            setInputText('');
            setCursorPosition(0);
        } catch (error) {
            console.error('Error sending message:', error);
        }
    }, [
        selectedModel,
        setInputText,
        activeTools,
        conversationPrompts,
        defaultPromptId,
        prompts,
        sendMessage,
        activeConversationId,
        selectedMessageId,
        conversations,
        createConversation,
        getConversation
    ]);

    const handleSend = () => {
        if (inputText.trim() && !isLoading) {
            handleChatMessage(inputText);
        }
    };

    const handleKeyDown = (e: KeyboardEvent<HTMLTextAreaElement>) => {
        if (e.key === 'Enter' && e.ctrlKey) {
            e.preventDefault();
            handleSend();
        }
    };

    useEffect(() => {
        const handleAppendToPrompt = (event: CustomEvent<{ text: string }>) => {
            const textToAppend = event.detail.text;

            setInputText(currentText => currentText + textToAppend);

            if (textareaRef.current) {
                textareaRef.current.focus();

                setTimeout(() => {
                    if (textareaRef.current) {
                        const length = textareaRef.current.value.length;
                        textareaRef.current.setSelectionRange(length, length);
                        setCursorPosition(length);
                    }
                }, 0);
            }
        };

        const handleSetPrompt = (event: CustomEvent<{ text: string }>) => {
            const newText = event.detail.text;

            setInputText(newText);

            if (textareaRef.current) {
                textareaRef.current.focus();

                setTimeout(() => {
                    if (textareaRef.current) {
                        const length = textareaRef.current.value.length;
                        textareaRef.current.setSelectionRange(length, length);
                        setCursorPosition(length);
                    }
                }, 0);
            }
        };

        window.addEventListener('append-to-prompt', handleAppendToPrompt as EventListener);
        window.addEventListener('set-prompt', handleSetPrompt as EventListener);

        return () => {
            window.removeEventListener('append-to-prompt', handleAppendToPrompt as EventListener);
            window.removeEventListener('set-prompt', handleSetPrompt as EventListener);
        };
    }, []);

    return (
        <div className="h-[30vh] bg-gray-900 border-t border-gray-700/50 shadow-2xl p-3 relative before:content-[''] before:absolute before:top-[-15px] before:left-0 before:right-0 before:h-[15px] before:bg-transparent backdrop-blur-sm">
            <div className="h-full flex flex-col">
                <div className="mb-2">
                    <ToolSelector onManageTools={onManageTools || (() => window.dispatchEvent(new CustomEvent('open-tool-panel')))} />
                </div>

                <div className="flex-1 flex gap-2">
                    <div className="relative flex-1">
                        <textarea
                            ref={textareaRef}
                            className="w-full h-full p-4 border border-gray-700/50 rounded-xl resize-none focus:outline-none focus:ring-2 focus:ring-blue-500/50 bg-gray-800/50 text-gray-100 shadow-inner transition-all duration-200 placeholder:text-gray-400"
                            value={inputText}
                            onChange={handleTextAreaInput}
                            onClick={handleTextAreaClick}
                            onKeyUp={handleTextAreaKeyUp}
                            onKeyDown={handleKeyDown}
                            placeholder="Type your message here... (Ctrl+Enter to send)"
                            disabled={isLoading}
                        />
                    </div>

                    <div className="flex flex-col gap-2 justify-end">
                        <FileAttachment
                            onAttach={handleAttachFile}
                            disabled={isLoading}
                        />

                        {onVoiceInputClick && (
                            <Button
                                variant="outline"
                                size="icon"
                                onClick={onVoiceInputClick}
                                className="bg-gray-800 border-gray-700 text-gray-300 hover:text-blue-400 hover:bg-gray-700 transition-colors"
                                aria-label="Voice input"
                                disabled={isLoading}
                            >
                                <Mic className="h-5 w-5" />
                            </Button>
                        )}

                        <Button
                            variant="outline"
                            size="icon"
                            onClick={handleSend}
                            className="bg-blue-600 hover:bg-blue-700 text-white border-blue-500 transition-colors"
                            aria-label="Send message"
                            disabled={isLoading}
                        >
                            <Send className="h-5 w-5" />
                        </Button>
                    </div>
                </div>
            </div>
        </div>
    );
}

window.appendToPrompt = function (text) {
    const appendEvent = new CustomEvent('append-to-prompt', {
        detail: { text: text }
    });

    window.dispatchEvent(appendEvent);

    console.log(`Appended to prompt: "${text}"`);

    return true;
};

window.setPrompt = function (text) {
    const setEvent = new CustomEvent('set-prompt', {
        detail: { text: text }
    });

    window.dispatchEvent(setEvent);

    console.log(`Set prompt to: "${text}"`);

    return true;
};