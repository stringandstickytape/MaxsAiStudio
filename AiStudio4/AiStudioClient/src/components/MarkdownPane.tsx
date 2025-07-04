// AiStudioClient\src\components\MarkdownPane.tsx
import { useState, useEffect, useMemo, useCallback, useRef } from 'react';
import React from 'react';
import ReactMarkdown from 'react-markdown';
import { codeBlockRendererRegistry } from '@/components/diagrams/codeBlockRendererRegistry';
import remarkGfm from 'remark-gfm';
import { CodeBlock } from './MarkdownPane/CodeBlock';
import { cn } from '@/lib/utils';
import matter from 'gray-matter';
import { MarpRenderer } from './renderers/marp-renderer';

// Themeable properties for MarkdownPane code headers
export const themeableProps = {
    codeHeaderBackground: {
        cssVar: '--markdownpane-codeheader-bg',
        description: 'Background color for code block header',
        default: '#181c20',
    },
    codeHeaderText: {
        cssVar: '--markdownpane-codeheader-text',
        description: 'Text color for code block header',
        default: '#bfc7d5',
    },
    codeHeaderBorder: {
        cssVar: '--markdownpane-codeheader-border',
        description: 'Border color for code block header',
        default: '#283040',
    },
    codeHeaderAccent: {
        cssVar: '--markdownpane-codeheader-accent',
        description: 'Accent color for code block header buttons/highlights',
        default: '#4f8cff',
    },
    style: {
        description: 'Arbitrary CSS style for MarkdownPane root',
        default: {},
    },
};

// NEW: Define the variant type
export type MarkdownVariant = 'default' | 'system';

interface MarkdownPaneProps {
    message: string;
    variant?: MarkdownVariant; // <-- ADD PROP
}

