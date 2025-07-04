import React from 'react';
import { CodeBlockRenderer } from './types';
import { MarpRenderer } from '../renderers/marp-renderer';
import matter from 'gray-matter';

interface MarkdownMarpComponentProps {
  content: string;
  className?: string;
}

const MarkdownMarpComponent: React.FC<MarkdownMarpComponentProps> = ({ content, className }) => {
  // Parse the markdown content to check for Marp frontmatter
  try {
    const parsed = matter(content);
    const isMarp = parsed.data?.marp === true;
    
    if (isMarp) {
      // Render as Marp presentation
      return (
        <div className={`marp-code-block-container ${className || ''}`}>
          <MarpRenderer
            markdown={parsed.content}
            frontmatter={parsed.data}
          />
        </div>
      );
    }
  } catch (error) {
    console.error('Error parsing markdown for Marp:', error);
  }
  
  // Fallback: render as plain text if not valid Marp
  return (
    <pre className={`text-sm ${className || ''}`}>
      <code>{content}</code>
    </pre>
  );
};

export const MarkdownMarpRenderer: CodeBlockRenderer = {
  type: ['markdown'],
  Component: MarkdownMarpComponent,
  initialize: () => {
    console.log('Markdown Marp renderer initialized');
  }
};