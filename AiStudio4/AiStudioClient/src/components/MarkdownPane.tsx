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
import { useCodeBlockStore } from '@/stores/useCodeBlockStore';

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
    messageId?: string; // Message ID for state management
    variant?: MarkdownVariant; // <-- ADD PROP
}

export const MarkdownPane = React.memo(function MarkdownPane({ 
    message, 
    messageId,
    variant = 'default' // <-- ADD PROP
}: MarkdownPaneProps) {
    const [markdownContent, setMarkdownContent] = useState<string>('');
    const [mermaidKey, setMermaidKey] = useState(0);
    const [isVisualStudio, setIsVisualStudio] = useState(false);
    
    // Zustand store for code block state management
    const clearBlockStates = useCodeBlockStore(state => state.clearBlockStates);
    
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
    
    // Helper function to find the next code block start with backtick count
    const findNextCodeBlockStart = useCallback((markdown: string, startIndex: number) => {
        let index = startIndex;
        
        while (index < markdown.length) {
            const backtickMatch = markdown.slice(index).match(/^```+/m);
            if (!backtickMatch) {
                return { index: -1, backtickCount: 0 };
            }
            
            const matchIndex = index + markdown.slice(index).indexOf(backtickMatch[0]);
            
            // Check if this is at the start of a line
            if (matchIndex === 0 || markdown[matchIndex - 1] === '\n') {
                return { 
                    index: matchIndex, 
                    backtickCount: backtickMatch[0].length 
                };
            }
            
            index = matchIndex + 1;
        }
        
        return { index: -1, backtickCount: 0 };
    }, []);
    
    // Helper function to find the matching closing fence
    const findMatchingCodeBlockEnd = useCallback((markdown: string, startIndex: number, backtickCount: number) => {
        const openingFence = markdown.slice(startIndex);
        const firstNewlineIndex = openingFence.indexOf('\n');
        
        if (firstNewlineIndex === -1) {
            return -1; // No content after opening fence
        }
        
        let searchIndex = startIndex + firstNewlineIndex + 1;
        
        while (searchIndex < markdown.length) {
            const remainingContent = markdown.slice(searchIndex);
            const lineStart = remainingContent.match(/^```+/m);
            
            if (!lineStart) {
                return -1; // No closing fence found
            }
            
            const lineStartIndex = searchIndex + remainingContent.indexOf(lineStart[0]);
            
            // Check if this is at the start of a line and has enough backticks
            if ((lineStartIndex === 0 || markdown[lineStartIndex - 1] === '\n') && 
                lineStart[0].length >= backtickCount) {
                
                // Find the end of this line
                const lineEndIndex = markdown.indexOf('\n', lineStartIndex);
                return lineEndIndex === -1 ? markdown.length : lineEndIndex + 1;
            }
            
            searchIndex = lineStartIndex + 1;
        }
        
        return -1; // No matching closing fence found
    }, []);
    
    // Parse the markdown into segments (completed code blocks and other content)
    const parseMarkdownSegments = useCallback((markdown: string) => {
        const segments: Array<{ content: string; type: 'completed' | 'incomplete' }> = [];
        let currentIndex = 0;
        
        while (currentIndex < markdown.length) {
            // Find the next code block start
            const codeBlockStart = findNextCodeBlockStart(markdown, currentIndex);
            
            if (codeBlockStart.index === -1) {
                // No more code blocks, add remaining content
                const remainingContent = markdown.slice(currentIndex);
                if (remainingContent.trim()) {
                    segments.push({ content: remainingContent, type: 'completed' });
                }
                break;
            }
            
            // Add content before the code block
            if (codeBlockStart.index > currentIndex) {
                const beforeContent = markdown.slice(currentIndex, codeBlockStart.index);
                if (beforeContent.trim()) {
                    segments.push({ content: beforeContent, type: 'completed' });
                }
            }
            
            // Find the matching closing fence
            const codeBlockEnd = findMatchingCodeBlockEnd(markdown, codeBlockStart.index, codeBlockStart.backtickCount);
            
            if (codeBlockEnd === -1) {
                // Incomplete code block at the end
                const incompleteContent = markdown.slice(codeBlockStart.index);
                if (incompleteContent.trim()) {
                    segments.push({ content: incompleteContent, type: 'incomplete' });
                }
                break;
            }
            
            // Add the completed code block
            const codeBlockContent = markdown.slice(codeBlockStart.index, codeBlockEnd);
            segments.push({ content: codeBlockContent, type: 'completed' });
            currentIndex = codeBlockEnd;
        }
        
        return segments;
    }, [findNextCodeBlockStart, findMatchingCodeBlockEnd]);

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

    // Clear code block states when message ID changes
    useEffect(() => {
        if (messageId) {
            clearBlockStates();
        }
    }, [messageId, clearBlockStates]);

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
            
            // Create stable blockId that includes message ID and index
            const blockId = `${messageId || 'unknown'}-code-block-${codeBlockIndex++}`;
            return (
                <CodeBlock
                    key={blockId}
                    blockId={blockId}
                    language={language}
                    content={content}
                    diagramRenderer={diagramRenderer}
                    isVisualStudio={isVisualStudio}
                    mermaidKey={mermaidKey}
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
    }), [mermaidKey, isVisualStudio, variant, messageId]); // Removed state dependencies

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