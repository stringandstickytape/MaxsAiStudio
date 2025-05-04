// AiStudioClient\src\components\InputBar\MessageInputArea.tsx
import React, { useState, KeyboardEvent, useRef, useEffect } from 'react';
import { Textarea } from '@/components/ui/textarea';
import { webSocketService } from '@/services/websocket/WebSocketService';
import { SlashDropdown } from '@/components/SlashDropdown';
import { getCursorPosition } from '@/utils/textAreaUtils';

interface MessageInputAreaProps {
    inputText: string;
    setInputText: (value: string) => void;
    onSend: () => void;
    isLoading: boolean;
    disabled: boolean;
    onCursorPositionChange?: (position: number | null) => void;
}

export function MessageInputArea({
    inputText,
    setInputText,
    onSend,
    isLoading,
    disabled,
    onCursorPositionChange
}: MessageInputAreaProps) {
    const textareaRef = useRef<HTMLTextAreaElement>(null);
    const [cursorPosition, setCursorPosition] = useState<number | null>(null);
    const [showSlashDropdown, setShowSlashDropdown] = useState(false);
    const [slashQuery, setSlashQuery] = useState('');
    // We don't need to track dropdown position anymore as it's calculated in the SlashDropdown component

    useEffect(() => {
        if (onCursorPositionChange) {
            onCursorPositionChange(cursorPosition);
        }
    }, [cursorPosition, onCursorPositionChange]);

    const handleTextAreaInput = (e: React.ChangeEvent<HTMLTextAreaElement>) => {
        const value = e.target.value;
        setInputText(value);
        setCursorPosition(e.target.selectionStart);
        
        // Check for slash command trigger
        const match = /(?:^|\s)\/([^\s]*)$/.exec(value);
        
        if (match) {
            const query = match[1];
            setSlashQuery(query);
            setShowSlashDropdown(true);
            
            // No need to calculate position as it's fixed above the input bar
        } else {
            setShowSlashDropdown(false);
        }
    };

    const handleTextAreaClick = (e: React.MouseEvent<HTMLTextAreaElement>) => {
        setCursorPosition(e.currentTarget.selectionStart);
        
        // Check if we should show the slash dropdown after click
        if (textareaRef.current) {
            const value = textareaRef.current.value;
            const selectionStart = textareaRef.current.selectionStart;
            const textBeforeCursor = value.substring(0, selectionStart);
            const match = /(?:^|\s)\/([^\s]*)$/.exec(textBeforeCursor);
            
            if (match) {
                const query = match[1];
                setSlashQuery(query);
                setShowSlashDropdown(true);
                
                // No need to calculate position as it's fixed above the input bar
            } else {
                setShowSlashDropdown(false);
            }
        }
    };

    const handleTextAreaKeyUp = (e: React.KeyboardEvent<HTMLTextAreaElement>) => {
        setCursorPosition(e.currentTarget.selectionStart);
        
        // Don't process special keys that are handled by the dropdown
        if (['ArrowUp', 'ArrowDown', 'Enter', 'Tab', 'Escape'].includes(e.key)) {
            return;
        }
        
        // Check if we should show the slash dropdown after key press
        if (textareaRef.current) {
            const value = textareaRef.current.value;
            const selectionStart = textareaRef.current.selectionStart;
            const textBeforeCursor = value.substring(0, selectionStart);
            const match = /(?:^|\s)\/([^\s]*)$/.exec(textBeforeCursor);
            
            if (match) {
                const query = match[1];
                setSlashQuery(query);
                setShowSlashDropdown(true);
                
                // No need to calculate position as it's fixed above the input bar
            } else {
                setShowSlashDropdown(false);
            }
        }
    };

    const handleKeyDown = (e: KeyboardEvent<HTMLTextAreaElement>) => {
        // If dropdown is open, let it handle these keys
        if (showSlashDropdown && ['ArrowUp', 'ArrowDown', 'Enter', 'Tab', 'Escape', ' '].includes(e.key)) {
            e.preventDefault();
            if (e.key === ' ') {
                // Insert space and hide dropdown
                setShowSlashDropdown(false);
                const cursorPos = e.currentTarget.selectionStart;
                const text = e.currentTarget.value;
                const newText = text.substring(0, cursorPos) + ' ' + text.substring(cursorPos);
                e.currentTarget.value = newText;
                // Need to update React state to match the DOM change
                setInputText(newText);
                // Set cursor position after the space
                setTimeout(() => {
                    if (textareaRef.current) {
                        textareaRef.current.selectionStart = textareaRef.current.selectionEnd = cursorPos + 1;
                    }
                }, 0);
            }
            return;
        }
        
        // Standard Ctrl+Enter to send
        if (e.key === 'Enter' && (e.ctrlKey || e.metaKey)) {
            e.preventDefault();
            // If tool loop is running (isLoading), send interjection instead of normal send
            if (isLoading) {
                webSocketService.sendInterjection(inputText);
                setInputText('');
            } else {
                onSend();
            }
            return;
        }
    };

    // Handle selection from dropdown
    const handleSlashItemSelect = (text: string) => {
        // Replace the slash command with the selected text
        if (textareaRef.current) {
            const value = textareaRef.current.value;
            const selectionStart = textareaRef.current.selectionStart;
            const textBeforeCursor = value.substring(0, selectionStart);
            const textAfterCursor = value.substring(selectionStart);
            
            // Find the last slash command before cursor
            const match = /(?:^|\s)\/([^\s]*)$/.exec(textBeforeCursor);
            if (match) {
                const matchStart = match.index;
                const matchEnd = matchStart + match[0].length;
                
                // Replace the slash command with the selected text
                const newValue = textBeforeCursor.substring(0, matchStart) + 
                    (match[0].startsWith(' ') ? ' ' : '') + 
                    text + 
                    textAfterCursor;
                
                setInputText(newValue);
                
                // Set cursor position after the inserted text
                const newCursorPosition = matchStart + 
                    (match[0].startsWith(' ') ? 1 : 0) + 
                    text.length;
                
                setTimeout(() => {
                    if (textareaRef.current) {
                        textareaRef.current.focus();
                        textareaRef.current.setSelectionRange(newCursorPosition, newCursorPosition);
                        setCursorPosition(newCursorPosition);
                    }
                }, 0);
            }
        }
        
        setShowSlashDropdown(false);
    };

    // Handle cancellation
    const handleSlashDropdownCancel = () => {
        setShowSlashDropdown(false);
    };

    // Expose focus method to parent component via ref
    React.useImperativeHandle(
        textareaRef,
        () => ({
            ...textareaRef.current,
            focusWithCursor: (length: number | null = null) => {
                if (!textareaRef.current) return;
                textareaRef.current.focus();
                setTimeout(() => {
                    if (textareaRef.current) {
                        const len = length ?? textareaRef.current.value.length;
                        textareaRef.current.setSelectionRange(len, len);
                        setCursorPosition(len);
                    }
                }, 0);
            }
        }),
        [textareaRef]
    );

    return (
        <div className="relative flex-1 flex flex-col">
            <Textarea
                ref={textareaRef}
                className="flex-1 w-full p-4 border rounded-xl resize-none focus:outline-none shadow-inner transition-all duration-200 placeholder:text-gray-400 input-ghost"
                value={inputText}
                onChange={handleTextAreaInput}
                onClick={handleTextAreaClick}
                onKeyUp={handleTextAreaKeyUp}
                onKeyDown={handleKeyDown}
                placeholder="Type your message here... (Ctrl+Enter to send, also works during AI processing)"
                disabled={disabled}
                showLineCount={true}
                style={{
                    backgroundColor: 'var(--inputbar-edit-bg, #2d3748)',
                    color: 'var(--inputbar-text-color, #e2e8f0)',
                    ...(window?.theme?.InputBar?.style || {})
                }}
            />
            
            {showSlashDropdown && (
                <SlashDropdown
                    query={slashQuery}
                    onSelect={handleSlashItemSelect}
                    onCancel={handleSlashDropdownCancel}
                    anchorElement={textareaRef.current}
                />
            )}
        </div>
    );
}

// Create a type for the ref
export type MessageInputAreaRef = {
    focusWithCursor: (length?: number | null) => void;
} & HTMLTextAreaElement;