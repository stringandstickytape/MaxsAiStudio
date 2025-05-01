import { FC } from 'react';

export interface CodeBlockRenderer {
  type: string[];
  initialize?: () => void;
  render: (content: string) => Promise<void>;
  Component: FC<{ content: string; className?: string }>;
}
