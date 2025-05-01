// AiStudioClient\src\components\MarkdownPane.tsx
import { useState, useEffect, useRef, useMemo, useCallback } from 'react';
import React from 'react';
import ReactMarkdown from 'react-markdown';

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
import { Prism as SyntaxHighlighter } from 'react-syntax-highlighter';
import { nightOwl } from 'react-syntax-highlighter/dist/cjs/styles/prism';
import { codeBlockRendererRegistry } from '@/components/diagrams/codeBlockRendererRegistry';
import remarkGfm from 'remark-gfm';
import { ExternalLink, Clipboard, ChevronDown, ChevronUp } from 'lucide-react';

interface MarkdownPaneProps {
    message: string;
}

export const MarkdownPane = React.memo(function MarkdownPane({ message }: MarkdownPaneProps) {
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
        // Add a style tag to make p tags inside li display as inline
        const styleTag = document.createElement('style');
        styleTag.textContent = `
      li > p {
        display: inline;
        margin: 0;
      }
      
      /* Add transition styles for code block collapsing/expanding */
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
                    <meta charset="UTF-8">
                    <meta name="viewport" content="width=device-width, initial-scale=1.0">
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

            // unformatted / no filetype
            if (!match) return <code className={className}>{children}</code>;

            const language = match ? match[1] : 'txt';

            const content = String(children).replace(/\n$/, '');


            const diagramRenderer = codeBlockRendererRegistry.get(language);
            const blockId = `${language}-${content.slice(0, 20)}`;


            if (showRawContent[blockId] === undefined) {
                setShowRawContent((prev) => ({ ...prev, [blockId]: false }));
            }

            // Initialize collapse state to true (collapsed) by default
            if (isCodeCollapsed[blockId] === undefined) {
                setIsCodeCollapsed((prev) => ({ ...prev, [blockId]: true }));
            }
            const isRawView = showRawContent[blockId];
            const isCollapsed = isCodeCollapsed[blockId];

            const toggleView = useCallback(() => {

                setShowRawContent((prev) => ({
                    ...prev,
                    [blockId]: !prev[blockId],
                }));


                setMermaidKey((prev) => prev + 1);
            }, [blockId]);

            const toggleCollapse = useCallback(() => {
                // Specifically and deliberately scroll up 1px so that when the collapser opens, we don't scroll to bottom <- DO NOT REMOVE THIS COMMENT

                // Scroll up 150px before collapsing/expanding
                const markdownPaneElement = document.querySelector('.markdown-pane').parentElement.parentElement || document.documentElement;
                const currentScrollPosition = markdownPaneElement.scrollTop;
                markdownPaneElement.scrollTo({
                    top: Math.max(0, currentScrollPosition - 1),
                    behavior: 'auto'
                });

                setTimeout(() => {
                    setIsCodeCollapsed((prev) => ({
                        ...prev,
                        [blockId]: !prev[blockId],
                    }));
                    markdownPaneElement.scrollTo({
                        top: Math.max(0, currentScrollPosition + 1),
                        behavior: 'auto'
                    });
                }, 10); // Small delay to allow scroll to start

            }, [blockId]);


            const isHtmlBlock = language === 'html' || language === 'htm';

            const showRenderedOrRawButton = (
                <button
                    onClick={toggleView}
                    className="text-small-gray-400 bg-gray-800 px-2 py-1 rounded hover:bg-gray-700 transition-colors"
                >
                    {isRawView ? 'Show Rendered' : 'Show Raw'}
                </button>
            );


            const launchButton = isHtmlBlock ? (
                <button
                    onClick={() => launchHtml(content)}
                    className="text-small-gray-400 bg-gray-800 px-2 py-1 rounded hover:bg-gray-700 transition-colors flex items-center gap-1"
                >
                    <ExternalLink className="h-3 w-3" />
                    Launch
                </button>
            ) : null;

            const createCodeHeader = useCallback((isFooter = false) => (
                <div
                    className={`MarkdownPane flex items-center justify-between px-4 py-2 ${isFooter ? 'rounded-b-xl border-t' : 'rounded-t-xl border-b'} text-sm`}
                    style={{
                        background: 'var(--markdownpane-codeheader-bg, var(--global-backgroundColor, #181c20))',
                        color: 'var(--markdownpane-codeheader-text, var(--global-textColor, #bfc7d5))',
                        borderColor: 'var(--markdownpane-codeheader-border, var(--global-borderColor, #283040))',
                        borderStyle: 'solid',
                        borderWidth: '1px 0 0 0', // Only top border for footer, bottom for header
                        ...(isFooter ? { borderTopWidth: '1px', borderBottomWidth: 0 } : { borderBottomWidth: '1px', borderTopWidth: 0 }),
                    }}
                >
                    <div className="flex items-center space-x-2">
                        <button
                            onClick={toggleCollapse}
                            className="transition-colors p-1"
                            style={{ color: 'var(--markdownpane-codeheader-accent, #4f8cff)' }}
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
                                    background: 'var(--markdownpane-codeheader-accent, var(--global-primaryColor, #4f8cff))',
                                    color: 'var(--markdownpane-codeheader-bg, var(--global-backgroundColor, #181c20))',
                                }}
                            >
                                Apply Diff
                            </button>
                        )}
                        {launchButton}
                        {language === 'theme' && (
                            <>
                                <button
                                    onClick={async () => {
                                        try {
                                            console.log('[Theme Debug] Starting theme application from code block');
                                            console.log('[Theme Debug] Theme content:', content);

                                            // Apply theme visually
                                            const parsedContent = JSON.parse(content);
                                            console.log('[Theme Debug] Parsed content:', parsedContent);
                                            
                                            // Apply theme visually
                                            window.applyLLMTheme(parsedContent);
                                            
                                            // Get theme name from the theme-name property if available, otherwise use timestamp
                                            const themeName = parsedContent['theme-name'] || `Theme from code block ${new Date().toLocaleTimeString()}`;
                                            
                                            // Add theme to zustand store
                                            if (window.addThemeToStore) {
                                                window.addThemeToStore({
                                                    name: themeName,
                                                    description: 'Theme applied from markdown code block',
                                                    themeJson: parsedContent
                                                });
                                            }
                                        } catch (e) {
                                            console.error('[Theme Debug] Error processing theme:', e);
                                        }
                                    }}
                                    className="px-2 py-1 rounded transition-colors mr-2"
                                    style={{
                                        background: 'var(--markdownpane-codeheader-accent, #4f8cff)',
                                        color: 'var(--markdownpane-codeheader-bg, #181c20)',
                                    }}
                                >
                                    Use Theme
                                </button>
                                <button
                                    onClick={async () => {
                                        try {
                                            console.log('[Theme Debug] Starting theme installation from code block');
                                            console.log('[Theme Debug] Theme content:', content);

                                            // Parse the theme content
                                            const parsedContent = JSON.parse(content);
                                            console.log('[Theme Debug] Parsed content:', parsedContent);
                                            
                                            // Get theme name from the theme-name property if available, otherwise use timestamp
                                            const themeName = parsedContent['theme-name'] || `Theme from code block ${new Date().toLocaleTimeString()}`;
                                            console.log('[Theme Debug] Using theme name:', themeName);
                                            
                                            // Install the theme to the library
                                            if (typeof window.createTheme === 'function') {
                                                await window.createTheme({
                                                    name: themeName,
                                                    description: 'Theme installed from markdown code block',
                                                    themeJson: parsedContent,
                                                    previewColors: Object.values(parsedContent)
                                                        .filter(value => typeof value === 'string' && value.startsWith('#'))
                                                        .slice(0, 5)
                                                });
                                            } else {
                                                console.error('createTheme function not available on window');
                                                // Try to import the function directly as a fallback
                                                try {
                                                    const { useThemeManagement } = await import('@/hooks/useThemeManagement');
                                                    const { createTheme } = useThemeManagement();
                                                    
                                                    if (typeof createTheme === 'function') {
                                                        await createTheme({
                                                            name: themeName,
                                                            description: 'Theme installed from markdown code block',
                                                            themeJson: parsedContent,
                                                            previewColors: Object.values(parsedContent)
                                                                .filter(value => typeof value === 'string' && value.startsWith('#'))
                                                                .slice(0, 5)
                                                        });
                                                    } else {
                                                        throw new Error('createTheme function not available');
                                                    }
                                                } catch (importError) {
                                                    console.error('Failed to import useThemeManagement:', importError);
                                                }
                                            }
                                        } catch (e) {
                                            console.error('[Theme Debug] Error installing theme:', e);
                                        }
                                    }}
                                    className="px-2 py-1 rounded transition-colors"
                                    style={{
                                        background: 'var(--markdownpane-codeheader-accent, #4f8cff)',
                                        color: 'var(--markdownpane-codeheader-bg, #181c20)',
                                    }}
                                >
                                    Install Theme
                                </button>
                            </>
                        )}
                        <button
                            onClick={() => navigator.clipboard.writeText(content)}
                            className="px-2 py-1 rounded transition-colors"
                            style={{
                                background: 'var(--markdownpane-codeheader-accent, #4f8cff)',
                                color: 'var(--markdownpane-codeheader-bg, #181c20)',
                            }}
                        >
                            Copy
                        </button>
                        {showRenderedOrRawButton}
                        <button
                            onClick={async () => {
                                // Suggest a filename based on language if possible
                                let ext = language ? `.${language}` : '.txt';
                                let suggestedFilename = `codeblock${ext}`;
                                try {
                                    // Dynamically import to avoid circular deps
                                    const { saveCodeBlockAsFile } = await import('@/services/api/apiClient');
                                    await saveCodeBlockAsFile({ content, suggestedFilename });
                                } catch (e) {
                                    console.error('Save As failed:', e);
                                }
                            }}
                            className="px-2 py-1 rounded transition-colors"
                            style={{
                                background: 'var(--markdownpane-codeheader-accent, #4f8cff)',
                                color: 'var(--markdownpane-codeheader-bg, #181c20)',
                            }}
                        >
                            Save As
                        </button>
                    </div>
                </div>
            ), [language, content, isVisualStudio, isCollapsed, toggleCollapse]);

            const codeHeader = createCodeHeader(false);

            if (diagramRenderer) {
                const DiagramComponent = diagramRenderer.Component;
                return isRawView ? (
                    <div className="rounded-xl overflow-hidden border border-gray-700 shadow-lg mb-4">
                        {codeHeader}
                        <div className={`code-content ${isCollapsed ? 'collapsed' : ''} p-4 bg-gray-800 rounded-b-lg`}>
                            <pre style={{ whiteSpace: 'break-spaces' }}>{content}</pre>
                        </div>
                    </div>
                ) : (
                    <div className="rounded-xl overflow-hidden border border-gray-700 shadow-lg mb-4" key={mermaidKey}>
                        {codeHeader}
                        <div className={`code-content ${isCollapsed ? 'collapsed' : ''} p-4 bg-gray-800 rounded-b-lg diagram-container`} data-type={diagramRenderer.type[0]} data-content={content}>
                            <DiagramComponent content={content} className="overflow-auto" />
                        </div>
                    </div>
                );
            }

            return isRawView ? (
                <div className="rounded-xl overflow-hidden border border-gray-700 shadow-lg mb-4">
                    {createCodeHeader(true)}
                    <div className={`code-content ${isCollapsed ? 'collapsed' : ''} p-4 bg-gray-800/40 backdrop-blur-sm shadow-inner border-t border-gray-700/30 rounded-b-xl`}>
                        <pre style={{ whiteSpace: 'break-spaces' }}>{content}</pre>
                    </div>
                    {codeHeader}
                </div>
            ) : (
                <div className="rounded-xl overflow-hidden border border-gray-700 shadow-lg mb-4">
                    {codeHeader}
                    <div className={`code-content ${isCollapsed ? 'collapsed' : ''} p-4 bg-gray-800/40 backdrop-blur-sm shadow-inner border-t border-gray-700/30 rounded-b-xl hover:bg-gray-800/50 transition-colors duration-200`}>
                        <SyntaxHighlighter
                            style={nightOwl as any}
                            language={match[1]}
                            PreTag="div"
                            className="rounded-lg"

                            wrapLines={false}
                            wrapLongLines={false}
                            showLineNumbers={false}
                            useInlineStyles={true}
                            customStyle={{ display: 'block', width: '100%', overflow: 'auto' }}
                        >
                            {String(children).replace(/\n$/, '')}
                        </SyntaxHighlighter>
                    </div>
                    <div className={`code-content ${isCollapsed ? 'collapsed' : ''}`}>
                        {createCodeHeader(true)}
                    </div>
                </div>
            );
        },
        pre: ({ children }: any) => <pre className="whitespace-pre-wrap break-words">{children}</pre>,
        p: ({ children }: any) => <p className="my-1 whitespace-pre-wrap break-words">{children}</p>,
        h1: ({ children }: any) => <h1 className="text-3xl font-bold my-6">{children}</h1>,
        h2: ({ children }: any) => <h2 className="text-2xl font-bold my-5">{children}</h2>,
        h3: ({ children }: any) => <h3 className="text-xl font-bold ">{children}</h3>,
        ul: ({ children }: any) => <ul className="list-disc list-inside  space-y-2">{children}</ul>,
        ol: ({ children }: any) => <ol className="list-decimal list-inside  space-y-2">{children}</ol>,
        li: ({ children }: any) => <li className="ml-4">{children}</li>,
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
    }), [showRawContent, isCodeCollapsed, mermaidKey, isVisualStudio]);

    return (

        <ReactMarkdown components={components} remarkPlugins={[remarkGfm]}>
            {markdownContent}
        </ReactMarkdown>

    );
});