export const MarkdownPane = React.memo(function MarkdownPane({ 
    message, 
    variant = 'default' // <-- ADD PROP
}: MarkdownPaneProps) {
    const [markdownContent, setMarkdownContent] = useState<string>('');
    const [mermaidKey, setMermaidKey] = useState(0);
    const [showRawContent, setShowRawContent] = useState<Record<string, boolean>>({});
    const [isCodeCollapsed, setIsCodeCollapsed] = useState<Record<string, boolean>>({});
    const [isVisualStudio, setIsVisualStudio] = useState(false);
    
    // Track completed portions and only render changes
    const [completedSegments, setCompletedSegments] = useState<Array<{ content: string; type: 'completed' | 'incomplete' }>>([]);
    const lastProcessedLength = useRef<number>(0);
    
    // Parse frontmatter to check for Marp
    const parsedContent = useMemo(() => {
        try {
            const parsed = matter(message);
            const isMarp = parsed.data?.marp === true;
            return {
                content: parsed.content,
                data: parsed.data,
                isMarp
            };
        } catch (error) {
            // If parsing fails, treat as regular markdown
            return {
                content: message,
                data: {},
                isMarp: false
            };
        }
    }, [message]);
    
    // Parse the markdown into segments (completed code blocks and other content)
    const parseMarkdownSegments = useCallback((markdown: string) => {
        const segments: Array<{ content: string; type: 'completed' | 'incomplete' }> = [];
        const codeBlockRegex = /```[\s\S]*?```/g;
        let lastIndex = 0;
        let match;
        
        while ((match = codeBlockRegex.exec(markdown)) !== null) {
            // Add content before the code block
            if (match.index > lastIndex) {
                const beforeContent = markdown.slice(lastIndex, match.index);
                if (beforeContent.trim()) {
                    segments.push({ content: beforeContent, type: 'completed' });
                }
            }
            
            // Add the completed code block
            segments.push({ content: match[0], type: 'completed' });
            lastIndex = match.index + match[0].length;
        }
        
        // Add remaining content (might be incomplete)
        if (lastIndex < markdown.length) {
            const remainingContent = markdown.slice(lastIndex);
            if (remainingContent.trim()) {
                // Check if there's an incomplete code block at the end
                const hasIncompleteCodeBlock = /```[^`]*$/.test(remainingContent);
                segments.push({ 
                    content: remainingContent, 
                    type: hasIncompleteCodeBlock ? 'incomplete' : 'completed' 
                });
            }
        }
        
        return segments;
    }, []);

    useEffect(() => {
        const contentToProcess = parsedContent.content;
        if (contentToProcess !== markdownContent) {
            // Only process if content has actually grown (streaming case)
            if (contentToProcess.length > lastProcessedLength.current && contentToProcess.startsWith(markdownContent)) {
                // This is a streaming update - only update the segments
                const newSegments = parseMarkdownSegments(contentToProcess);
                setCompletedSegments(newSegments);
                lastProcessedLength.current = contentToProcess.length;
            } else {
                // This is a completely new message - reset everything
                const newSegments = parseMarkdownSegments(contentToProcess);
                setCompletedSegments(newSegments);
                lastProcessedLength.current = contentToProcess.length;
                setMermaidKey((prev) => prev + 1);
            }
            setMarkdownContent(contentToProcess);
        }
    }, [parsedContent.content, markdownContent, parseMarkdownSegments]);

    useEffect(() => {
        const isVS = localStorage.getItem('isVisualStudio') === 'true';
        setIsVisualStudio(isVS);
    }, []);

    // Removed renderAll call since React components handle their own rendering

    // Add CSS to handle li > p display
    useEffect(() => {
        const styleTag = document.createElement('style');
        styleTag.textContent = `
      li > p {
        display: inline;
        margin: 0;
      }
      .code-content {
        max-height: 50000px;
        transition: max-height 0.3s ease-in-out, opacity 0.3s ease-in-out;
        overflow: hidden;
        opacity: 1;
      }
      .code-content.collapsed {
        max-height: 0;
        opacity: 0;
        padding-top: 0 !important;
        padding-bottom: 0 !important;
      }
    `;
        document.head.appendChild(styleTag);
        return () => {
            document.head.removeChild(styleTag);
        };
    }, []);

    const launchHtml = (content: string) => {
        const newWindow = window.open('', '_blank');
        if (newWindow) {
            newWindow.document.write(`
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset=\"UTF-8\">
                    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">
                    <title>HTML Preview</title>
                    <style>
                        body {
                            font-family: Inter, system-ui, Avenir, Helvetica, Arial, sans-serif;
                            line-height: 1.5;
                            padding: 20px;
                        }
                    </style>
                </head>
                <body>
                    ${content}
                </body>
                </html>
            `);
            newWindow.document.close();
        }
    };

    let codeBlockIndex = 0;

    const components = useMemo(() => ({
        code({ className, children, ...props }: any) {
            // Inline code typically has no className or a simple one without 'language-'
            // Code blocks always get a className (even if empty) from ReactMarkdown
            const isCodeBlock = className !== undefined || String(children).includes('\n');
            
            // If this is inline code, render it as a simple <code> element
            if (!isCodeBlock) {
                return <code className={className}>{children}</code>;
            }
            
            const match = /language-(\w+)/.exec(className || '');
            const language = match ? match[1] : 'txt';
            const content = String(children).replace(/\n$/, '');
            const diagramRenderer = codeBlockRendererRegistry.get(language);
            const blockId = `code-block-${codeBlockIndex++}`;
            const isRawView = showRawContent[blockId] ?? false;
            const isCollapsed = isCodeCollapsed[blockId] ?? true;
            const handleToggleRaw = useCallback(() => {
                setShowRawContent((prev) => ({ ...prev, [blockId]: !prev[blockId] }));
                setMermaidKey((prev) => prev + 1);
            }, [blockId]);
            const handleToggleCollapse = useCallback(() => {
                // Find the scrollable container - try multiple selectors
                let scrollContainer = document.querySelector('.markdown-pane')?.parentElement?.parentElement;
                
                // If that doesn't work, try finding a container with scroll
                if (!scrollContainer || scrollContainer === document.documentElement) {
                    const containers = [
                        document.querySelector('[data-testid="chat-container"]'),
                        document.querySelector('.chat-container'),
                        document.querySelector('.overflow-auto'),
                        document.querySelector('.scroll-container')
                    ].filter(Boolean);
                    
                    scrollContainer = containers.find(container => 
                        container && container.scrollHeight > container.clientHeight
                    ) || null;
                }
                
                // Only proceed if we found a valid scroll container (not document.documentElement)
                if (scrollContainer && scrollContainer !== document.documentElement) {
                    const currentScrollPosition = scrollContainer.scrollTop;
                    setIsCodeCollapsed((prev) => ({ ...prev, [blockId]: !(prev[blockId] ?? true) }));
                } else {
                    // Just toggle without scroll manipulation if we can't find proper container
                    setIsCodeCollapsed((prev) => ({ ...prev, [blockId]: !(prev[blockId] ?? true) }));
                }
            }, [blockId]);
            return (
                <CodeBlock
                    language={language}
                    content={content}
                    diagramRenderer={diagramRenderer}
                    isVisualStudio={isVisualStudio}
                    blockId={blockId}
                    isRawView={isRawView}
                    isCollapsed={isCollapsed}
                    mermaidKey={mermaidKey}
                    onToggleRaw={handleToggleRaw}
                    onToggleCollapse={handleToggleCollapse}
                    launchHtml={launchHtml}
                    variant={variant}
                    fullMarkdown={markdownContent}
                />
            );
        },
        hr: () => <hr className="my-2"/>,
        pre: ({ children }: any) => <pre className="leading-snug whitespace-pre-wrap break-words" style={{
            backgroundColor: 'var(--global-background-color, #000000)',
            color: 'var(--global-text-color, var(--markdownpane-codeheader-bg, #181c20))',
            borderRadius: 'var(--global-border-radius, inherit)',
            lineBreak: 'anywhere'
        }}>{children}</pre>,
        p: ({ children }: any) => <p className="leading-snug mb-1 whitespace-pre-wrap break-words">{children}</p>,
        h1: ({ children }: any) => <h1 className="mb-1 leading-relaxed text-3xl font-bold my-6">{children}</h1>,
        h2: ({ children }: any) => <h2 className="mb-1 leading-normal text-2xl font-bold my-5">{children}</h2>,
        h3: ({ children }: any) => <h3 className="mb-1 leading-normal text-xl font-bold ">{children}</h3>,
        ul: ({ children }: any) => <ul className="mb-2 leading-snug list-disc list-inside  space-y-1">{children}</ul>,
        ol: ({ children }: any) => <ol className="mb-2 leading-snug list-decimal list-inside  space-y-1">{children}</ol>,
        li: ({ children }: any) => <li className="mb-2 leading-snug ml-4">{children}</li>,
        blockquote: ({ children }: any) => (
            <blockquote className="border-l-4 border-gray-600 pl-4  italic">{children}</blockquote>
        ),
        a: ({ href, children }: any) => (
            <a href={href} className="text-blue-400 hover:text-blue-300 underline">
                {children}
            </a>
        ),
        thead: ({ children }: any) => <thead className="bg-gray-800">{children}</thead>,
        tbody: ({ children }: any) => <tbody className="divide-y divide-gray-700">{children}</tbody>,
        tr: ({ children }: any) => <tr>{children}</tr>,
        th: ({ children }: any) => <th className="px-4 py-2 text-left font-medium">{children}</th>,
        td: ({ children }: any) => <td className="px-4 py-2 border-t border-gray-700">{children}</td>,
    }), [showRawContent, isCodeCollapsed, mermaidKey, isVisualStudio, variant]); // <-- Simplified dependencies

    // Track completed segments separately for better memoization
    const completedSegmentsOnly = useMemo(() => {
        return completedSegments.filter(segment => segment.type === 'completed');
    }, [completedSegments]);

    // Memoized completed segments to prevent re-rendering
    const memoizedCompletedSegments = useMemo(() => {
        return completedSegmentsOnly.map((segment, index) => (
            <div key={`completed-${index}-${segment.content.slice(0, 50)}`}>
                <ReactMarkdown components={components} remarkPlugins={[remarkGfm]}>
                    {segment.content}
                </ReactMarkdown>
            </div>
        ));
    }, [completedSegmentsOnly, components]);

    // Current incomplete segment that should re-render
    const incompleteSegment = completedSegments.find(segment => segment.type === 'incomplete');

    // If this is a Marp presentation, render with MarpRenderer
    if (parsedContent.isMarp) {
        return (
            <div className="marp-presentation-container">
                <MarpRenderer
                    markdown={parsedContent.content}
                    frontmatter={parsedContent.data}
                />
            </div>
        );
    }

    // Otherwise, render as regular markdown
    return (
        <div className={cn(
            "text-sm",
            variant === 'system' && "border-l-4 border-destructive p-2 bg-destructive/10 rounded prose-headings:text-destructive-foreground prose-p:text-destructive-foreground/90 prose-li:text-destructive-foreground/90 prose-a:text-red-400 prose-a:hover:text-red-300 prose-strong:text-destructive-foreground prose-blockquote:border-destructive/50 prose-blockquote:text-destructive-foreground/80",
        )}>
            {/* Render memoized completed segments */}
            {memoizedCompletedSegments}
            
            {/* Render incomplete segment that can change */}
            {incompleteSegment && (
                <ReactMarkdown components={components} remarkPlugins={[remarkGfm]}>
                    {incompleteSegment.content}
                </ReactMarkdown>
            )}
        </div>
    );
});