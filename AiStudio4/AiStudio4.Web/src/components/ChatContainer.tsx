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
        if (messagesEndRef.current) {
            requestAnimationFrame(() => {
                if (messagesEndRef.current) {
                    messagesEndRef.current.scrollTo({
                        top: messagesEndRef.current.scrollHeight,
                        behavior: 'smooth'
                    });
                }
            });
        }
    }, [streamTokens]);

    return (
        <div
            ref={messagesEndRef}
            className="flex-1 overflow-y-auto p-4 mt-[4.5rem] mb-[31vh] scroll-smooth"
        >
            <ConversationView streamTokens={streamTokens} /> {/* Pass streamTokens */}
        </div>
    );
}