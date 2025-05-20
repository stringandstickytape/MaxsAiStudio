// AiStudioClient\src\components\ConvView\MessageMetadata.tsx
import { formatModelDisplay } from '@/utils/modelUtils';
import { useGeneralSettingsStore } from '@/stores/useGeneralSettingsStore';

interface MessageMetadataProps {
  message: any;
}

// Helper function to format duration in a human-readable format
const formatDuration = (message?: any, propName: string = 'durationMs') => {
  // Safety check for null/undefined message
  if (!message) {
    return 'Unknown';
  }
  
  // First try direct property access
  let durationMs = message[propName];
  
  // If that fails, try a few other approaches
  if (durationMs === undefined) {
    // Try Object.getOwnPropertyDescriptor
    const descriptor = Object.getOwnPropertyDescriptor(message, propName);
    if (descriptor) {
      durationMs = descriptor.value;
    }
  }
  
  // Return early if the value is undefined or null
  if (durationMs === undefined || durationMs === null) {
    return "Unknown";
  }
  
  // Ensure we're working with a number
  const duration = Number(durationMs);
  
  // Check if conversion resulted in a valid number
  if (isNaN(duration)) {
    return "Invalid";
  }
  
  // Handle zero case
  if (duration === 0) {
    return '0ms';
  }
  
  // Format based on duration length
  if (duration < 1000) {
    return `${duration}ms`;
  } else if (duration < 60000) {
    return `${(duration / 1000).toFixed(1)}s`;
  } else {
    const minutes = Math.floor(duration / 60000);
    const seconds = Math.floor((duration % 60000) / 1000);
    return `${minutes}m ${seconds}s`;
  }
};

// Helper function to format timestamp
const formatTimestamp = (timestamp?: number | null) => {
  if (!timestamp) return null;
  
  const date = new Date(timestamp);
  return date.toLocaleDateString([], { month: 'short', day: 'numeric' }) + ' ' + 
         date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit', second: '2-digit' });
};

export const MessageMetadata = ({ message }: MessageMetadataProps) => {
  const { useExperimentalCostTracking } = useGeneralSettingsStore(); // <-- Get the setting

  // Modify the initial null check to be less strict if cost tracking is off
  const hasTokenUsage = message.costInfo?.tokenUsage;
  // Only consider costInfo relevant for this check if tracking is enabled OR if modelGuid is present (for displaying model name)
  const hasDisplayableCostInfo = message.costInfo && (useExperimentalCostTracking || message.costInfo.modelGuid);
  const hasTimestamp = typeof message.timestamp === 'number' && message.timestamp > 0;
  const hasDuration = typeof message.durationMs === 'number' && message.durationMs > 0;

  if (!hasTokenUsage && !hasDisplayableCostInfo && !hasTimestamp && !hasDuration && !message.id) {
      return null;
  }

  return (
    <div className="ConvView text-small pt-1" style={{ /* ... */ }}>
      <div className="ConvView flex flex-wrap items-center gap-x-4 text-[0.75rem]">
        {/* Timestamp and duration info (existing) */}
        {(typeof message.timestamp === 'number' || typeof message.durationMs === 'number') && (
          <div className="flex items-center gap-x-2">
            {typeof message.timestamp === 'number' && message.timestamp > 0 && (
              <span title={new Date(message.timestamp).toLocaleString()}>
                {formatTimestamp(message.timestamp)}
              </span>
            )}
            {typeof message.durationMs === 'number' && message.durationMs > 0 && (
              <span title={`Response took ${message.durationMs}ms`}>
                Duration: {formatDuration(message)}
              </span>
            )}
          </div>
        )}

        {/* Token and Cost Info */}
        {message.costInfo && (
          <div className="flex items-center gap-x-2">
            <span>
              Tokens: {message.costInfo.tokenUsage.inputTokens} in / {message.costInfo.tokenUsage.outputTokens} out
            </span>
            {(message.costInfo.tokenUsage.cacheCreationInputTokens > 0 ||
              message.costInfo.tokenUsage.cacheReadInputTokens > 0) && (
                <span>
                  (Cache: {message.costInfo.tokenUsage.cacheCreationInputTokens} created,{' '}
                  {message.costInfo.tokenUsage.cacheReadInputTokens} read)
                </span>
              )}
            
            {/* Conditionally render Cost and Cumulative Cost */}
            {useExperimentalCostTracking && (
                <span className="flex items-center">Cost: ${message.costInfo.totalCost.toFixed(3)} (Cumulative: ${message.cumulativeCost?.toFixed(3)})</span>
            )}

            <span className="text-gray-500">
              (${message.costInfo.inputCostPer1M.toFixed(2)}/1M in, $
              {message.costInfo.outputCostPer1M.toFixed(2)}/1M out)
            </span>
            {(message.source === 'ai' || message.source === 'assistant') && typeof message.temperature === 'number' && (
                <span title={`AI response generated with temperature: ${message.temperature.toFixed(1)}`}>
                    Temp: {message.temperature.toFixed(1)}
                </span>
            )}
            {message.costInfo.modelGuid && (
              <span className="ml-1 text-gray-400 text-xs font-medium bg-gray-700 px-2 py-0.5 rounded-full">
                              {formatModelDisplay(message.costInfo.modelGuid)}
              </span>
            )}
          </div>
        )}
      </div>
    </div>
  );
};