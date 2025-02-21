import { useEffect, useRef } from 'react';
import { cn } from '@/lib/utils';
import { ConversationView } from './ConversationView';

interface ChatContainerProps {
    liveStreamContent: string;
    isMobile: boolean;
}

export function ChatContainer({
    liveStreamContent,
    isMobile
}: ChatContainerProps) {
    const messagesEndRef = useRef<HTMLDivElement>(null);

    useEffect(() => {
        if (messagesEndRef.current && liveStreamContent) {
            requestAnimationFrame(() => {
                setTimeout(() => {
                    if (messagesEndRef.current) {
                        messagesEndRef.current.scrollTo({
                            top: messagesEndRef.current.scrollHeight,
                            behavior: 'smooth'
                        });
                    }
                }, 10);
            });
        }
    }, [liveStreamContent]);
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