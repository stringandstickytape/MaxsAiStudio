// AiStudioClient\src\components\ConvView\MessageMetadata.tsx
import { formatModelDisplay } from '@/utils/modelUtils';
import { useGeneralSettingsStore } from '@/stores/useGeneralSettingsStore';
import React from 'react'; // Added React import

interface MessageMetadataProps {
  message: any;
}

// Custom comparison function for MessageMetadata memoization
const areMetadataPropsEqual = (prevProps: MessageMetadataProps, nextProps: MessageMetadataProps) => {
  const prevMsg = prevProps.message;
  const nextMsg = nextProps.message;
  
  if (!prevMsg && !nextMsg) return true;
  if (!prevMsg || !nextMsg) return false;
  
  // Compare properties that affect metadata display
  if (prevMsg.id !== nextMsg.id) return false;
  if (prevMsg.timestamp !== nextMsg.timestamp) return false;
  if (prevMsg.durationMs !== nextMsg.durationMs) return false;
  if (prevMsg.temperature !== nextMsg.temperature) return false;
  if (prevMsg.source !== nextMsg.source) return false;
  if (prevMsg.cumulativeCost !== nextMsg.cumulativeCost) return false;
  
  // Deep compare costInfo object
  const prevCost = prevMsg.costInfo;
  const nextCost = nextMsg.costInfo;
  
  if (!prevCost && !nextCost) return true;
  if (!prevCost || !nextCost) return false;
  
  if (prevCost.totalCost !== nextCost.totalCost) return false;
  if (prevCost.inputCostPer1M !== nextCost.inputCostPer1M) return false;
  if (prevCost.outputCostPer1M !== nextCost.outputCostPer1M) return false;
  if (prevCost.modelGuid !== nextCost.modelGuid) return false;
  
  // Compare tokenUsage
  const prevTokens = prevCost.tokenUsage;
  const nextTokens = nextCost.tokenUsage;
  
  if (!prevTokens && !nextTokens) return true;
  if (!prevTokens || !nextTokens) return false;
  
  if (prevTokens.inputTokens !== nextTokens.inputTokens) return false;
  if (prevTokens.outputTokens !== nextTokens.outputTokens) return false;
  if (prevTokens.cacheCreationInputTokens !== nextTokens.cacheCreationInputTokens) return false;
  if (prevTokens.cacheReadInputTokens !== nextTokens.cacheReadInputTokens) return false;
  
  return true;
};

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
    return date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })  + ' ' + 
        date.toLocaleDateString([], { month: 'short', day: 'numeric' });
};

export const MessageMetadata = React.memo(({ message }: MessageMetadataProps) => {
    const metadataItems = [];

    // Timestamp
    if (typeof message.timestamp === 'number' && message.timestamp > 0) {
        metadataItems.push(
            <span key="timestamp" title={new Date(message.timestamp).toLocaleString()}>
                {formatTimestamp(message.timestamp)}
            </span>
        );
    }

    // Duration
    if (typeof message.durationMs === 'number' && message.durationMs > 0) {
        metadataItems.push(
            <span key="duration" title={`Response took ${message.durationMs}ms`}>
                {formatDuration(message)}
            </span>
        );
    }

    // Token and Cost Info
    if (message.costInfo) {
        metadataItems.push(
            <span key="tokens">
                {message.costInfo.tokenUsage.inputTokens} ⬆️ {message.costInfo.tokenUsage.outputTokens} ⬇️
            </span>
        );
    

    if (message.costInfo.tokenUsage.cacheCreationInputTokens > 0 || message.costInfo.tokenUsage.cacheReadInputTokens > 0) {
        metadataItems.push(
            <span key="cache">
                Cache {message.costInfo.tokenUsage.cacheCreationInputTokens} new,{' '}
                {message.costInfo.tokenUsage.cacheReadInputTokens} read
            </span>
        );
    }

    metadataItems.push(
        <span key="cost" className="flex items-center">
            ${message.costInfo.totalCost.toFixed(2)}  Σ${message.cumulativeCost?.toFixed(2)}
        </span>
    );

    if (message.costInfo.modelGuid) {
        metadataItems.push(
            <span key="model">
                {formatModelDisplay(message.costInfo.modelGuid)}
            </span>
        );
    }

    


}


    if (metadataItems.length === 0 && !message.id) {
        return null;
    }


  return (
    <div className="ConvView flex flex-row flex-nowrap items-center text-[0.75rem] whitespace-nowrap overflow-x-hidden pb-1">
      {metadataItems.map((item, index) => (
        <React.Fragment key={index}>
          {item}
          {index < metadataItems.length - 1 && <span className="ConvView mx-2 text-gray-500">|</span>}
        </React.Fragment>
      ))}
    </div>
  );
}, areMetadataPropsEqual);