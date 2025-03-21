// src/components/InputBar.tsx
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
    activeTools?: string[]; // Optional override from parent
    onManageTools?: () => void; // Add this prop for the Manage Tools button
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



    // If props are provided, use them, otherwise use local state
    const [localInputText, setLocalInputText] = useState('');

    // Use either the props or local state
    const inputText = inputValue !== undefined ? inputValue : localInputText;
    const setInputText = onInputChange || setLocalInputText;

    // Get active tools from Zustand store if not provided via props
    const { activeTools: activeToolsFromStore } = useToolStore();
    
    // Use props if provided, otherwise use from store
    const activeTools = activeToolsFromProps || activeToolsFromStore;

    // Get system prompts from Zustand store
    const { conversationPrompts, defaultPromptId, prompts } = useSystemPromptStore();
    
    // Get conversation state from Zustand store
    const { 
        activeConversationId, 
        selectedMessageId,
        conversations,
        createConversation,
        getConversation 
    } = useConversationStore();

    // Use the new chat management hook instead of RTK Query
    const { sendMessage, isLoading } = useChatManagement();

    // Track cursor position for file insertion
    const [cursorPosition, setCursorPosition] = useState<number | null>(null);
    
    const handleTextAreaInput = (e: React.ChangeEvent<HTMLTextAreaElement>) => {
        setInputText(e.target.value);
        // Save cursor position
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

        
        // Standard behavior for web environment
        // Format text to insert
        const textToInsert = `\`\`\`${fileName}\n${content}\n\`\`\`\n`;

        // Insert at cursor position or append to end
        const pos = cursorPosition !== null ? cursorPosition : inputText.length;

        const newText = inputText.substring(0, pos) +
            textToInsert +
            inputText.substring(pos);

        setInputText(newText);

        // Focus back on textarea after insertion
        setTimeout(() => {
            if (textareaRef.current) {
                textareaRef.current.focus();
                // Move cursor to the end of the inserted text
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

            // Determine which system prompt to use
            let systemPromptId = null;
            let systemPromptContent = null;

            // If no active conversation, create a new one
            if (!conversationId) {
                conversationId = `conv_${uuidv4()}`;
                const messageId = `msg_${uuidv4()}`;
                
                // Create a new conversation in the Zustand store
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
                // Use the current selected message ID from the store if available
                parentMessageId = selectedMessageId;
                
                // If still no parent ID, try to find the last message in the conversation
                if (!parentMessageId) {
                    const conversation = conversations[conversationId];
                    if (conversation && conversation.messages.length > 0) {
                        parentMessageId = conversation.messages[conversation.messages.length - 1].id;
                    }
                }
            }

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

            // Send the message using the chat API
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
        // Listen for append-to-prompt events
        const handleAppendToPrompt = (event: CustomEvent<{ text: string }>) => {
            // Get the text to append
            const textToAppend = event.detail.text;

            // Update the input text by appending the new text
            setInputText(currentText => currentText + textToAppend);

            // Focus the textarea
            if (textareaRef.current) {
                textareaRef.current.focus();

                // Move cursor to the end
                setTimeout(() => {
                    if (textareaRef.current) {
                        const length = textareaRef.current.value.length;
                        textareaRef.current.setSelectionRange(length, length);
                        setCursorPosition(length);
                    }
                }, 0);
            }
        };

        // Listen for set-prompt events
        const handleSetPrompt = (event: CustomEvent<{ text: string }>) => {
            // Get the text to set
            const newText = event.detail.text;

            // Set the input text to the new text
            setInputText(newText);

            // Focus the textarea
            if (textareaRef.current) {
                textareaRef.current.focus();

                // Move cursor to the end
                setTimeout(() => {
                    if (textareaRef.current) {
                        const length = textareaRef.current.value.length;
                        textareaRef.current.setSelectionRange(length, length);
                        setCursorPosition(length);
                    }
                }, 0);
            }
        };

        // Add event listeners
        window.addEventListener('append-to-prompt', handleAppendToPrompt as EventListener);
        window.addEventListener('set-prompt', handleSetPrompt as EventListener);

        // Clean up event listeners when component unmounts
        return () => {
            window.removeEventListener('append-to-prompt', handleAppendToPrompt as EventListener);
            window.removeEventListener('set-prompt', handleSetPrompt as EventListener);
        };
    }, []); // Empty dependency array means this runs once on mount

    return (
        <div className="h-[30vh] bg-gray-900 border-t border-gray-700/50 shadow-2xl p-3 relative before:content-[''] before:absolute before:top-[-15px] before:left-0 before:right-0 before:h-[15px] before:bg-transparent backdrop-blur-sm">
            <div className="h-full flex flex-col">
                {/* Tool selector - pass onManageTools prop */}
                <div className="mb-2">
                    <ToolSelector onManageTools={onManageTools || (() => window.dispatchEvent(new CustomEvent('open-tool-panel')))} />
                </div>

                <div className="flex-1 flex gap-2">
                    {/* Input area */}
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

                    {/* Vertical button bar */}
                    <div className="flex flex-col gap-2 justify-end">
                        {/* File attachment button */}
                        <FileAttachment
                            onAttach={handleAttachFile}
                            disabled={isLoading}
                        />

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

window.appendToPrompt = function (text) {
    // Create a custom event with the text to append
    const appendEvent = new CustomEvent('append-to-prompt', {
        detail: { text: text }
    });

    // Dispatch the event for components to listen for
    window.dispatchEvent(appendEvent);

    // Log for confirmation
    console.log(`Appended to prompt: "${text}"`);

    return true; // For success feedback
};

// This function will allow you to both append and set the prompt
window.setPrompt = function (text) {
    // Create a custom event with the text to set
    const setEvent = new CustomEvent('set-prompt', {
        detail: { text: text }
    });

    // Dispatch the event for components to listen for
    window.dispatchEvent(setEvent);

    // Log for confirmation
    console.log(`Set prompt to: "${text}"`);

    return true; // For success feedback
};
