// src/components/diagrams/txt-renderer.tsx
import { CodeBlockRenderer } from '@/components/diagrams/types';

import React, { useRef, useLayoutEffect } from 'react';

export const TxtRenderer: CodeBlockRenderer = {
  type: ['txt', 'text'],
  initialize: () => {
    // No initialization needed for plain text
  },
  render: async () => {
    // No async rendering needed for plain text
  },
  Component: React.memo(({ content, className }) => {
    const preRef = useRef<HTMLPreElement>(null);
    const previousContent = useRef<string>(content);
    const savedScrollTop = useRef<number>(0);
    
    // Preserve scroll position during content updates
    useLayoutEffect(() => {
      const pre = preRef.current;
      if (!pre) return;
      
      // If content is growing (streaming case), preserve scroll position
      if (content.length > previousContent.current.length && 
          content.startsWith(previousContent.current) &&
          savedScrollTop.current > 0) {
        pre.scrollTop = savedScrollTop.current;
      }
      
      previousContent.current = content;
    }, [content]);
    
    const handleScroll = (e: React.UIEvent<HTMLPreElement>) => {
      savedScrollTop.current = e.currentTarget.scrollTop;
    };

    return (
      <pre 
        ref={preRef}
        className={`whitespace-pre-wrap break-words ${className || ''}`}
        style={{ 
          backgroundColor: 'var(--vscode-editor-background)', 
          color: 'var(--vscode-editor-foreground)',
          padding: '10px',
          borderRadius: '4px',
          fontFamily: 'var(--vscode-editor-font-family)',
          fontSize: 'var(--vscode-editor-font-size)',
          lineHeight: 'var(--vscode-editor-line-height)',
          overflowX: 'auto' 
        }}
        onScroll={handleScroll}
      >
        {content}
      </pre>
    );
  }),
};
