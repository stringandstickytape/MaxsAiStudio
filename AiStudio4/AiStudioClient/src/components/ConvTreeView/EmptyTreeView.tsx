// AiStudioClient\src\components\ConvTreeView\EmptyTreeView.tsx
import React from 'react';

/**
 * Component to display when there are no messages to show in the tree view
 */
export const EmptyTreeView: React.FC = () => {
  return (
    <div className="ConvTreeView text-center p-4 rounded-md shadow-inner mx-auto my-8 max-w-md border"
      style={{
        backgroundColor: 'var(--convtree-bg, #111827)',
        color: 'var(--convtree-text-color, #9ca3af)',
        borderColor: 'var(--convtree-border-color, #1f2937)',
        boxShadow: 'inset 0 2px 4px 0 rgba(0, 0, 0, 0.3)'
      }}
    >
      <p>No conv history to display</p>
      <p className="ConvTreeView text-sm mt-2"
        style={{
          color: 'var(--convtree-text-color, #6b7280)'
        }}
      >Start a new conv to see the tree view</p>
    </div>
  );
};