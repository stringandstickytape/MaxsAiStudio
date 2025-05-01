// AiStudioClient\src\components\InputBar\ActionButtons.tsx
import React from 'react';
import { Button } from '@/components/ui/button';
import { BookMarked, Mic, Send, X } from 'lucide-react';
import { FileAttachment } from '@/components/FileAttachment';
import { Attachment } from '@/types/attachment';
import { useModalStore } from '@/stores/useModalStore';
import { useWebSocketStore } from '@/stores/useWebSocketStore';

interface ActionButtonsProps {
    onSend: () => void;
    onVoiceInputClick?: () => void;
    addAttachments: (files: Attachment[]) => void;
    isLoading: boolean;
    isCancelling: boolean;
    disabled: boolean;
}

export function ActionButtons({
    onSend,
    onVoiceInputClick,
    addAttachments,
    isLoading,
    isCancelling,
    disabled
}: ActionButtonsProps) {
    const isConnected = useWebSocketStore(state => state.isConnected);
    
    return (
        <div className="flex flex-col gap-2 justify-end">
            <FileAttachment
                onFilesSelected={addAttachments}
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
                <BookMarked className="h-5 w-5" />
            </Button>

            {onVoiceInputClick && (
                <Button
                    variant="outline"
                    size="icon"
                    onClick={onVoiceInputClick}
                    aria-label="Voice input"
                    disabled={isLoading || disabled}
                    style={{
                        backgroundColor: 'var(--inputbar-button-bg, #2d3748)',
                        borderColor: 'var(--inputbar-border-color, #4a5568)',
                        color: 'var(--inputbar-text-color, #e2e8f0)',
                        opacity: (isLoading || disabled) ? 0.5 : 1,
                        ...(window?.theme?.InputBar?.style || {})
                    }}
                >
                    <Mic className="h-5 w-5" />
                </Button>
            )}

            <div className="flex flex-col gap-2">
                <Button
                    variant="outline"
                    size="icon"
                    onClick={onSend}
                    aria-label={isLoading ? 'Cancel' : isConnected ? 'Send message' : 'Reconnect and Send'}
                    disabled={isCancelling || disabled}
                    title={!isConnected && !isLoading ? 'WebSocket disconnected. Click to reconnect.' : ''}
                    style={{
                        backgroundColor: isLoading ? '#dc2626' : 'var(--inputbar-button-bg, #2d3748)',
                        borderColor: !isConnected && !isLoading ? 'red' : 'var(--inputbar-border-color, #4a5568)',
                        color: 'var(--inputbar-text-color, #e2e8f0)',
                        opacity: (isCancelling || disabled) ? 0.5 : 1,
                        ...(window?.theme?.InputBar?.style || {})
                    }}
                >
                    {isLoading ? (
                        <X className="h-5 w-5" />
                    ) : (
                        <Send className="h-5 w-5" />
                    )}
                </Button>
            </div>
        </div>
    );
}