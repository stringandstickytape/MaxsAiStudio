import React from 'react';
import { MarkdownPane } from '@/components/MarkdownPane';
import { useDebugStore } from '@/stores/useDebugStore';
import { ContentBlockRendererProps } from './contentBlockRendererRegistry';

export const AiHiddenContentRenderer: React.FC<ContentBlockRendererProps> = ({ block }) => {
  const showDevContentView = useDebugStore((state) => state.showDevContentView);

  //if (!showDevContentView) {
  //  return null;
  //}

  return (
    <div className="my-2 border-2 border-dashed border-yellow-500/50 bg-yellow-900/10 p-3 rounded-lg opacity-80">
      <div className="text-xs font-bold text-yellow-400 mb-1">
        AI Context
      </div>
      <MarkdownPane message={block.content} />
    </div>
  );
};