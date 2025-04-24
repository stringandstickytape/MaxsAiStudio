// AiStudio4.Web/src/components/StatusMessage.tsx
import { useStatusMessageStore } from '@/stores/useStatusMessageStore';

export function StatusMessage() {
  const { message } = useStatusMessageStore();

  // Hide component when message is empty
  if (!message) return null;

  return (
    <div className="status-message-container p-2 mb-4 rounded-md text-sm font-medium bg-blue-900/80 text-blue-200 flex items-center gap-2 animate-fade-in">
      <span className="animate-pulse">⚙️</span>
      {message}
    </div>
  );
}