import { useState, useEffect } from "react"
import ReactMarkdown from 'react-markdown'
import { Prism as SyntaxHighlighter } from 'react-syntax-highlighter'
import { nightOwl } from 'react-syntax-highlighter/dist/cjs/styles/prism'
import { diagramRegistry } from './diagrams/registry'

interface MarkdownPaneProps {
    message: string;
}

export function MarkdownPane({ message }: MarkdownPaneProps) {
    const [markdownContent, setMarkdownContent] = useState<string>('')
    const [mermaidKey, setMermaidKey] = useState(0)
    const [showRawContent, setShowRawContent] = useState<Record<string, boolean>>({})

    useEffect(() => {
        setMarkdownContent(message);
        setMermaidKey(prev => prev + 1); // Force re-render of Mermaid diagrams
    }, [message])

    // Re-render diagrams when content changes
    useEffect(() => {
        diagramRegistry.renderAll();
    }, [markdownContent, mermaidKey]);

    const components = {
        code({ className, children }: any) {
            const match = /language-(\w+)/.exec(className || '')
            if (!match) return <code className={className}>{children}</code>;

            const language = match[1];
            const content = String(children).replace(/\n$/, '');

            // Check if it's a supported diagram type
            const diagramRenderer = diagramRegistry.get(language);
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

            const toggleButton = (
                <button
                    onClick={toggleView}
                    className="absolute right-2 top-2 text-xs text-gray-400 bg-gray-800 px-2 py-1 rounded hover:bg-gray-700"
                >
                    {isRawView ? 'Show Rendered' : 'Show Raw'}
                </button>
            );

            // Handle diagrams first
            if (diagramRenderer) {
                const DiagramComponent = diagramRenderer.Component;
                return isRawView ? (
                    <div className="relative  p-4 bg-gray-800 rounded-lg">
                        {toggleButton}
                        <pre>{content}</pre>
                    </div>
                ) : (
                    <div className="relative" key={mermaidKey}>
                        {toggleButton}
                        <DiagramComponent
                            content={content}
                            className="p-4 bg-gray-800 rounded-lg overflow-auto"
                        />
                    </div>
                );
            }

            // Only handle regular code blocks if not a diagram
            return isRawView ? (
                <div className="relative  p-4 bg-gray-800 rounded-lg">
                    {toggleButton}
                    <pre>{content}</pre>
                </div>
            ) : (
                <div className="relative ">
                    {toggleButton}
                    <SyntaxHighlighter
                        style={nightOwl as any}
                        language={match[1]}
                        PreTag="div"
                        className="rounded-lg"
                    >
                        {String(children).replace(/\n$/, '')}
                    </SyntaxHighlighter>
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
        table: ({ children }: any) => (
            <div className="overflow-x-auto ">
                <table className="min-w-full divide-y divide-gray-700">
                    {children}
                </table>
            </div>
        ),
        th: ({ children }: any) => (
            <th className="px-4 py-2 bg-gray-800">{children}</th>
        ),
        td: ({ children }: any) => (
            <td className="px-4 py-2 border-t border-gray-700">{children}</td>
        ),
    }

    return (
            <ReactMarkdown components={components}>
                {markdownContent}
            </ReactMarkdown>
    )
}