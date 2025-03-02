import React, { useState, KeyboardEvent, useCallback } from 'react';
import { Button } from '@/components/ui/button';
import { ChatService } from '@/services/ChatService';
import { store } from '@/store/store';
import { v4 as uuidv4 } from 'uuid';
import { createConversation } from '@/store/conversationSlice';
import { useSelector } from 'react-redux';
import { RootState } from '@/store/store';
import { ToolSelector } from './tools/ToolSelector';
import { Mic, Tool } from 'lucide-react';

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
    // If props are provided, use them, otherwise use local state
    const [localInputText, setLocalInputText] = useState('');

    // Use either the props or local state
    const inputText = inputValue !== undefined ? inputValue : localInputText;
    const setInputText = onInputChange || setLocalInputText;

    // Get active tools from Redux store
    const activeTools = useSelector((state: RootState) => state.tools.activeTools);

    const handleChatMessage = useCallback(async (message: string) => {
        console.log('Sending message with active tools:', activeTools);
        try {
            const state = store.getState();
            let conversationId = state.conversations.activeConversationId;

            // If no active conversation, create a new one
            if (!conversationId) {
                conversationId = `conv_${uuidv4()}`;
                store.dispatch(createConversation({
                    id: conversationId,
                    rootMessage: {
                        id: `msg_${uuidv4()}`,
                        content: '',
                        source: 'system',
                        timestamp: Date.now()
                    }
                }));
            }

            const messageId = `msg_${uuidv4()}`;
            const parentMessageId = state.conversations.conversationHistory?.[conversationId]?.lastMessageId
                || state.conversations.conversations?.[conversationId]?.rootMessage?.id
                || `msg_${uuidv4()}`;

            await ChatService.sendMessage(message, selectedModel, activeTools);

            // Clear the input after sending
            setInputText('');
        } catch (error) {
            console.error('Error sending message:', error);
        }
    }, [selectedModel, setInputText, activeTools]);

    const handleSend = () => {
        if (inputText.trim()) {
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
                    <ToolSelector onManageTools={() => {}} />
                </div>
                
                <div className="relative flex-1">
                    <textarea
                        className="w-full h-full p-4 border border-gray-700/50 rounded-xl resize-none focus:outline-none focus:ring-2 focus:ring-blue-500/50 bg-gray-800/50 text-gray-100 shadow-inner transition-all duration-200 placeholder:text-gray-400"
                        value={inputText}
                        onChange={(e) => setInputText(e.target.value)}
                        onKeyDown={handleKeyDown}
                        placeholder="Type your message here... (Ctrl+Enter to send)"
                    />

                    {/* Add voice input button */}
                    {onVoiceInputClick && (
                        <Button
                            variant="ghost"
                            size="icon"
                            onClick={onVoiceInputClick}
                            className="absolute right-3 bottom-3 text-gray-400 hover:text-blue-400 hover:bg-gray-700/50"
                            aria-label="Voice input"
                        >
                            <Mic className="h-5 w-5" />
                        </Button>
                    )}
                </div>
                <Button
                    className="w-full bg-gradient-to-r from-blue-600 to-blue-700 hover:from-blue-700 hover:to-blue-800 text-white font-semibold shadow-lg hover:shadow-xl transition-all duration-200 transform hover:-translate-y-0.5 rounded-xl"
                    onClick={handleSend}
                >
                    Send
                </Button>
            </div>
        </div>
    );
}