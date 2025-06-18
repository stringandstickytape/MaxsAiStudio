import { FC } from 'react';

export interface CodeBlockRenderer {
  type: string[];
  initialize?: () => void;
  Component: FC<{ content: string; className?: string }>;
}
