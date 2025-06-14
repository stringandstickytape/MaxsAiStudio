import React from 'react';
import { MarkdownPane } from '@/components/MarkdownPane';
import { ContentBlockRendererProps } from './contentBlockRendererRegistry';

export const TextContentRenderer: React.FC<ContentBlockRendererProps> = ({ block }) => {
  if (!block.content) return null;

  return <MarkdownPane message={block.content} variant="default" />;
};