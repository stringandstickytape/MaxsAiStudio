import { RefObject } from 'react';
import { cn } from '@/lib/utils';
import { ConversationView } from './ConversationView';

interface ChatContainerProps {
    messagesEndRef: RefObject<HTMLDivElement>;
    liveStreamContent: string;
    isMobile: boolean;
}

export function ChatContainer({
    messagesEndRef,
    liveStreamContent,
    isMobile
}: ChatContainerProps) {
    return (
        <div
            ref={messagesEndRef}
            className={cn(
                "flex-1 overflow-y-auto p-4 mt-[4.5rem] mb-[31vh] scroll-smooth",
                !isMobile && "ml-16"
            )}
        >
            <ConversationView liveStreamContent={liveStreamContent} />
        </div>
    );
}