import React from 'react';
import { MarkdownPane } from './markdown-pane';

interface LiveStreamTokenProps {
  token: string;
}

export const LiveStreamToken: React.FC<LiveStreamTokenProps> = ({ token }) => {
  return (
    <span className="inline whitespace-pre-wrap">
      {token}
    </span>
  );
};