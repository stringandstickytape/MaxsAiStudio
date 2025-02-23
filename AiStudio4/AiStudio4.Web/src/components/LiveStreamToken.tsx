import React from 'react';
import { MarkdownPane } from './markdown-pane';

interface LiveStreamTokenProps {
  token: string;
}

export const LiveStreamToken: React.FC<LiveStreamTokenProps> = ({ token }) => {
  // Split on newlines and join with br elements
  const parts = token.split('\n').map((part, i, arr) => (
    <React.Fragment key={i}>
      {part}
      {i < arr.length - 1 && <br />}
    </React.Fragment>
  ));

  return (
    <span className="inline whitespace-normal">
      {parts}
    </span>
  );
};