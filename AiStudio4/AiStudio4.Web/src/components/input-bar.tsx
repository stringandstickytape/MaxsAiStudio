import React, { useState, KeyboardEvent } from 'react';
import { Button } from '@/components/ui/button';

export function InputBar({ onSendMessage }: { onSendMessage: (message: string) => void }) {
    const [inputText, setInputText] = useState('');

    const handleKeyDown = (e: KeyboardEvent<HTMLTextAreaElement>) => {
        if (e.key === 'Enter' && e.ctrlKey) {
            e.preventDefault();
            handleSend();
        }
    };

    const handleSend = () => {
        if (inputText.trim()) {
            onSendMessage(inputText);
            setInputText(''); // Clear the input after sending
        }
    };

    return (
        <div className="h-[30vh] bg-[#1f2937] border-t border-gray-700 shadow-lg p-4">
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