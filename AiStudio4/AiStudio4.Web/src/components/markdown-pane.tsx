import { useState, useEffect } from "react"
import { evaluate } from '@mdx-js/mdx'
import * as runtime from 'react/jsx-runtime'

export function MarkdownPane() {
    const [Content, setContent] = useState<React.ComponentType | null>(null)

    useEffect(() => {
        const compileMdx = async () => {
            const mdxContent = `
# Dynamic MDX

This content was generated programmatically!

- Item 1
- Item 2
            `

            const { default: Component } = await evaluate(mdxContent, {
                ...runtime,
                development: false
            })

            setContent(() => Component)
        }

        compileMdx()
    }, [])

    return (
        <div className="mt-4 prose dark:prose-invert">
            {Content && <Content />}
        </div>
    )
}