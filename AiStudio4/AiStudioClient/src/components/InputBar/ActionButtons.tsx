// AiStudioClient\src\components\InputBar\ActionButtons.tsx
import React from 'react';
import { Button } from '@/components/ui/button';
import { BookMarked, Mic, Send, X, MessageSquarePlus } from 'lucide-react';
import { FileAttachment } from '@/components/FileAttachment';

import { useModalStore } from '@/stores/useModalStore';
import { useWebSocketStore } from '@/stores/useWebSocketStore';
import { webSocketService } from '@/services/websocket/WebSocketService';


interface ActionButtonsProps {
    onSend: () => void;
    onCancel: () => void;

    addAttachments?: (files: File[]) => void; // Optional since we'll use the store directly
    isLoading: boolean;
    isCancelling: boolean;
    disabled: boolean;
    inputText: string;
    setInputText: (text: string) => void;
    messageSent?: boolean;
    isListening: boolean; // Added
    onToggleListening: () => void; // Added
}

// Custom comparison function for ActionButtons memoization
const areActionButtonsPropsEqual = (prevProps: ActionButtonsProps, nextProps: ActionButtonsProps) => {
  // Compare all props that affect rendering
  return (
    prevProps.isLoading === nextProps.isLoading &&
    prevProps.isCancelling === nextProps.isCancelling &&
    prevProps.disabled === nextProps.disabled &&
    prevProps.isListening === nextProps.isListening &&
    prevProps.messageSent === nextProps.messageSent &&
    prevProps.inputText === nextProps.inputText
    // Note: We don't compare function props as they should be stable references
  );
};

export const ActionButtons = React.memo(({
    onSend,
    onCancel,

    addAttachments,
    isLoading,
    isCancelling,
    disabled,
    inputText,
    setInputText,
    messageSent = false,
    isListening, // Added
    onToggleListening, // Added
}: ActionButtonsProps) => {
    const isConnected = useWebSocketStore(state => state.isConnected);
    
    const handleInterjection = () => {
        if (inputText.trim()) {
            webSocketService.sendInterjection(inputText);
            setInputText('');
        }
    };
    
    return (
        <div className="flex flex-row gap-2 items-center self-center">
            <FileAttachment
                className="h-6"
                disabled={isLoading || disabled}
                maxFiles={5}
                style={{
                    backgroundColor: 'transparent',
                    border: 'none',
                    color: 'var(--global-text-color)',
                    opacity: (isLoading || disabled) ? 0.5 : 1,
                    ...(window?.theme?.InputBar?.style || {})
                }}
            />

            <Button
                className="h-6"
                variant="outline"
                size="icon"
                onClick={() => useModalStore.getState().openModal('userPrompt')}
                aria-label="User prompts"
                disabled={isLoading || disabled}
                style={{
                    backgroundColor: 'transparent',
                    border: 'none',
                    color: 'var(--global-text-color)',
                    opacity: (isLoading || disabled) ? 0.5 : 1,
                    ...(window?.theme?.InputBar?.style || {})
                }}
            >
                <BookMarked className="h-5 w-5" />
            </Button>

            {/* Voice Input Button - appearance changes based on isListening */}
            <Button
                className={`h-6 ${isListening ? 'animate-pulse' : ''}`}
                variant="outline"
                size="icon"
                onClick={onToggleListening}
                aria-label={isListening ? "Stop Voice Input" : "Start Voice Input"}
                title={isListening ? "Stop Voice Input" : "Start Voice Input"} // Added title for tooltip consistency
                disabled={isLoading || disabled} // Retain existing disabled logic
                style={{
                    backgroundColor: 'transparent',
                    border: 'none',
                    opacity: (isLoading || disabled) ? 0.5 : 1,
                    ...(window?.theme?.InputBar?.style || {}), // Spread theme first
                    // Use global primary color for all states
                    color: 'var(--global-text-color)',
                }}
            >
                <Mic className="h-5 w-5" />
            </Button>

            {/* Send Button - Only visible when not loading */}
            {!isLoading && (
                <Button
                    className="h-8"
                    variant="outline"
                    size="icon"
                    onClick={onSend}
                    aria-label={isConnected ? 'Send message' : 'Reconnect and Send'}
                    disabled={disabled}
                    title={!isConnected ? 'WebSocket disconnected. Click to reconnect.' : ''}
                    style={{
                        backgroundColor: 'transparent',
                        border: 'none',
                        color: 'var(--global-text-color)',
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
                    className="h-8"
                    variant="outline"
                    size="icon"
                    onClick={onCancel}
                    aria-label="Cancel"
                    disabled={isCancelling || disabled}
                    style={{
                        backgroundColor: 'transparent',
                        border: 'none',
                        color: 'var(--global-primary-color)',
                        opacity: (isCancelling || disabled) ? 0.5 : 1,
                        ...(window?.theme?.InputBar?.style || {})
                    }}
                >
                    <X className="h-5 w-5" />
                </Button>
            )}
            
            {/* Interject Button - Only visible when loading, not cancelling, and there's text */}
            {isLoading && !isCancelling && messageSent && (
                <Button
                    className="h-8"
                    variant="outline"
                    size="icon"
                    onClick={handleInterjection}
                    aria-label="Send interjection"
                    title="Send interjection during processing"
                    disabled={!inputText.trim() || disabled}
                    style={{
                        backgroundColor: 'transparent',
                        border: 'none',
                        color: 'var(--global-primary-color)',
                        opacity: (!inputText.trim() || disabled) ? 0.5 : 1,
                        ...(window?.theme?.InputBar?.style || {})
                    }}
                >
                    <MessageSquarePlus className="h-5 w-5" />
                </Button>
            )}
        </div>
    );
}, areActionButtonsPropsEqual);