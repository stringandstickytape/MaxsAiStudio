// AiStudioClient\src\components\InputBar\MessageInputArea.tsx
import React, { useState, KeyboardEvent, useRef, useEffect, useCallback, useMemo } from 'react';
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
    onAttachFile?: (file: File) => void; // New prop for handling file attachments
}

function MessageInputAreaComponent({
    inputText: propInputText,
    setInputText: propSetInputText,
    onSend,
    isLoading,
    disabled,
    onCursorPositionChange,
    onAttachFile
}: MessageInputAreaProps) {
    const textareaRef = useRef<HTMLTextAreaElement>(null);
    const [cursorPosition, setCursorPosition] = useState<number | null>(null);
    const [showSlashDropdown, setShowSlashDropdown] = useState(false);
    const [slashQuery, setSlashQuery] = useState('');
    
    // Initialize textarea value from prop on mount only
    useEffect(() => {
        if (textareaRef.current) {
            textareaRef.current.value = propInputText;
        }
    }, []); // Empty dependency array - only run on mount

    // Notify parent of cursor position changes
    useEffect(() => {
        if (onCursorPositionChange) {
            onCursorPositionChange(cursorPosition);
        }
    }, [cursorPosition, onCursorPositionChange]);

    // Consolidated function to check for slash commands
    const checkSlashCommand = useCallback((value: string, selectionStart: number) => {
        const textBeforeCursor = value.substring(0, selectionStart);
        const match = /(?:^|\s)\/([^\s]*)$/.exec(textBeforeCursor);
        
        if (match) {
            const query = match[1];
            setSlashQuery(query);
            setShowSlashDropdown(true);
        } else {
            setShowSlashDropdown(false);
        }
    }, []);
    
    // Handle text input - ref-based approach, no state updates on keystroke
    const handleTextAreaInput = useCallback((e: React.ChangeEvent<HTMLTextAreaElement>) => {
        const value = e.target.value;
        const selectionStart = e.target.selectionStart;
        
        // Don't update any state on keystroke to prevent infinite loops
        // Only check slash commands, cursor position will be updated on keyUp
        checkSlashCommand(value, selectionStart);
    }, [checkSlashCommand]);

    // Add blur handler to sync with parent when user finishes typing
    const handleTextAreaBlur = useCallback(() => {
        if (textareaRef.current) {
            propSetInputText(textareaRef.current.value);
        }
    }, [propSetInputText]);

    // Handle click events on textarea
    const handleTextAreaClick = useCallback((e: React.MouseEvent<HTMLTextAreaElement>) => {
        const selectionStart = e.currentTarget.selectionStart;
        setCursorPosition(selectionStart);
        
        // Check for slash commands on click
        if (textareaRef.current) {
            const value = textareaRef.current.value;
            checkSlashCommand(value, selectionStart);
        }
    }, [checkSlashCommand]);

    // Handle key up events
    const handleTextAreaKeyUp = useCallback((e: React.KeyboardEvent<HTMLTextAreaElement>) => {
        const selectionStart = e.currentTarget.selectionStart;
        setCursorPosition(selectionStart);
        
        // Don't process special keys that are handled by the dropdown
        if (['ArrowUp', 'ArrowDown', 'Enter', 'Tab', 'Escape'].includes(e.key)) {
            return;
        }
        
        // Check for slash commands on key up
        if (textareaRef.current) {
            const value = textareaRef.current.value;
            checkSlashCommand(value, selectionStart);
        }
    }, [checkSlashCommand]);

    // Handle key down events
    const handleKeyDown = useCallback((e: KeyboardEvent<HTMLTextAreaElement>) => {
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
                // Only update parent state
                propSetInputText(newText);
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
                const currentText = textareaRef.current?.value || '';
                webSocketService.sendInterjection(currentText);
                if (textareaRef.current) {
                    textareaRef.current.value = '';
                }
                propSetInputText('');
            } else {
                onSend();
            }
            return;
        }
    }, [showSlashDropdown, isLoading, onSend, propSetInputText]);

    // Handle selection from dropdown
    const handleSlashItemSelect = useCallback((text: string) => {
        // Replace the slash command with the selected text (or remove it if text is empty)
        if (textareaRef.current) {
            const value = textareaRef.current.value;
            const selectionStart = textareaRef.current.selectionStart;
            const textBeforeCursor = value.substring(0, selectionStart);
            const textAfterCursor = value.substring(selectionStart);
            
            // Find the last slash command before cursor
            const match = /(?:^|\s)\/([^\s]*)$/.exec(textBeforeCursor);
            if (match) {
                // Get the full match and the slash command part
                const fullMatch = match[0];
                const slashCommand = '/' + match[1];
                const matchStart = match.index;
                
                // Find where the actual slash character starts within the match
                const slashIndex = fullMatch.indexOf('/');
                
                // Calculate the start position of the slash command (not including any leading space)
                const slashStart = matchStart + slashIndex;
                
                // If text is empty, just remove the slash command
                // Otherwise, replace with the selected text
                const newValue = text === ''
                    ? textBeforeCursor.substring(0, slashStart) + textAfterCursor
                    : textBeforeCursor.substring(0, slashStart) + text + textAfterCursor;
                
                // Update textarea value and parent state
                textareaRef.current.value = newValue;
                propSetInputText(newValue);
                
                // Set cursor position after the inserted text (or at slashStart if removed)
                const newCursorPosition = text === '' ? slashStart : (slashStart + text.length);
                
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
    }, [propSetInputText]);

    // Handle cancellation
    const handleSlashDropdownCancel = useCallback(() => {
        setShowSlashDropdown(false);
    }, []);

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
        []
    );

    // Memoize the style object to prevent unnecessary re-renders
    const textareaStyle = useMemo(() => ({
        background: 'var(--global-ai-message-background, #1f2937)',
        color: 'var(--global-ai-message-text-color, #ffffff)',
        borderRadius: 'var(--global-border-radius, 0.5rem)',
        borderColor: 'var(--global-ai-message-border-color, rgba(55, 65, 81, 0.3))',
        borderWidth: '0px',
        borderStyle: 'var(--global-ai-message-border-style, solid)',
        ...(window?.theme?.InputBar?.style || {})
    }), []);




    return (
        <div className="relative flex-1 flex flex-col">
            <Textarea
                ref={textareaRef}
                className="flex-1 w-full p-2 border rounded-xl resize-none focus:outline-none shadow-inner transition-all duration-200 placeholder:text-gray-400 input-ghost"
                defaultValue={propInputText}
                onChange={handleTextAreaInput}
                onBlur={handleTextAreaBlur}
                onClick={handleTextAreaClick}
                onKeyUp={handleTextAreaKeyUp}
                onKeyDown={handleKeyDown}
                placeholder="Enter prompt here (CTRL-Return to send)"
                disabled={disabled}
                showLineCount={true}
                style={textareaStyle}
            />
            
            {showSlashDropdown && (
                <SlashDropdown
                    query={slashQuery}
                    onSelect={handleSlashItemSelect}
                    onCancel={handleSlashDropdownCancel}
                    anchorElement={textareaRef.current}
                    onAttachFile={onAttachFile}
                />
            )}
        </div>
    );
}

// Create a type for the ref
export type MessageInputAreaRef = {
    focusWithCursor: (length?: number | null) => void;
} & HTMLTextAreaElement;

// Export memoized component to prevent unnecessary re-renders
export const MessageInputArea = React.memo(MessageInputAreaComponent, 
    (prevProps, nextProps) => {
        // Only re-render when these props change
        return (
            prevProps.isLoading === nextProps.isLoading &&
            prevProps.disabled === nextProps.disabled &&
            prevProps.inputText === nextProps.inputText
            // We don't compare function props as they should be stable references
        );
    }
);