// AiStudioClient/src/components/diagrams/modifyFiles-renderer.tsx
import React, { FC, useState } from 'react';
import { CodeBlockRenderer } from '@/components/diagrams/types';
import { ChevronDown, ChevronRight, Folder, File, CheckCircle, XCircle, Clock, Copy } from 'lucide-react';

interface ModifyFilesChange {
  description: string;
}

interface ModifyFilesFile {
  path: string;
  status: 'modified' | 'failed';
  message: string;
  changeCount: number;
  changes: ModifyFilesChange[];
}

interface ModifyFilesSummary {
  totalFiles: number;
  totalChanges: number;
  success: boolean;
  successfulFiles?: number;
  failedFiles?: number;
  error?: string;
}

interface ModifyFilesData {
  summary: ModifyFilesSummary;
  files: ModifyFilesFile[];
}

const ModifyFilesRendererComponent: FC<{ content: string; className?: string }> = ({ content, className }) => {

  let data: ModifyFilesData | null = null;
  let parseError: string | null = null;

  try {
    data = JSON.parse(content);
  } catch (error) {
    parseError = error instanceof Error ? error.message : 'Failed to parse JSON';
    console.error('ModifyFilesRenderer: Failed to parse JSON', error);
  }

  if (parseError || !data) {
    return (
      <div className={`p-4 border border-red-500 rounded ${className || ''}`}>
        <p className="text-red-600 font-bold mb-2">Error rendering ModifyFiles:</p>
        {parseError && <p className="mb-2">{parseError}</p>}
        <p className="mb-2">Raw content:</p>
        <pre className="whitespace-pre-wrap break-words bg-gray-800 text-gray-300 p-2 rounded text-xs">
          {content}
        </pre>
      </div>
    );
  }

  return (
    <div className={`p-4 border rounded-md ${className || ''}`} style={{ backgroundColor: 'var(--global-background-color)', color: 'var(--global-text-color)', fontSize: '14px' }}>
      <h3 className="text-lg font-semibold mb-4" style={{ color: 'var(--global-accent-color)' }}>File Modifications</h3>
      
      {/* Summary */}
      {data.summary && (
        <div className="mb-4 p-3 rounded border" style={{ backgroundColor: 'var(--global-secondary-background-color)' }}>
          <div className="flex items-center gap-2 mb-2">
            {data.summary.success ? (
              <CheckCircle className="w-4 h-4" style={{ color: 'var(--global-success-color)' }} />
            ) : (
              <XCircle className="w-4 h-4" style={{ color: 'var(--global-error-color)' }} />
            )}
            <span style={{ color: data.summary.success ? 'var(--global-success-color)' : 'var(--global-error-color)' }}>
              {data.summary.success ? 'Success' : 'Failed'}
            </span>
          </div>
          <div className="text-xs" style={{ color: 'var(--global-muted-text-color)' }}>
            {data.summary.totalFiles} file(s), {data.summary.totalChanges} change(s)
          </div>
        </div>
      )}
      
      {/* Files */}
      {data.files?.map((file, fileIndex) => (
        <div key={fileIndex} className="mb-6 border rounded p-4" style={{ borderColor: 'var(--global-border-color)' }}>
          <h4 className="font-medium mb-3 break-all" style={{ color: 'var(--global-accent-color)' }}>
            {file.path}
          </h4>
          
          <div className="space-y-3">
            {file.changes?.map((change, changeIndex) => (
              <div key={changeIndex} className="border-l-2 pl-3" style={{ borderLeftColor: 'var(--global-accent-color)' }}>
                <p className="text-sm mb-2" style={{ color: 'var(--global-text-color)' }}>
                  {change.description}
                </p>
              </div>
            ))}
          </div>
        </div>
      ))}
      
      {parseError && <p className="mb-2">{data.toString()}</p>}
    </div>
  );
};

export const ModifyFilesRenderer: CodeBlockRenderer = {
  type: ['modifyfiles'],
  initialize: () => {
    // No initialization needed
  },
  render: async () => {
    // No async rendering needed
  },
  Component: ModifyFilesRendererComponent,
};