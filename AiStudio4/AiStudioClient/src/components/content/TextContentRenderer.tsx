import React from 'react';
import { MarkdownPane } from '@/components/MarkdownPane';
import { ContentBlockRendererProps } from './contentBlockRendererRegistry';

export const TextContentRenderer: React.FC<ContentBlockRendererProps> = ({ block, messageId }) => {
  if (!block.content) return null;

  return <MarkdownPane message={block.content} messageId={messageId} variant="default" />;
};