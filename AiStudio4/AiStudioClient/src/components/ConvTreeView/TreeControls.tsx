// AiStudioClient\src\components\ConvTreeView\TreeControls.tsx
import React from 'react';

interface TreeControlsProps {
  onFocusLatest: () => void;
  onCenter: () => void;
  onZoomIn: () => void;
  onZoomOut: () => void;
}

/**
 * Component for tree view control buttons (zoom, focus, center)
 */
export const TreeControls: React.FC<TreeControlsProps> = ({
  onFocusLatest,
  onCenter,
  onZoomIn,
  onZoomOut
}) => {
  return (
    <div className="ConvTreeView absolute bottom-4 right-4 flex flex-col gap-2">
      <button
        onClick={onFocusLatest}
        className="ConvTreeView p-2 rounded-full shadow-lg"
        title="Focus on Latest Message"
        style={{
          backgroundColor: 'var(--convtree-bg, #1f2937)',
          color: 'var(--convtree-text-color, #ffffff)',
          boxShadow: '0 4px 6px -1px rgba(0, 0, 0, 0.1), 0 2px 4px -1px rgba(0, 0, 0, 0.06)',
        }}
      >
        <svg xmlns="http://www.w3.org/2000/svg" className="h-5 w-5" viewBox="0 0 20 20" fill="currentColor">
          <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm1-11a1 1 0 10-2 0v3.586L7.707 9.293a1 1 0 00-1.414 1.414l3 3a1 1 0 001.414 0l3-3a1 1 0 10-1.414-1.414L11 10.586V7z" clipRule="evenodd" />
        </svg>
      </button>

      <button
        onClick={onCenter}
        className="ConvTreeView p-2 rounded-full shadow-lg"
        title="Center View"
        style={{
          backgroundColor: 'var(--convtree-bg, #1f2937)',
          color: 'var(--convtree-text-color, #ffffff)',
          boxShadow: '0 4px 6px -1px rgba(0, 0, 0, 0.1), 0 2px 4px -1px rgba(0, 0, 0, 0.06)',
        }}
      >
        <svg xmlns="http://www.w3.org/2000/svg" className="h-5 w-5" viewBox="0 0 20 20" fill="currentColor">
          <path
            fillRule="evenodd"
            d="M10 18a8 8 0 100-16 8 8 0 000 16zm0-2a6 6 0 100-12 6 6 0 000 12zm0-8a2 2 0 11-4 0 2 2 0 014 0z"
            clipRule="evenodd"
          />
        </svg>
      </button>
      
      <button
        onClick={onZoomIn}
        className="ConvTreeView p-2 rounded-full shadow-lg"
        title="Zoom In"
        style={{
          backgroundColor: 'var(--convtree-bg, #1f2937)',
          color: 'var(--convtree-text-color, #ffffff)',
          boxShadow: '0 4px 6px -1px rgba(0, 0, 0, 0.1), 0 2px 4px -1px rgba(0, 0, 0, 0.06)',
        }}
      >
        <svg xmlns="http://www.w3.org/2000/svg" className="h-5 w-5" viewBox="0 0 20 20" fill="currentColor">
          <path
            fillRule="evenodd"
            d="M10 5a1 1 0 011 1v3h3a1 1 0 110 2h-3v3a1 1 0 11-2 0v-3H6a1 1 0 110-2h3V6a1 1 0 011-1z"
            clipRule="evenodd"
          />
        </svg>
      </button>
      
      <button
        onClick={onZoomOut}
        className="ConvTreeView p-2 rounded-full shadow-lg"
        title="Zoom Out"
        style={{
          backgroundColor: 'var(--convtree-bg, #1f2937)',
          color: 'var(--convtree-text-color, #ffffff)',
          boxShadow: '0 4px 6px -1px rgba(0, 0, 0, 0.1), 0 2px 4px -1px rgba(0, 0, 0, 0.06)',
        }}
      >
        <svg xmlns="http://www.w3.org/2000/svg" className="h-5 w-5" viewBox="0 0 20 20" fill="currentColor">
          <path fillRule="evenodd" d="M5 10a1 1 0 011-1h8a1 1 0 110 2H6a1 1 0 01-1-1z" clipRule="evenodd" />
        </svg>
      </button>
    </div>
  );
};