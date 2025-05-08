// src/components/diagrams/txt-renderer.tsx
import { CodeBlockRenderer } from '@/components/diagrams/types';

export const TxtRenderer: CodeBlockRenderer = {
  type: ['txt', 'text'],
  initialize: () => {
    // No initialization needed for plain text
  },
  render: async () => {
    // No async rendering needed for plain text
  },
  Component: ({ content, className }) => {
    return (
      <pre 
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
      >
        {content}
      </pre>
    );
  },
};
