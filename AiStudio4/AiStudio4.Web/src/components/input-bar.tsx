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
        <div className="fixed bottom-0 left-0 right-0 h-[30vh] bg-white border-t shadow-lg p-4">
            <div className="h-full flex flex-col gap-2">
                <textarea
                    className="flex-1 w-full p-2 border rounded-md resize-none focus:outline-none focus:ring-2 focus:ring-blue-500 bg-white text-gray-900"
                    value={inputText}
                    onChange={(e) => setInputText(e.target.value)}
                    onKeyDown={handleKeyDown}
                    placeholder="Type your message here... (Ctrl+Enter to send)"
                />
                <Button 
                    className="w-full" 
                    onClick={handleSend}
                >
                    Send
                </Button>
            </div>
        </div>
    );
}