// AiStudioClient\src\components\diagrams\codeDiff-renderer.tsx
import React, { FC } from 'react';
import { CodeBlockRenderer } from '@/components/diagrams/types';

interface CodeDiffChange {
  change_type: string;
  description?: string;
  lineNumber?: number;
  oldContent?: string;
  newContent?: string;
}

interface CodeDiffFile {
  path: string;
  changes: CodeDiffChange[];
}

interface CodeDiffChangeset {
  description: string;
  files: CodeDiffFile[];
}

interface CodeDiffData {
  changeset: CodeDiffChangeset;
}

const CodeDiffRendererComponent: FC<{ content: string; className?: string }> = ({ content, className }) => {
  let data: CodeDiffData | null = null;
  let parseError: string | null = null;

  try {
    data = JSON.parse(content);
  } catch (error) {
    parseError = error instanceof Error ? error.message : 'Failed to parse JSON';
    console.error('CodeDiffRenderer: Failed to parse JSON', error);
  }

  if (parseError || !data) {
    return (
      <div className={`p-4 border border-red-500 rounded ${className || ''}`}>
        <p className="text-red-600 font-bold mb-2">Error rendering CodeDiff:</p>
        {parseError && <p className="mb-2">{parseError}</p>}
        <p className="mb-2">Raw content:</p>
        <pre 
          className="whitespace-pre-wrap break-words bg-gray-800 text-gray-300 p-2 rounded text-xs"
        >
          {content}
        </pre>
      </div>
    );
  }

  const { changeset } = data;

  return (
    <div className={`p-2 border rounded-md bg-gray-900 text-gray-200 font-sans text-sm ${className || ''}`}>
      <h3 className="text-lg font-semibold mb-3 border-b border-gray-700 pb-2">{changeset.description}</h3>
      {changeset.files.map((file, fileIndex) => (
        <div key={fileIndex} className="mb-4 p-3 bg-gray-800 rounded">
          <p className="font-mono text-xs text-blue-400 break-all mb-2">{file.path}</p>
          {file.changes.map((change, changeIndex) => (
            <div key={changeIndex} className="mb-3 p-2 border border-gray-700 rounded bg-gray-850">
              <p className="text-xs italic text-gray-400 mb-1">
                {change.description || 'Change details:'} ({change.change_type}{change.lineNumber ? ` near line ${change.lineNumber}` : ''})
              </p>
              {change.oldContent && (
                <div className="mb-1">
                  <span className="text-red-400 text-xs font-semibold">- Removed:</span>
                  <pre 
                    className="whitespace-pre-wrap break-words bg-red-900/20 text-red-300 p-2 rounded text-xs border border-red-700/50 mt-1 font-mono"
                  >
                    {change.oldContent}
                  </pre>
                </div>
              )}
              {change.newContent && (
                 <div className="mt-1">
                  <span className="text-green-400 text-xs font-semibold">+ Added:</span>
                  <pre 
                    className="whitespace-pre-wrap break-words bg-green-900/20 text-green-300 p-2 rounded text-xs border border-green-700/50 mt-1 font-mono"
                  >
                    {change.newContent}
                  </pre>
                 </div>
              )}
            </div>
          ))}
        </div>
      ))}
    </div>
  );
};

export const CodeDiffRenderer: CodeBlockRenderer = {
  type: ['codediff'],
    initialize: () => {
    // No initialization needed
  },
  render: async () => {
      // No async rendering needed
  },
  Component: CodeDiffRendererComponent,
};