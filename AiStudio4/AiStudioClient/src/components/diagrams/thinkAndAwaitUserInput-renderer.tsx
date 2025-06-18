// AiStudioClient/src/components/diagrams/thinkAndAwaitUserInput-renderer.tsx
import React, { FC } from 'react';
import { CodeBlockRenderer } from '@/components/diagrams/types';
import { Brain, Clock, MessageCircle } from 'lucide-react';

interface ThinkAndAwaitUserInputData {
  thought: string;
  timestamp?: string;
  status?: string;
}

const ThinkAndAwaitUserInputRendererComponent: FC<{ content: string; className?: string }> = ({ content, className }) => {
  let data: ThinkAndAwaitUserInputData | null = null;
  let parseError: string | null = null;
  let isPlainText = false;

  // Try to parse as JSON first
  try {
    data = JSON.parse(content);
  } catch (error) {
    // If JSON parsing fails, treat as plain text thought
    isPlainText = true;
    data = { thought: content.trim() };
  }

  if (!data || !data.thought) {
    return (
      <div className={`p-4 border border-red-500 rounded ${className || ''}`}>
        <p className="text-red-600 font-bold mb-2">Error rendering ThinkAndAwaitUserInput:</p>
        <p className="mb-2">No thought content found</p>
        <pre className="whitespace-pre-wrap break-words bg-gray-800 text-gray-300 p-2 rounded text-xs">
          {content}
        </pre>
      </div>
    );
  }

  return (
    <div className={`p-4 border rounded-md ${className || ''}`} style={{ backgroundColor: 'var(--global-background-color)', color: 'var(--global-text-color)', fontSize: '14px' }}>
      {/* Header */}
      <div className="flex items-center gap-2 mb-4">
        <Brain className="w-5 h-5" style={{ color: 'var(--global-accent-color)' }} />
        <h3 className="text-lg font-semibold" style={{ color: 'var(--global-accent-color)' }}>
          AI Thinking Process
        </h3>
        <div className="flex items-center gap-1 ml-auto">
          <Clock className="w-4 h-4" style={{ color: 'var(--global-muted-text-color)' }} />
          <span className="text-xs" style={{ color: 'var(--global-muted-text-color)' }}>
            Awaiting User Input
          </span>
        </div>
      </div>

      {/* Status indicator */}
      <div className="mb-4 p-3 rounded border" style={{ backgroundColor: 'var(--global-secondary-background-color)', borderColor: 'var(--global-border-color)' }}>
        <div className="flex items-center gap-2">
          <MessageCircle className="w-4 h-4" style={{ color: 'var(--global-warning-color)' }} />
          <span className="text-sm font-medium" style={{ color: 'var(--global-warning-color)' }}>
            Processing Paused - User Input Required
          </span>
        </div>
        <div className="text-xs mt-1" style={{ color: 'var(--global-muted-text-color)' }}>
          The AI has completed its analysis and is waiting for your response to continue.
        </div>
      </div>
      
      {/* Thought content */}
      <div className="border-l-4 pl-4" style={{ borderLeftColor: 'var(--global-accent-color)' }}>
        <h4 className="font-medium mb-2" style={{ color: 'var(--global-text-color)' }}>
          Analysis & Reasoning:
        </h4>
        <div className="whitespace-pre-wrap text-sm leading-relaxed" style={{ color: 'var(--global-text-color)' }}>
          {data.thought}
        </div>
      </div>

      {/* Timestamp if available */}
      {data.timestamp && (
        <div className="mt-4 pt-2 border-t" style={{ borderTopColor: 'var(--global-border-color)' }}>
          <span className="text-xs" style={{ color: 'var(--global-muted-text-color)' }}>
            Timestamp: {data.timestamp}
          </span>
        </div>
      )}

      {/* Debug info for development */}
      {isPlainText && process.env.NODE_ENV === 'development' && (
        <div className="mt-2 text-xs" style={{ color: 'var(--global-muted-text-color)' }}>
          <em>Note: Content parsed as plain text</em>
        </div>
      )}
    </div>
  );
};

export const ThinkAndAwaitUserInputRenderer: CodeBlockRenderer = {
  type: ['thinkandawaituserinput', 'think-and-await-user-input', 'think'],
  initialize: () => {
    // No initialization needed
  },
  Component: ThinkAndAwaitUserInputRendererComponent,
};