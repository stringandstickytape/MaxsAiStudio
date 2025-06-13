import React from 'react';
import { MarkdownPane } from '@/components/MarkdownPane';
import { ContentBlockRendererProps } from './contentBlockRendererRegistry';

export const SystemContentRenderer: React.FC<ContentBlockRendererProps> = ({ block }) => {
  if (!block.content) return null;

  return <MarkdownPane message={block.content} variant="system" />;
};