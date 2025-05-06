// MarkdownPane/CodeBlockHeader.tsx
import React from 'react';
import { ExternalLink, Clipboard, ChevronDown, ChevronUp } from 'lucide-react';

export interface CodeBlockHeaderProps {
    language: string;
    content: string;
    isCollapsed: boolean;
    isRawView: boolean;
    isVisualStudio: boolean;
    onToggleRaw: () => void;
    onToggleCollapse: () => void;
    launchHtml?: () => void;
}

export const CodeBlockHeader: React.FC<CodeBlockHeaderProps> = ({
    language,
    content,
    isCollapsed,
    isRawView,
    isVisualStudio,
    onToggleRaw,
    onToggleCollapse,
    launchHtml,
}) => {
    return (
        <div
            className={`MarkdownPane flex items-center justify-between px-2 py-1 rounded-t-xl border-b text-sm`}
            style={{
                background: 'var(--global-backgroundColor, #181c20)',
                color: 'var(--global-textColor, #bfc7d5)',
                borderColor: 'var(--global-borderColor, #283040)',
                borderStyle: 'solid',
                borderBottomWidth: '1px',
                borderTopWidth: 0,
            }}
        >
            <div className="flex items-center space-x-2">
                <button
                    onClick={onToggleCollapse}
                    className="transition-colors p-1"
                    style={{ color: 'var(--global-primaryColor, #4f8cff)' }}
                >
                    {isCollapsed ? <ChevronDown size={16} /> : <ChevronUp size={16} />}
                </button>
                <div className="font-medium">{language}</div>
            </div>
            <div className="flex space-x-2">
                {isVisualStudio && (
                    <button
                        onClick={() => window.chrome.webview.postMessage({ type: 'applyNewDiff', content: content.trim() })}
                        className="px-2 py-1 rounded transition-colors"
                        style={{
                            background: 'var(--global-primaryColor, #4f8cff)',
                            color: 'var(--global-backgroundColor, #181c20)',
                        }}
                    >
                        Apply Diff
                    </button>
                )}
                {launchHtml && (
                    <button
                        onClick={launchHtml}
                        className="text-small-gray-400 bg-gray-800 px-2 py-1 rounded hover:bg-gray-700 transition-colors flex items-center gap-1"
                    >
                        <ExternalLink className="h-3 w-3" />
                        Launch
                    </button>
                )}
                <button
                    onClick={() => navigator.clipboard.writeText(content)}
                    className="px-2 py-1 rounded transition-colors"
                    style={{
                        background: 'var(--global-primaryColor, #4f8cff)',
                        color: 'var(--global-backgroundColor, #181c20)',
                    }}
                >
                    Copy
                </button>
                <button
                    onClick={onToggleRaw}
                    className="px-2 py-1 rounded transition-colors"
                    style={{
                        background: 'var(--global-primaryColor, #4f8cff)',
                        color: 'var(--global-backgroundColor, #181c20)',
                    }}
                >
                    {isRawView ? 'Show Rendered' : 'Show Raw'}
                </button>
            </div>
        </div>
    );
};