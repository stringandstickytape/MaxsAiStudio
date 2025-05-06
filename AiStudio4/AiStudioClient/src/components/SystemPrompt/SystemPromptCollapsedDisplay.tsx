// AiStudioClient/src/components/SystemPrompt/SystemPromptCollapsedDisplay.tsx
import React from 'react';
import { cn } from '@/lib/utils';

interface SystemPromptCollapsedDisplayProps {
  isVisible: boolean;
  onClick: () => void;
  title: string;
}

export function SystemPromptCollapsedDisplay({ isVisible, onClick, title }: SystemPromptCollapsedDisplayProps) {
  return (
    <div
      className={cn(
        'absolute inset-0 cursor-pointer transition-opacity duration-200 ease-in-out',
        isVisible ? 'opacity-100' : 'opacity-0 pointer-events-none'
      )}
      onClick={onClick}
    >
      <span className="text-sm truncate block w-full">System Prompt: {title}</span>
    </div>
  );
}