// AiStudioClient/src/components/InputBar/InputAreaWithButtons.tsx
import React, { useState, KeyboardEvent, useRef, useEffect, useCallback, useMemo } from 'react';
import { Textarea } from '@/components/ui/textarea';
import { Button } from '@/components/ui/button';
import { BookMarked, Mic, Send, X, MessageSquarePlus } from 'lucide-react';
import { FileAttachment } from '@/components/FileAttachment';
import { SlashDropdown } from '@/components/SlashDropdown';
import { webSocketService } from '@/services/websocket/WebSocketService';
import { useModalStore } from '@/stores/useModalStore';
import { useWebSocketStore } from '@/stores/useWebSocketStore';

interface InputAreaWithButtonsProps {
    inputText: string;
    setInputText: (value: string) => void;
    onSend: () => void;
    onCancel: () => void;
    isLoading: boolean;
    isCancelling: boolean;
    disabled: boolean;
    messageSent?: boolean;
    isListening: boolean;
    onToggleListening: () => void;
    onCursorPositionChange?: (position: number | null) => void;
    onAttachFile?: (file: File) => void;
    addAttachments?: (files: File[]) => void;
}

function InputAreaWithButtonsComponent({
    inputText: propInputText,
    setInputText: propSetInputText,
    onSend,
    onCancel,
    isLoading,
    isCancelling,
    disabled,
    messageSent = false,
    isListening,
    onToggleListening,
    onCursorPositionChange,
    onAttachFile,
    addAttachments
}: InputAreaWithButtonsProps) {
    const textareaRef = useRef<HTMLTextAreaElement>(null);
    const [localInputText, setLocalInputText] = useState<string>(propInputText);
    const [cursorPosition, setCursorPosition] = useState<number | null>(null);
    const [showSlashDropdown, setShowSlashDropdown] = useState(false);
    const [slashQuery, setSlashQuery] = useState('');
    
    const isConnected = useWebSocketStore(state => state.isConnected);
    
    // Update local state when prop changes
    useEffect(() => {
        setLocalInputText(propInputText);
    }, [propInputText]);

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
    
    // Handle text input with debounced parent updates
    const handleTextAreaInput = useCallback((e: React.ChangeEvent<HTMLTextAreaElement>) => {
        const value = e.target.value;
        const selectionStart = e.target.selectionStart;
        
        // Update both local and parent state immediately
        setLocalInputText(value);
        propSetInputText(value);
        
        setCursorPosition(selectionStart);
        checkSlashCommand(value, selectionStart);
    }, [propSetInputText, checkSlashCommand]);

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
                // Update both local and parent state
                setLocalInputText(newText);
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
                webSocketService.sendInterjection(localInputText);
                setLocalInputText('');
                propSetInputText('');
            } else {
                onSend();
            }
            return;
        }
    }, [showSlashDropdown, isLoading, localInputText, onSend, propSetInputText]);

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
                
                // Update both local and parent state
                setLocalInputText(newValue);
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

    const handleInterjection = () => {
        if (localInputText.trim()) {
            webSocketService.sendInterjection(localInputText);
            setLocalInputText('');
            propSetInputText('');
        }
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
        []
    );

    // Memoize the style object to prevent unnecessary re-renders
    const textareaStyle = useMemo(() => ({
        background: 'var(--global-ai-message-background, #1f2937)',
        color: 'var(--global-ai-message-text-color, #ffffff)',
        borderRadius: 'var(--global-border-radius, 0.5rem)',
        borderColor: 'var(--global-ai-message-border-color, rgba(55, 65, 81, 0.3))',
        borderWidth: 'var(--global-ai-message-border-width, 0px)',
        borderStyle: 'var(--global-ai-message-border-style, solid)',
        ...(window?.theme?.InputBar?.style || {})
    }), []);

    return (
        <div className="relative flex-1 flex flex-col">
            {/* Main input area using flexbox layout */}
            <div className="flex flex-col flex-1 border rounded-xl overflow-hidden"
                 style={{
                     backgroundColor: 'var(--global-ai-message-background, #1f2937)',
                     borderColor: 'var(--global-ai-message-border-color, rgba(55, 65, 81, 0.3))',
                 }}>
                
                {/* Textarea takes most of the space */}
                <Textarea
                    ref={textareaRef}
                    className="flex-1 w-full p-4 border-0 resize-none focus:outline-none shadow-inner transition-all duration-200 placeholder:text-gray-400 input-ghost"
                    value={localInputText}
                    onChange={handleTextAreaInput}
                    onClick={handleTextAreaClick}
                    onKeyUp={handleTextAreaKeyUp}
                    onKeyDown={handleKeyDown}
                    placeholder="Type your message here...

* CTRL+Return to send
* CTRL+Return to interject (during AI tool loops)
* Type / for quicklist of user prompts and project files
* SHIFT+select a file in the slash menu to attach it"
                    disabled={disabled}
                    showLineCount={true}
                    style={textareaStyle}
                />
                
                {/* Button row at the bottom - using flexbox */}
                <div className="flex justify-end items-center gap-2 p-2 border-t border-gray-700/30">
                    {/* Utility buttons */}
                    <FileAttachment
                        className="h-8 w-8"
                        disabled={isLoading || disabled}
                        maxFiles={5}
                        style={{
                            backgroundColor: 'var(--inputbar-button-bg, #2d3748)',
                            borderColor: 'var(--inputbar-border-color, #4a5568)',
                            color: 'var(--inputbar-text-color, #e2e8f0)',
                            opacity: (isLoading || disabled) ? 0.5 : 1,
                            ...(window?.theme?.InputBar?.style || {})
                        }}
                    />

                    <Button
                        className="h-8 w-8"
                        variant="outline"
                        size="icon"
                        onClick={() => useModalStore.getState().openModal('userPrompt')}
                        aria-label="User prompts"
                        disabled={isLoading || disabled}
                        style={{
                            backgroundColor: 'var(--inputbar-button-bg, #2d3748)',
                            borderColor: 'var(--inputbar-border-color, #4a5568)',
                            color: 'var(--inputbar-text-color, #e2e8f0)',
                            opacity: (isLoading || disabled) ? 0.5 : 1,
                            ...(window?.theme?.InputBar?.style || {})
                        }}
                    >
                        <BookMarked className="h-4 w-4" />
                    </Button>

                    {/* Voice Input Button */}
                    <Button
                        className={`h-8 w-8 ${isListening ? 'animate-pulse' : ''}`}
                        variant="outline"
                        size="icon"
                        onClick={onToggleListening}
                        aria-label={isListening ? "Stop Voice Input" : "Start Voice Input"}
                        title={isListening ? "Stop Voice Input" : "Start Voice Input"}
                        disabled={isLoading || disabled}
                        style={{
                            backgroundColor: 'var(--inputbar-button-bg, #2d3748)',
                            borderColor: 'var(--inputbar-border-color, #4a5568)',
                            opacity: (isLoading || disabled) ? 0.5 : 1,
                            ...(window?.theme?.InputBar?.style || {}),
                            color: isListening 
                                   ? 'var(--global-primary-color, #3b82f6)' 
                                   : ((window?.theme?.InputBar?.style || {}).color || 'var(--inputbar-text-color, #e2e8f0)'),
                        }}
                    >
                        <Mic className="h-4 w-4" />
                    </Button>

                    {/* Send Button - Only visible when not loading */}
                    {!isLoading && (
                        <Button
                            className="h-10 w-10"
                            variant="outline"
                            size="icon"
                            onClick={onSend}
                            aria-label={isConnected ? 'Send message' : 'Reconnect and Send'}
                            disabled={disabled}
                            title={!isConnected ? 'WebSocket disconnected. Click to reconnect.' : ''}
                            style={{
                                backgroundColor: 'var(--inputbar-button-bg, #2d3748)',
                                borderColor: !isConnected ? 'red' : 'var(--inputbar-border-color, #4a5568)',
                                color: 'var(--inputbar-text-color, #e2e8f0)',
                                opacity: disabled ? 0.5 : 1,
                                ...(window?.theme?.InputBar?.style || {})
                            }}
                        >
                            <Send className="h-5 w-5" />
                        </Button>
                    )}
                    
                    {/* Cancel Button - Only visible when loading and not cancelling */}
                    {isLoading && !isCancelling && (
                        <Button
                            className="h-8 w-8"
                            variant="outline"
                            size="icon"
                            onClick={onCancel}
                            aria-label="Cancel"
                            disabled={isCancelling || disabled}
                            style={{
                                backgroundColor: '#dc2626',
                                borderColor: 'var(--inputbar-border-color, #4a5568)',
                                color: 'var(--inputbar-text-color, #e2e8f0)',
                                opacity: (isCancelling || disabled) ? 0.5 : 1,
                                ...(window?.theme?.InputBar?.style || {})
                            }}
                        >
                            <X className="h-4 w-4" />
                        </Button>
                    )}
                    
                    {/* Interject Button - Only visible when loading, not cancelling, and there's text */}
                    {isLoading && !isCancelling && messageSent && (
                        <Button
                            className="h-8 w-8"
                            variant="outline"
                            size="icon"
                            onClick={handleInterjection}
                            aria-label="Send interjection"
                            title="Send interjection during processing"
                            disabled={!localInputText.trim() || disabled}
                            style={{
                                backgroundColor: 'var(--inputbar-button-bg, #2d3748)',
                                borderColor: 'var(--inputbar-border-color, #4a5568)',
                                color: 'var(--inputbar-text-color, #e2e8f0)',
                                opacity: (!localInputText.trim() || disabled) ? 0.5 : 1,
                                ...(window?.theme?.InputBar?.style || {})
                            }}
                        >
                            <MessageSquarePlus className="h-4 w-4" />
                        </Button>
                    )}
                </div>
            </div>
            
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
export type InputAreaWithButtonsRef = {
    focusWithCursor: (length?: number | null) => void;
} & HTMLTextAreaElement;

// Export memoized component to prevent unnecessary re-renders
export const InputAreaWithButtons = React.memo(InputAreaWithButtonsComponent, 
    (prevProps, nextProps) => {
        // Only re-render when these props change
        return (
            prevProps.isLoading === nextProps.isLoading &&
            prevProps.disabled === nextProps.disabled &&
            prevProps.inputText === nextProps.inputText &&
            prevProps.isListening === nextProps.isListening &&
            prevProps.isCancelling === nextProps.isCancelling &&
            prevProps.messageSent === nextProps.messageSent
        );
    }
);