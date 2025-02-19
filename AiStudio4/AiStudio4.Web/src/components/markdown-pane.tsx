import { useState, useEffect } from "react"
import { evaluate } from '@mdx-js/mdx'
import * as runtime from 'react/jsx-runtime'

interface MarkdownPaneProps {
    message: string;
}

export function MarkdownPane({ message }: MarkdownPaneProps) {
    const [Content, setContent] = useState<React.ComponentType | null>(null)

    useEffect(() => {
        const compileMdx = async () => {
            
            const mdxContent = `
# Message
# Message2
${JSON.parse(message)}
            `

            console.log(mdxContent)
            debugger;

            const { default: Component } = await evaluate(mdxContent, {
                ...runtime,
                development: false
            })

            setContent(() => Component)
        }

        compileMdx()
    }, [message])

    return (
        <div className="mt-4 prose dark:prose-invert">
            {Content && <Content />}
        </div>
    )
}