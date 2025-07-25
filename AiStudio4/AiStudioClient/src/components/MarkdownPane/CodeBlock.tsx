﻿// MarkdownPane/CodeBlock.tsx
import React, { useCallback, useState } from 'react';
import { Prism as SyntaxHighlighter } from 'react-syntax-highlighter';
import { nightOwl } from 'react-syntax-highlighter/dist/cjs/styles/prism';
import { useStickToBottom } from 'use-stick-to-bottom';
import { CodeBlockHeader } from './CodeBlockHeader';
import { CodeBlockScrollButton } from './CodeBlockScrollButton';
import { MarkdownVariant } from '../MarkdownPane';
import { codeBlockRendererRegistry } from '../diagrams/codeBlockRendererRegistry';
import { useCodeBlockStore } from '@/stores/useCodeBlockStore';

export interface CodeBlockProps {
    blockId: string; // Now required
    language: string;
    content: string;
    diagramRenderer?: any;
    isVisualStudio: boolean;
    mermaidKey: number;
    launchHtml: (content: string) => void;
    variant: MarkdownVariant;
    fullMarkdown?: string;
}

export const CodeBlock = React.memo<CodeBlockProps>(({
    blockId,
    language,
    content,
    diagramRenderer,
    isVisualStudio,
    mermaidKey,
    launchHtml,
    variant,
    fullMarkdown,
}) => {
    // Optimized state subscription - only re-renders when this block's state changes
    const isCollapsed = useCodeBlockStore(state => state.isCollapsed(blockId));
    const isRawView = useCodeBlockStore(state => state.isRawView(blockId));
    const toggleCollapse = useCodeBlockStore(state => state.toggleCollapse);
    const toggleRawView = useCodeBlockStore(state => state.toggleRawView);

    // Local hover state for the code block
    const [isCodeBlockHovered, setIsCodeBlockHovered] = useState(false);

    // Initialize stick-to-bottom for each code block
    const stickToBottomInstance = useStickToBottom({
        initial: 'smooth',
        resize: 'smooth'
    });

    const handleToggleCollapse = useCallback(() => {
        // Optional: Handle scroll position preservation
        const scrollContainer = document.querySelector('.markdown-pane')?.parentElement?.parentElement;
        if (scrollContainer && scrollContainer !== document.documentElement) {
            const currentScrollPosition = scrollContainer.scrollTop;
            toggleCollapse(blockId);
            // You could restore scroll position here if needed
        } else {
            toggleCollapse(blockId);
        }
    }, [blockId, toggleCollapse]);

    const handleToggleRaw = useCallback(() => {
        toggleRawView(blockId);
    }, [blockId, toggleRawView]);
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
            onToggleRaw={handleToggleRaw}
            onToggleCollapse={handleToggleCollapse}
            launchHtml={isHtmlBlock ? () => launchHtml(content) : undefined}
            variant={variant} // <-- PASS PROP
        />
    );

    // Diagram block
    if (DiagramComponent) {
        return isRawView ? (
            <div className=" overflow-hidden my-2" onMouseEnter={() => setIsCodeBlockHovered(true)} onMouseLeave={() => setIsCodeBlockHovered(false)}>
                {header}
                <div className="relative">
                    <div 
                        ref={stickToBottomInstance.scrollRef} 
                        className="max-h-[500px] overflow-auto"
                    >
                        <div ref={stickToBottomInstance.contentRef}>
                            <div className={`code-content ${isCollapsed ? 'collapsed' : ''} p-1 rounded-b-lg`}>
                                <pre style={{ whiteSpace: 'break-spaces' }}>{content}</pre>
                            </div>
                        </div>
                    </div>
                    <CodeBlockScrollButton stickToBottomInstance={stickToBottomInstance} isHovered={isCodeBlockHovered} />
                </div>
            </div>
        ) : (
            <div className=" overflow-hidden my-2" key={mermaidKey} onMouseEnter={() => setIsCodeBlockHovered(true)} onMouseLeave={() => setIsCodeBlockHovered(false)}>
                    {header}
                    <div className="relative">
                        <div 
                            ref={stickToBottomInstance.scrollRef} 
                            className="max-h-[500px] overflow-auto"
                        >
                            <div ref={stickToBottomInstance.contentRef}>
                                <div className={`code-content ${isCollapsed ? 'collapsed' : ''} rounded-b-lg diagram-container`} data-type={diagramRenderer.type[0]} data-content={content}>
                                    <DiagramComponent content={content} className="overflow-auto" />
                                </div>
                            </div>
                        </div>
                        <CodeBlockScrollButton stickToBottomInstance={stickToBottomInstance} isHovered={isCodeBlockHovered} />
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
        <div className=" overflow-hidden my-2" onMouseEnter={() => setIsCodeBlockHovered(true)} onMouseLeave={() => setIsCodeBlockHovered(false)}>
            {header}
            <div className="relative">
                <div 
                    ref={stickToBottomInstance.scrollRef} 
                    className="max-h-[500px] overflow-auto"
                >
                    <div ref={stickToBottomInstance.contentRef}>
                        <div className={`code-content ${isCollapsed ? 'collapsed' : ''} p-1 backdrop-blur-sm shadow-inner border-t border-gray-700/30 rounded-b-xl`}>
                            <pre style={{ whiteSpace: 'break-spaces' }}>{content}</pre>
                        </div>
                    </div>
                </div>
                <CodeBlockScrollButton stickToBottomInstance={stickToBottomInstance} />
            </div>
        </div>
    ) : (
        <div className=" overflow-hidden my-2" onMouseEnter={() => setIsCodeBlockHovered(true)} onMouseLeave={() => setIsCodeBlockHovered(false)}>
                {header}
                <div className="relative">
                    <div 
                        ref={stickToBottomInstance.scrollRef} 
                        className="max-h-[500px] overflow-auto"
                    >
                        <div ref={stickToBottomInstance.contentRef}>
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
                    <CodeBlockScrollButton stickToBottomInstance={stickToBottomInstance} isHovered={isCodeBlockHovered} />
                </div>
        </div>
    );
});