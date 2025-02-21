import React, { useState, KeyboardEvent, useCallback } from 'react';
import { Button } from '@/components/ui/button';
import { ChatService } from '@/services/ChatService';

interface InputBarProps {
    selectedModel: string;
}

export function InputBar({ selectedModel }: InputBarProps) {
    const [inputText, setInputText] = useState('');

    const handleChatMessage = useCallback(async (message: string) => {
        try {
            await ChatService.sendMessage(message, selectedModel);
        } catch (error) {
            console.error('Error sending chat message:', error);
            // TODO: Add error handling/user feedback
        }
    }, [selectedModel]);

    const handleKeyDown = (e: KeyboardEvent<HTMLTextAreaElement>) => {
        if (e.key === 'Enter' && e.ctrlKey) {
            e.preventDefault();
            handleSend();
        }
    };

    const handleSend = () => {
        if (inputText.trim()) {
            handleChatMessage(inputText);
            setInputText(''); // Clear the input after sending
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