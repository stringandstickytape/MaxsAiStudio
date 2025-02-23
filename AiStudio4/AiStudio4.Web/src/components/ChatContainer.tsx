import { useEffect, useRef } from 'react';
import { cn } from '@/lib/utils';
import { ConversationView } from './ConversationView';

interface ChatContainerProps {
    streamTokens: string[];
    isMobile: boolean;
}

export function ChatContainer({
    isMobile,
    streamTokens
}: ChatContainerProps) {
    const messagesEndRef = useRef<HTMLDivElement>(null);

    useEffect(() => {
        if (messagesEndRef.current) { // Removed liveStreamContent check
            requestAnimationFrame(() => {
                if (messagesEndRef.current) {
                    messagesEndRef.current.scrollTo({
                        top: messagesEndRef.current.scrollHeight,
                        behavior: 'smooth'
                    });
                }
            });
        }
    }, []); // Remove dependency on streamTokens since we're not using it in the effect

    return (
        <div
            ref={messagesEndRef}
            className={cn(
                "flex-1 overflow-y-auto p-4 mt-[4.5rem] mb-[31vh] scroll-smooth",
                !isMobile && "ml-16"
            )}
        >
            <ConversationView streamTokens={streamTokens} /> {/* Pass streamTokens */}
        </div>
    );
}