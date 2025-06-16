// AiStudioClient\src\components\MarkdownPane.tsx
import { useState, useEffect, useMemo, useCallback } from 'react';
import React from 'react';
import ReactMarkdown from 'react-markdown';
import { codeBlockRendererRegistry } from '@/components/diagrams/codeBlockRendererRegistry';
import remarkGfm from 'remark-gfm';
import { CodeBlock } from './MarkdownPane/CodeBlock';
import { cn } from '@/lib/utils';

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

    useEffect(() => {
        if (message !== markdownContent) {
            setMarkdownContent(message);
            setMermaidKey((prev) => prev + 1);
        }
    }, [message, markdownContent]);

    useEffect(() => {
        const isVS = localStorage.getItem('isVisualStudio') === 'true';
        setIsVisualStudio(isVS);
    }, []);

    useEffect(() => {
        const timer = setTimeout(() => {
            codeBlockRendererRegistry.renderAll();
        }, 50);
        return () => clearTimeout(timer);
    }, [markdownContent, mermaidKey]);

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

    const components = useMemo(() => ({
        code({ className, children }: any) {
            const match = /language-(\w+)/.exec(className || '');
            if (!match) return <code className={className}>{children}</code>;
            const language = match ? match[1] : 'txt';
            const content = String(children).replace(/\n$/, '');
            const diagramRenderer = codeBlockRendererRegistry.get(language);
            const blockId = `${language}-${content.slice(0, 20)}`;
            if (showRawContent[blockId] === undefined) {
                setShowRawContent((prev) => ({ ...prev, [blockId]: false }));
            }
            if (isCodeCollapsed[blockId] === undefined) {
                setIsCodeCollapsed((prev) => ({ ...prev, [blockId]: false }));
            }
            const isRawView = showRawContent[blockId];
            const isCollapsed = isCodeCollapsed[blockId];
            const handleToggleRaw = useCallback(() => {
                setShowRawContent((prev) => ({ ...prev, [blockId]: !prev[blockId] }));
                setMermaidKey((prev) => prev + 1);
            }, [blockId]);
            const handleToggleCollapse = useCallback(() => {
                const markdownPaneElement = document.querySelector('.markdown-pane')?.parentElement?.parentElement || document.documentElement;
                const currentScrollPosition = markdownPaneElement.scrollTop;
                markdownPaneElement.scrollTo({
                    top: Math.max(0, currentScrollPosition - 1),
                    behavior: 'auto'
                });
                setTimeout(() => {
                    setIsCodeCollapsed((prev) => ({ ...prev, [blockId]: !prev[blockId] }));
                    markdownPaneElement.scrollTo({
                        top: Math.max(0, currentScrollPosition + 1),
                        behavior: 'auto'
                    });
                }, 10);
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
                    variant={variant} // <-- PASS VARIANT DOWN
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
    }), [showRawContent, isCodeCollapsed, mermaidKey, isVisualStudio, variant]); // <-- ADD 'variant' to dependency array

    return (
        <div className={cn(
            "text-sm",
            variant === 'system' && "border-l-4 border-destructive p-2 bg-destructive/10 rounded prose-headings:text-destructive-foreground prose-p:text-destructive-foreground/90 prose-li:text-destructive-foreground/90 prose-a:text-red-400 prose-a:hover:text-red-300 prose-strong:text-destructive-foreground prose-blockquote:border-destructive/50 prose-blockquote:text-destructive-foreground/80",
        )}>
            <ReactMarkdown components={components} remarkPlugins={[remarkGfm]}>
                {markdownContent}
            </ReactMarkdown>
        </div>
    );
});