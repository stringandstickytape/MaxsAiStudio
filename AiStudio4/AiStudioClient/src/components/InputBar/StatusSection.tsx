// AiStudioClient\src\components\InputBar\StatusSection.tsx
import React from 'react';
import { Button } from '@/components/ui/button';
import { ArrowDown } from 'lucide-react';
import { useJumpToEndStore } from '@/stores/useJumpToEndStore';
import { windowEventService, WindowEvents } from '@/services/windowEvents';
import { useWebSocketStore } from '@/stores/useWebSocketStore';

interface StatusSectionProps {
    isAtBottom: boolean;
    disabled: boolean;
}

export function StatusSection({ isAtBottom, disabled }: StatusSectionProps) {
    const { isLoading } = useWebSocketStore();
    
    return (
        <div className="absolute left-0 right-0 -top-10 z-10 flex justify-center items-center">
            {isLoading && (
                <div className="absolute right-0 text-xs text-gray-300 bg-gray-600/50 px-2 py-1 rounded-full border border-gray-700/70">
                    Press Ctrl+Enter to send an interjection
                </div>
            )}
            
            {!isAtBottom && (
                <Button
                    variant="ghost"
                    size="sm"
                    onClick={() => {
                        // Set jumpToEndEnabled to true
                        useJumpToEndStore.getState().setJumpToEndEnabled(true);
                        
                        // Try to use the global function if available
                        if (window.scrollConversationToBottom) {
                            window.scrollConversationToBottom();
                        }
                        
                        // Also emit the event as a fallback
                        windowEventService.emit(WindowEvents.SCROLL_TO_BOTTOM);
                    }}
                    className="h-5 px-2 py-0 text-xs rounded-full bg-gray-600/50 border border-gray-700/70 text-gray-300 hover:bg-gray-600/90 hover:text-gray-100 transition-colors flex-shrink-0"
                    title="Scroll to bottom of conversation"
                    disabled={disabled}
                >
                    <ArrowDown className="h-3 w-3 mr-1" />
                    <span>Scroll to Bottom</span>
                </Button>
            )}
        </div>
    );
}