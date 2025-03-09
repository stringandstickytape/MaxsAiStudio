// src/markdown-pane.tsx
import { useState, useEffect } from "react"
import ReactMarkdown from 'react-markdown'
import { Prism as SyntaxHighlighter } from 'react-syntax-highlighter'
import { nightOwl } from 'react-syntax-highlighter/dist/cjs/styles/prism'
import { codeBlockRendererRegistry } from '@/components/diagrams/codeBlockRendererRegistry'
import remarkGfm from 'remark-gfm'
import { ExternalLink } from 'lucide-react'

interface MarkdownPaneProps {
    message: string;
}


export function MarkdownPane({ message }: MarkdownPaneProps) {
    const [markdownContent, setMarkdownContent] = useState<string>('')
    const [mermaidKey, setMermaidKey] = useState(0)
    const [showRawContent, setShowRawContent] = useState<Record<string, boolean>>({})
    const [isVisualStudio, setIsVisualStudio] = useState(false);

    useEffect(() => {
        setMarkdownContent(message);
        setMermaidKey(prev => prev + 1); // Force re-render of Mermaid diagrams
    }, [message])

    useEffect(() => {
        const isVS = localStorage.getItem('isVisualStudio') === 'true';
        setIsVisualStudio(isVS);
    }, []);


    // Re-render diagrams when content changes
    useEffect(() => {
        codeBlockRendererRegistry.renderAll();
    }, [markdownContent, mermaidKey]);

    // Function to launch HTML content in a new window
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

    const components = {
        code({ className, children }: any) {
            const match = /language-(\w+)/.exec(className || '')
            if (!match) return <code className={className}>{children}</code>;

            const language = match[1];
            const content = String(children).replace(/\n$/, '');

            // Check if it's a supported diagram type
            const diagramRenderer = codeBlockRendererRegistry.get(language);
            const blockId = `${language}-${content.slice(0, 20)}`;
            const isRawView = showRawContent[blockId];

            const toggleView = () => {
                setShowRawContent(prev => ({
                    ...prev,
                    [blockId]: !prev[blockId]
                }));
                // Force re-render of diagrams when toggling back to rendered view
                if (showRawContent[blockId]) {
                    setMermaidKey(prev => prev + 1);
                }
            };

            // Determine if this is an HTML block that can be launched
            const isHtmlBlock = language === 'html' || language === 'htm';

            const showRenderedOrRawButton = (
                <button
                    onClick={toggleView}
                    className="text-xs text-gray-400 bg-gray-800 px-2 py-1 rounded hover:bg-gray-700 transition-colors"
                >
                    {isRawView ? 'Show Rendered' : 'Show Raw'}
                </button>
            );

            // Launch button for HTML blocks
            const launchButton = isHtmlBlock ? (
                <button
                    onClick={() => launchHtml(content)}
                    className="text-xs text-gray-400 bg-gray-800 px-2 py-1 rounded hover:bg-gray-700 transition-colors flex items-center gap-1"
                >
                    <ExternalLink className="h-3 w-3" />
                    Launch
                </button>
            ) : null;

            const codeHeader = (
                <div className="flex items-center justify-between bg-gray-900 px-4 py-2 rounded-t-xl border-b border-gray-700 text-sm text-gray-400">
                    <div className="font-medium">{language}</div>
                    <div className="flex space-x-2">
                        {isVisualStudio && (
                            <button
                                onClick={() => window.chrome.webview.postMessage({ type: 'applyNewDiff', content: content.trim() })}
                                className="text-xs text-gray-400 bg-gray-800 px-2 py-1 rounded hover:bg-gray-700 transition-colors"
                            >
                                Apply Diff
                            </button>
                        )}
                        {launchButton}
                        {showRenderedOrRawButton}
                    </div>
                </div>
            );

            // Handle diagrams first
            if (diagramRenderer) {
                const DiagramComponent = diagramRenderer.Component;
                return isRawView ? (
                    <div className="relative rounded-xl overflow-hidden border border-gray-700 shadow-lg mb-4">
                        {codeHeader}
                        <div className="p-4 bg-gray-800 rounded-b-lg">
                            <pre>{content}</pre>
                        </div>
                    </div>
                ) : (
                    <div className="relative rounded-xl overflow-hidden border border-gray-700 shadow-lg mb-4" key={mermaidKey}>
                        {codeHeader}
                        <div className="p-4 bg-gray-800 rounded-b-lg">
                            <DiagramComponent
                                content={content}
                                className="overflow-auto"
                            />
                        </div>
                    </div>
                );
            }

            // Only handle regular code blocks if not a diagram
            return isRawView ? (
                <div className="relative rounded-xl overflow-hidden border border-gray-700 shadow-lg mb-4">
                    {codeHeader}
                    <div className="p-4 bg-gray-800/40 backdrop-blur-sm shadow-inner border-t border-gray-700/30 rounded-b-xl">
                        <pre>{content}</pre>
                    </div>
                </div>
            ) : (
                <div className="relative rounded-xl overflow-hidden border border-gray-700 shadow-lg mb-4">
                    {codeHeader}
                    <div className="p-4 bg-gray-800/40 backdrop-blur-sm shadow-inner border-t border-gray-700/30 rounded-b-xl hover:bg-gray-800/50 transition-colors duration-200">
                        <SyntaxHighlighter
                            style={nightOwl as any}
                            language={match[1]}
                            PreTag="div"
                            className="rounded-lg"
                        >
                            {String(children).replace(/\n$/, '')}
                        </SyntaxHighlighter>
                    </div>
                </div>
            )
        },
        // Add styling for other markdown elements
        p: ({ children }: any) => (
            <p className="my-4 leading-relaxed">{children}</p>
        ),
        h1: ({ children }: any) => (
            <h1 className="text-3xl font-bold my-6">{children}</h1>
        ),
        h2: ({ children }: any) => (
            <h2 className="text-2xl font-bold my-5">{children}</h2>
        ),
        h3: ({ children }: any) => (
            <h3 className="text-xl font-bold ">{children}</h3>
        ),
        ul: ({ children }: any) => (
            <ul className="list-disc list-inside  space-y-2">{children}</ul>
        ),
        ol: ({ children }: any) => (
            <ol className="list-decimal list-inside  space-y-2">{children}</ol>
        ),
        li: ({ children }: any) => (
            <li className="ml-4">{children}</li>
        ),
        blockquote: ({ children }: any) => (
            <blockquote className="border-l-4 border-gray-600 pl-4  italic">
                {children}
            </blockquote>
        ),
        a: ({ href, children }: any) => (
            <a href={href} className="text-blue-400 hover:text-blue-300 underline">
                {children}
            </a>
        ),
        thead: ({ children }: any) => (
            <thead className="bg-gray-800">{children}</thead>
        ),
        tbody: ({ children }: any) => (
            <tbody className="divide-y divide-gray-700">{children}</tbody>
        ),
        tr: ({ children }: any) => (
            <tr>{children}</tr>
        ),
        th: ({ children }: any) => (
            <th className="px-4 py-2 text-left font-medium">{children}</th>
        ),
        td: ({ children }: any) => (
            <td className="px-4 py-2 border-t border-gray-700">{children}</td>
        ),
    }

    return (
        <ReactMarkdown components={components} remarkPlugins={[remarkGfm]}>
            {markdownContent}
        </ReactMarkdown>
    )
}