// AiStudio4.Web\src\components\InputBar\StatusSection.tsx
import React from 'react';
import { Button } from '@/components/ui/button';
import { ArrowDown } from 'lucide-react';
import { useJumpToEndStore } from '@/stores/useJumpToEndStore';
import { windowEventService, WindowEvents } from '@/services/windowEvents';

interface StatusSectionProps {
    isAtBottom: boolean;
    disabled: boolean;
}

export function StatusSection({ isAtBottom, disabled }: StatusSectionProps) {
    if (isAtBottom) {
        return null;
    }
    
    return (
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
            className="absolute left-1/2 transform -translate-x-1/2 -top-10 z-10 h-5 px-2 py-0 text-xs rounded-full bg-gray-600/50 border border-gray-700/70 text-gray-300 hover:bg-gray-600/90 hover:text-gray-100 transition-colors flex-shrink-0"
            title="Scroll to bottom of conversation"
            disabled={disabled}
        >
            <ArrowDown className="h-3 w-3 mr-1" />
            <span>Scroll to Bottom</span>
        </Button>
    );
}