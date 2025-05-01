// AiStudioClient\src\components\ConvView\ConversationControls.tsx

interface ConversationControlsProps {
  hasMoreToLoad: boolean;
  messageChainLength: number;
  visibleCount: number;
  onLoadMore: () => void;
}

export const ConversationControls = ({ 
  hasMoreToLoad, 
  messageChainLength, 
  visibleCount, 
  onLoadMore 
}: ConversationControlsProps) => {
  if (!hasMoreToLoad) {
    return null;
  }
  
  return (
    <button
      className="ConvView self-center rounded-full px-4 py-2 my-2 text-sm"
      onClick={onLoadMore}
      style={{
        backgroundColor: 'var(--convview-bg, #374151)',
        color: 'var(--convview-text-color, #ffffff)',
        ':hover': {
          backgroundColor: 'var(--convview-accent-color, #4b5563)'
        }
      }}
    >
      Load More Messages ({messageChainLength - visibleCount} remaining)
    </button>
  );
};