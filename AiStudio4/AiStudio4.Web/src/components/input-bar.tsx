// src/components/input-bar.tsx
import React, { useState, KeyboardEvent, useCallback } from 'react';
import { Button } from '@/components/ui/button';
import { v4 as uuidv4 } from 'uuid';
import { createConversation } from '@/store/conversationSlice';
import { useSelector, useDispatch } from 'react-redux';
import { RootState } from '@/store/store';
import { ToolSelector } from './tools/ToolSelector';
import { Mic, Send } from 'lucide-react';
import { useSendMessageMutation } from '@/services/api/chatApi';

interface InputBarProps {
    selectedModel: string;
    onVoiceInputClick?: () => void;
    inputValue?: string;
    onInputChange?: (value: string) => void;
}

export function InputBar({
    selectedModel,
    onVoiceInputClick,
    inputValue,
    onInputChange
}: InputBarProps) {
    const dispatch = useDispatch();

    // If props are provided, use them, otherwise use local state
    const [localInputText, setLocalInputText] = useState('');

    // Use either the props or local state
    const inputText = inputValue !== undefined ? inputValue : localInputText;
    const setInputText = onInputChange || setLocalInputText;

    // Get active tools and system prompts from Redux store
    const activeTools = useSelector((state: RootState) => state.tools.activeTools);
    const { conversationPrompts, defaultPromptId, prompts } = useSelector((state: RootState) => state.systemPrompts);

    // Use the sendMessage mutation from RTK Query
    const [sendMessage, { isLoading }] = useSendMessageMutation();

    const handleChatMessage = useCallback(async (message: string) => {
        console.log('Sending message with active tools:', activeTools);
        try {
            const state = store.getState();
            let conversationId = state.conversations.activeConversationId;

            // Determine which system prompt to use
            let systemPromptId = null;
            let systemPromptContent = null;

            // If no active conversation, create a new one
            if (!conversationId) {
                conversationId = `conv_${uuidv4()}`;
                dispatch(createConversation({
                    id: conversationId,
                    rootMessage: {
                        id: `msg_${uuidv4()}`,
                        content: '',
                        source: 'system',
                        timestamp: Date.now()
                    }
                }));
            }

            const parentMessageId = state.conversations.selectedMessageId ||
                state.conversations.conversations[conversationId]?.messages[0]?.id ||
                `msg_${uuidv4()}`;

            // Determine which system prompt to use for this conversation
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

            // Clear the input after sending
            setInputText('');
        } catch (error) {
            console.error('Error sending message:', error);
        }
    }, [selectedModel, setInputText, activeTools, conversationPrompts, defaultPromptId, prompts, sendMessage, dispatch]);

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

    return (
        <div className="h-[30vh] bg-gradient-to-b from-gray-900 to-gray-800 border-t border-gray-700/50 shadow-2xl p-6 relative before:content-[''] before:absolute before:top-[-15px] before:left-0 before:right-0 before:h-[15px] before:bg-gradient-to-t before:from-gray-900 before:to-transparent backdrop-blur-sm">
            <div className="h-full flex flex-col gap-2">
                {/* Tool selector */}
                <div className="mb-2">
                    <ToolSelector onManageTools={() => window.dispatchEvent(new CustomEvent('open-tool-panel'))} />
                </div>

                <div className="flex-1 flex gap-2">
                    {/* Input area */}
                    <div className="relative flex-1">
                        <textarea
                            className="w-full h-full p-4 border border-gray-700/50 rounded-xl resize-none focus:outline-none focus:ring-2 focus:ring-blue-500/50 bg-gray-800/50 text-gray-100 shadow-inner transition-all duration-200 placeholder:text-gray-400"
                            value={inputText}
                            onChange={(e) => setInputText(e.target.value)}
                            onKeyDown={handleKeyDown}
                            placeholder="Type your message here... (Ctrl+Enter to send)"
                            disabled={isLoading}
                        />
                    </div>

                    {/* Vertical button bar */}
                    <div className="flex flex-col gap-2 justify-end">
                        {/* Voice input button */}
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

                        {/* Send button */}
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