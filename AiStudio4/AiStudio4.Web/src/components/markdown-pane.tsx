import { useState, useEffect } from "react"
import ReactMarkdown from 'react-markdown'
import { Prism as SyntaxHighlighter } from 'react-syntax-highlighter'
import { nightOwl } from 'react-syntax-highlighter/dist/cjs/styles/prism'
import mermaid from 'mermaid'

// Initialize mermaid with dark theme configuration
mermaid.initialize({
    startOnLoad: true,
    theme: 'dark',
    securityLevel: 'loose',
    darkMode: true,
    themeVariables: {
        fontFamily: 'Inter, system-ui, Avenir, Helvetica, Arial, sans-serif',
        primaryColor: '#3b82f6',
        primaryTextColor: '#e0e0e0',
        primaryBorderColor: '#374151',
        lineColor: '#4b5563',
        secondaryColor: '#475569',
        tertiaryColor: '#1f2937',
    }
});

interface MarkdownPaneProps {
    message: string;
}

export function MarkdownPane({ message }: MarkdownPaneProps) {
    const [markdownContent, setMarkdownContent] = useState<string>('')
    const [mermaidKey, setMermaidKey] = useState(0)

    useEffect(() => {
        try {
            const content = JSON.parse(message);
            setMarkdownContent(content)
            setMermaidKey(prev => prev + 1) // Force re-render of Mermaid diagrams
        } catch (error) {
            console.error('Error parsing markdown content:', error)
            setMarkdownContent('')
        }
    }, [message])

    // Re-render Mermaid diagrams when content changes
    useEffect(() => {
        const renderMermaidDiagrams = async () => {
            try {
                // Reset any existing Mermaid diagrams
                document.querySelectorAll('.mermaid').forEach((element) => {
                    element.innerHTML = element.getAttribute('data-content') || '';
                });

                // Re-render all Mermaid diagrams
                await mermaid.run({
                    querySelector: '.mermaid',
                });
            } catch (error) {
                console.error('Error rendering Mermaid diagrams:', error);
            }
        };

        renderMermaidDiagrams();
    }, [markdownContent, mermaidKey]);

    const components = {
        code({ className, children }: any) {
            const match = /language-(\w+)/.exec(className || '')

            // Handle Mermaid diagrams
            if (match && match[1] === 'mermaid') {
                return (
                    <div
                        className="mermaid my-4 p-4 bg-gray-800 rounded-lg overflow-auto"
                        key={mermaidKey}
                        data-content={String(children).replace(/\n$/, '')}
                    >
                        {String(children).replace(/\n$/, '')}
                    </div>
                );
            }

            // Handle other code blocks
            return match ? (
                <div className="relative my-4">
                    {/* Language label */}
                    <div className="absolute right-2 top-2 text-xs text-gray-400 bg-gray-800 px-2 py-1 rounded">
                        {match[1]}
                    </div>
                    <SyntaxHighlighter
                        style={nightOwl as any}
                        language={match[1]}
                        PreTag="div"
                        className="rounded-lg"
                    >
                        {String(children).replace(/\n$/, '')}
                    </SyntaxHighlighter>
                </div>
            ) : (
                <code className={`${className} bg-gray-800 rounded px-1 py-0.5`}>
                    {children}
                </code>
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
            <h3 className="text-xl font-bold my-4">{children}</h3>
        ),
        ul: ({ children }: any) => (
            <ul className="list-disc list-inside my-4 space-y-2">{children}</ul>
        ),
        ol: ({ children }: any) => (
            <ol className="list-decimal list-inside my-4 space-y-2">{children}</ol>
        ),
        li: ({ children }: any) => (
            <li className="ml-4">{children}</li>
        ),
        blockquote: ({ children }: any) => (
            <blockquote className="border-l-4 border-gray-600 pl-4 my-4 italic">
                {children}
            </blockquote>
        ),
        a: ({ href, children }: any) => (
            <a href={href} className="text-blue-400 hover:text-blue-300 underline">
                {children}
            </a>
        ),
        table: ({ children }: any) => (
            <div className="overflow-x-auto my-4">
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
        <div className="mt-4 prose prose-invert prose-dark max-w-none text-left">
            <ReactMarkdown components={components}>
                {markdownContent}
            </ReactMarkdown>
        </div>
    )
}