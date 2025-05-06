// MarkdownPane/CodeBlock.tsx
import React, { useCallback } from 'react';
import { Prism as SyntaxHighlighter } from 'react-syntax-highlighter';
import { nightOwl } from 'react-syntax-highlighter/dist/cjs/styles/prism';
import { CodeBlockHeader } from './CodeBlockHeader';

export interface CodeBlockProps {
    language: string;
    content: string;
    diagramRenderer?: any;
    isVisualStudio: boolean;
    blockId: string;
    isRawView: boolean;
    isCollapsed: boolean;
    mermaidKey: number;
    onToggleRaw: () => void;
    onToggleCollapse: () => void;
    launchHtml: (content: string) => void;
}

export const CodeBlock: React.FC<CodeBlockProps> = ({
    language,
    content,
    diagramRenderer,
    isVisualStudio,
    blockId,
    isRawView,
    isCollapsed,
    mermaidKey,
    onToggleRaw,
    onToggleCollapse,
    launchHtml,
}) => {
    const isHtmlBlock = language === 'html' || language === 'htm';
    const DiagramComponent = diagramRenderer ? diagramRenderer.Component : null;

    // Header
    const header = (
        <CodeBlockHeader
            language={language}
            content={content}
            isCollapsed={isCollapsed}
            isRawView={isRawView}
            isVisualStudio={isVisualStudio}
            onToggleRaw={onToggleRaw}
            onToggleCollapse={onToggleCollapse}
            launchHtml={isHtmlBlock ? () => launchHtml(content) : undefined}
        />
    );

    // Diagram block
    if (DiagramComponent) {
        return isRawView ? (
            <div className="rounded-xl overflow-hidden border border-gray-700 shadow-lg my-2">
                {header}
                <div className={`code-content ${isCollapsed ? 'collapsed' : ''} p-4 bg-gray-800 rounded-b-lg`}>
                    <pre style={{ whiteSpace: 'break-spaces' }}>{content}</pre>
                </div>
            </div>
        ) : (
            <div className="rounded-xl overflow-hidden border border-gray-700 shadow-lg my-2" key={mermaidKey}>
                {header}
                <div className={`code-content ${isCollapsed ? 'collapsed' : ''} p-4 bg-gray-800 rounded-b-lg diagram-container`} data-type={diagramRenderer.type[0]} data-content={content}>
                    <DiagramComponent content={content} className="overflow-auto" />
                </div>
            </div>
        );
    }

    // Regular code block
    return isRawView ? (
        <div className="rounded-xl overflow-hidden border border-gray-700 shadow-lg my-2">
            {header}
            <div className={`code-content ${isCollapsed ? 'collapsed' : ''} p-4 bg-gray-800/40 backdrop-blur-sm shadow-inner border-t border-gray-700/30 rounded-b-xl`}>
                <pre style={{ whiteSpace: 'break-spaces' }}>{content}</pre>
            </div>
        </div>
    ) : (
        <div className="rounded-xl overflow-hidden border border-gray-700 shadow-lg my-2">
            {header}
            <div className={`code-content ${isCollapsed ? 'collapsed' : ''} p-4 bg-gray-800/40 backdrop-blur-sm shadow-inner border-t border-gray-700/30 rounded-b-xl hover:bg-gray-800/50 transition-colors duration-200`}>
                <SyntaxHighlighter
                    style={nightOwl as any}
                    language={language}
                    PreTag="div"
                    className="rounded-lg"
                    wrapLines={false}
                    wrapLongLines={false}
                    showLineNumbers={false}
                    useInlineStyles={true}
                    customStyle={{ display: 'block', width: '100%', overflow: 'auto' }}
                >
                    {content}
                </SyntaxHighlighter>
            </div>
        </div>
    );
};