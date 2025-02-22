import React, { useState, KeyboardEvent, useCallback } from 'react';
import { Button } from '@/components/ui/button';
import { ChatService } from '@/services/ChatService';
import { store } from '@/store/store';
import { v4 as uuidv4 } from 'uuid';
import { createConversation } from '@/store/conversationSlice';

interface InputBarProps {
    selectedModel: string;
}


export function InputBar({ selectedModel }: InputBarProps) {
    const [inputText, setInputText] = useState('');

    const handleChatMessage = useCallback(async (message: string) => {
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

            await ChatService.sendMessage(message, selectedModel, conversationId, messageId, parentMessageId);
        } catch (error) {
            console.error('Error sending message:', error);
        }
    }, [selectedModel]);

    const handleSend = () => {
        if (inputText.trim()) {
            handleChatMessage(inputText);
            setInputText('');
        }
    };

    const handleKeyDown = (e: KeyboardEvent<HTMLTextAreaElement>) => {
        if (e.key === 'Enter' && e.ctrlKey) {
            e.preventDefault();
            handleSend();
        }
    };

    return (
        <div className="h-[30vh] bg-[#1f2937] border-t border-gray-700 shadow-lg p-4 relative before:content-[''] before:absolute before:top-[-15px] before:left-0 before:right-0 before:h-[15px] before:bg-gradient-to-t before:from-[#1f2937] before:to-transparent">
            <div className="h-full flex flex-col gap-2">
                <textarea
                    className="flex-1 w-full p-2 border border-gray-700 rounded-md resize-none focus:outline-none focus:ring-2 focus:ring-blue-500 bg-[#2d3748] text-gray-100"
                    value={inputText}
                    onChange={(e) => setInputText(e.target.value)}
                    onKeyDown={handleKeyDown}
                    placeholder="Type your message here... (Ctrl+Enter to send)"
                />
                <Button
                    className="w-full bg-blue-600 hover:bg-blue-700 text-white"
                    onClick={handleSend}
                >
                    Send
                </Button>
            </div>
        </div>
    );
}