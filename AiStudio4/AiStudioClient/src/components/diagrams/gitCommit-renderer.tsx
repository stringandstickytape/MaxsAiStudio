// AiStudioClient/src/components/diagrams/gitCommit-renderer.tsx
import React, { FC } from 'react';
import { CheckCircle, XCircle, UploadCloud, FileText, AlertTriangle } from 'lucide-react';
import { CodeBlockRenderer } from '@/components/diagrams/types';

interface GitCommitData {
  overallSuccess: boolean;
  committedFiles: string[];
  commitMessage: string;
  pushRequested: boolean;
  pushNewBranch: boolean;
  errors: string[];
  summary?: string;
}

const GitCommitRendererComponent: FC<{ content: string; className?: string }> = ({ content, className }) => {
  let data: GitCommitData | null = null;
  let parseError: string | null = null;

  try {
    data = JSON.parse(content);
  } catch (error) {
    parseError = error instanceof Error ? error.message : 'Failed to parse JSON';
    console.error('GitCommitRenderer: Failed to parse JSON', error);
  }

  if (parseError || !data) {
    return (
      <div className={`p-4 border border-red-500 rounded ${className || ''}`.trim()}>
        <p className="text-red-600 font-bold mb-2">Error rendering GitCommit:</p>
        {parseError && <p className="mb-2">{parseError}</p>}
        <p className="mb-2">Raw content:</p>
        <pre className="whitespace-pre-wrap break-words bg-gray-800 text-gray-300 p-2 rounded text-xs">
          {content}
        </pre>
      </div>
    );
  }

  const {
    overallSuccess,
    committedFiles,
    commitMessage,
    pushRequested,
    pushNewBranch,
    errors,
    summary,
  } = data;

  return (
    <div
      className={`p-4 border rounded-md ${className || ''}`.trim()}
      style={{
        backgroundColor: 'var(--global-background-color)',
        color: 'var(--global-text-color)',
        fontSize: '14px',
      }}
    >
      {/* Header */}
      <div className="flex items-center gap-2 mb-4">
        {overallSuccess ? (
          <CheckCircle className="w-5 h-5" style={{ color: 'var(--global-success-color)' }} />
        ) : (
          <XCircle className="w-5 h-5" style={{ color: 'var(--global-error-color)' }} />
        )}
        <h3 className="text-lg font-semibold" style={{ color: 'var(--global-accent-color)' }}>
          Git Commit {overallSuccess ? 'Succeeded' : 'Failed'}
        </h3>
      </div>

      {/* Commit message */}
      <div className="mb-4">
        <div className="text-xs font-medium mb-1" style={{ color: 'var(--global-muted-text-color)' }}>
          Commit Message
        </div>
        <pre
          className="whitespace-pre-wrap break-words p-2 rounded text-xs font-mono"
          style={{
            backgroundColor: 'var(--global-secondary-background-color)',
            color: 'var(--global-text-color)',
            border: '1px solid var(--global-border-color)',
          }}
        >
          {commitMessage}
        </pre>
      </div>

      {/* Files committed */}
      <div className="mb-4">
        <div className="flex items-center gap-2 mb-2">
          <FileText className="w-4 h-4" />
          <span className="font-medium">{committedFiles.length} file(s) committed</span>
        </div>
        <ul className="text-xs ml-6 list-disc" style={{ color: 'var(--global-text-color)' }}>
          {committedFiles.map((file, idx) => (
            <li key={idx} className="break-all">
              {file}
            </li>
          ))}
        </ul>
      </div>

      {/* Push info */}
      <div className="mb-4 flex items-center gap-2 text-sm">
        <UploadCloud className="w-4 h-4" />
        <span>
          Push: {pushRequested ? 'Yes' : 'No'} {pushRequested && !overallSuccess ? '(may have failed)' : ''}
          {pushRequested && pushNewBranch ? ' (create new branch if needed)' : ''}
        </span>
      </div>

      {/* Errors */}
      {errors && errors.length > 0 && (
        <div className="mb-4">
          <div className="flex items-center gap-2 mb-2">
            <AlertTriangle className="w-4 h-4" style={{ color: 'var(--global-error-color)' }} />
            <span className="font-medium" style={{ color: 'var(--global-error-color)' }}>
              Errors ({errors.length})
            </span>
          </div>
          <ul className="text-xs ml-6 list-disc" style={{ color: 'var(--global-error-color)' }}>
            {errors.map((err, idx) => (
              <li key={idx} className="break-all">
                {err}
              </li>
            ))}
          </ul>
        </div>
      )}

      {/* Summary */}
      {summary && (
        <div className="mb-2">
          <div className="text-xs font-medium mb-1" style={{ color: 'var(--global-muted-text-color)' }}>
            Summary
          </div>
          <pre
            className="whitespace-pre-wrap break-words p-2 rounded text-xs font-mono"
            style={{
              backgroundColor: 'var(--global-secondary-background-color)',
              color: 'var(--global-text-color)',
              border: '1px solid var(--global-border-color)',
            }}
          >
            {summary}
          </pre>
        </div>
      )}
    </div>
  );
};

export const GitCommitRenderer: CodeBlockRenderer = {
  type: ['gitcommit'],
  initialize: () => {
    // No initialization necessary
  },
  render: async () => {
    // No async work needed per block; React handles rendering.
  },
  Component: GitCommitRendererComponent,
};