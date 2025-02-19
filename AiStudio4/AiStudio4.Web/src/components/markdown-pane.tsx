import { useState, useEffect } from "react"
import ReactMarkdown from 'react-markdown'
import { Prism as SyntaxHighlighter } from 'react-syntax-highlighter'
import { nightOwl } from 'react-syntax-highlighter/dist/cjs/styles/prism'

interface MarkdownPaneProps {
    message: string;
}

export function MarkdownPane({ message }: MarkdownPaneProps) {
    const [markdownContent, setMarkdownContent] = useState<string>('')

    useEffect(() => {
        try {
            const content = JSON.parse(message);
            setMarkdownContent(content)
        } catch (error) {
            console.error('Error parsing markdown content:', error)
            setMarkdownContent('')
        }
    }, [message])

    const components = {
        code({ className, children }: any) {
            const match = /language-(\w+)/.exec(className || '')
            return match ? (
                <div className="relative">
                    {/* Language label */}
                    <div className="absolute right-2 top-2 text-xs text-gray-400 bg-gray-800 px-2 py-1 rounded">
                        {match[1]}
                    </div>
                    <SyntaxHighlighter
                        style={nightOwl as any}
                        language={match[1]}
                        PreTag="div"
                    >
                        {String(children).replace(/\n$/, '')}
                    </SyntaxHighlighter>
                </div>
            ) : (
                <code className={className}>
                    {children}
                </code>
            )
        }
    }

    return (
        <div className="mt-4 prose prose-invert prose-dark max-w-none">
            <ReactMarkdown components={components}>
                {markdownContent}
            </ReactMarkdown>
        </div>
    )
}