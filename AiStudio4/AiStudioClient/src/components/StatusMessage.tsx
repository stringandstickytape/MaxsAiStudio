// AiStudioClient/src/components/StatusMessage.tsx
import { useStatusMessageStore } from '@/stores/useStatusMessageStore';
import { useEffect, useState } from 'react';

export function StatusMessage() {
    const { message, lastUpdated } = useStatusMessageStore();
    const [elapsedTime, setElapsedTime] = useState<string>('');

    useEffect(() => {
        if (!lastUpdated) {
            setElapsedTime('');
            return;
        }

        // Update the elapsed time every second
        const intervalId = setInterval(() => {
            const now = Date.now();
            const elapsedMs = now - lastUpdated;
            const seconds = Math.floor((elapsedMs / 1000) % 60);
            const minutes = Math.floor(elapsedMs / 1000 / 60);
            setElapsedTime(`${minutes}:${seconds.toString().padStart(2, '0')}`);
        }, 1000);

        // Initial calculation
        const now = Date.now();
        const elapsedMs = now - lastUpdated;
        const seconds = Math.floor((elapsedMs / 1000) % 60);
        const minutes = Math.floor(elapsedMs / 1000 / 60);
        setElapsedTime(`${minutes}:${seconds.toString().padStart(2, '0')}`);

        return () => clearInterval(intervalId);
    }, [lastUpdated]);

    // Hide component when message is empty
    if (!message) return null;

    return (
        <div className="status-message-container py-1 px-2 rounded-md text-sm font-medium bg-blue-900/80 text-blue-200 flex items-center gap-2 animate-fade-in">
            <span className="animate-pulse">⚙️</span>
            {elapsedTime && (
                <span className="opacity-70">{elapsedTime}</span>
            )}
            {message}
        </div>
    );
}