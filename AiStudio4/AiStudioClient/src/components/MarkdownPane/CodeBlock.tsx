// MarkdownPane/CodeBlock.tsx
import React, { useCallback } from 'react';
import { Prism as SyntaxHighlighter } from 'react-syntax-highlighter';
import { nightOwl } from 'react-syntax-highlighter/dist/cjs/styles/prism';
import { CodeBlockHeader } from './CodeBlockHeader';
import { MarkdownVariant } from '../MarkdownPane';
import { codeBlockRendererRegistry } from '../diagrams/codeBlockRendererRegistry';

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
    variant: MarkdownVariant;
    fullMarkdown?: string;
}

export const CodeBlock = React.memo<CodeBlockProps>(({
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
    variant,
    fullMarkdown,
}) => {
    const isHtmlBlock = language === 'html' || language === 'htm';
    const DiagramComponent = diagramRenderer ? diagramRenderer.Component : null;

    // Check if this code block should be rendered (only render if it's complete)
    const shouldRender = !fullMarkdown || codeBlockRendererRegistry.shouldRenderCodeBlock(fullMarkdown, content);
    
    // If we shouldn't render and this is a diagram block, show a placeholder
    if (!shouldRender && DiagramComponent) {
        return (
            <div className="overflow-hidden my-2">
                <div style={{ maxHeight: '500px', overflow: 'auto' }}>
                <div className="p-4 rounded-lg border-2 border-dashed border-gray-500 text-gray-500 text-center">
                    <span>Code block is being written...</span>
                    </div>
                </div>
            </div>
        );
    }

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
            variant={variant} // <-- PASS PROP
        />
    );

    // Diagram block
    if (DiagramComponent) {
        return isRawView ? (
            <div className=" overflow-hidden my-2">
                {header}
                <div style={{ maxHeight: '500px', overflow: 'auto' }}>
                <div className={`code-content ${isCollapsed ? 'collapsed' : ''} p-1 rounded-b-lg`}>
                    <pre style={{ whiteSpace: 'break-spaces' }}>{content}</pre>
                    </div>
                </div>
            </div>
        ) : (
            <div className=" overflow-hidden my-2" key={mermaidKey}>
                    {header}
                    <div style={{ maxHeight: '500px', overflow: 'auto' }}>
                <div className={`code-content ${isCollapsed ? 'collapsed' : ''} p-1 rounded-b-lg diagram-container`} data-type={diagramRenderer.type[0]} data-content={content}>
                    <DiagramComponent content={content} className="overflow-auto" />
                        </div>
                </div>
            </div>
        );
    }

    // If we shouldn't render a regular code block, show placeholder
    if (!shouldRender) {
        return (
            <div className="overflow-hidden my-2">
                <div className="p-4 rounded-lg border-2 border-dashed border-gray-500 text-gray-500 text-center">
                    <span>Code block is being written...</span>
                </div>
            </div>
        );
    }

    // Regular code block
    return isRawView ? (
        <div className=" overflow-hidden my-2">
            {header}
            <div style={{ maxHeight: '500px', overflow: 'auto' }}>
            <div className={`code-content ${isCollapsed ? 'collapsed' : ''} p-1 backdrop-blur-sm shadow-inner border-t border-gray-700/30 rounded-b-xl`}>
                <pre style={{ whiteSpace: 'break-spaces' }}>{content}</pre>
                </div>
            </div>
        </div>
    ) : (
        <div className=" overflow-hidden my-2">
                {header}
                <div style={{ maxHeight: '500px', overflow: 'auto' }}>
            <div className={`code-content ${isCollapsed ? 'collapsed' : ''} p-1 backdrop-blur-sm shadow-inner border-t border-gray-700/30 rounded-b-xl hover:bg-gray-800/50 transition-colors duration-200`}>
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
        </div>
    );
});