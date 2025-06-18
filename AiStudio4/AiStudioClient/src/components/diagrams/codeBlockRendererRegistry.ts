// src/components/diagrams/codeBlockRendererRegistry.ts
import { CodeBlockRenderer } from '@/components/diagrams/types';
import { MermaidRenderer } from '@/components/diagrams/mermaid-renderer';
import { JsonRenderer } from '@/components/diagrams/json-renderer';
import { HtmlRenderer } from '@/components/diagrams/html-renderer';
import { DotRenderer } from '@/components/diagrams/dot-renderer'; // Add this line
import { TxtRenderer } from '@/components/diagrams/txt-renderer'; // Add this line
import { CodeDiffRenderer } from '@/components/diagrams/codeDiff-renderer'; // Add this line
import { ModifyFilesRenderer } from '@/components/diagrams/modifyFiles-renderer'; // Add this line
import { GitCommitRenderer } from '@/components/diagrams/gitCommit-renderer'; // Add this line
import { AbcRenderer } from '@/components/diagrams/abc-renderer'; // Add this line
import { ThinkAndAwaitUserInputRenderer } from '@/components/diagrams/thinkAndAwaitUserInput-renderer'; // Add this line
class CodeBlockRendererRegistry {
    private renderers: Map<string, CodeBlockRenderer> = new Map();

    register(renderer: CodeBlockRenderer) {
        renderer.type.forEach((type) => {
            this.renderers.set(type, renderer);
        });
        renderer.initialize?.();
    }

    get(type: string): CodeBlockRenderer {
        return this.renderers.get(type) || this.renderers.get('txt')!;
    }

    isMarkdownComplete(markdown: string): boolean {
        // Count opening and closing fences
        const openingFences = (markdown.match(/^```/gm) || []).length;
        const closingFences = (markdown.match(/^```$/gm) || []).length;
        
        // Also check for incomplete fence at the end
        const endsWithIncompleteFence = /```[^`]*$/.test(markdown);
        
        return openingFences === closingFences && !endsWithIncompleteFence;
    }

    shouldRenderCodeBlock(fullMarkdown: string, codeBlockContent: string): boolean {
        // Always render if markdown is complete
        if (this.isMarkdownComplete(fullMarkdown)) {
            return true;
        }

        // Find the last incomplete code block
        const lastIncompleteMatch = fullMarkdown.match(/```[^`]*$/);
        if (lastIncompleteMatch) {
            const incompleteContent = lastIncompleteMatch[0].replace(/^```[^\n]*\n?/, '');
            // Don't render if this is the incomplete block at the end
            return incompleteContent !== codeBlockContent;
        }

        return true;
    }

    async renderAll(fullMarkdown?: string) {
        const uniqueRenderers = new Set(this.renderers.values());

        for (const renderer of uniqueRenderers) {
            const typeSelectors = renderer.type.map((type) => `.${type}, [data-type="${type}"]`).join(', ');
            const elements = document.querySelectorAll(typeSelectors);

            if (elements.length > 0) {
                const batchSize = 5;
                for (let i = 0; i < elements.length; i += batchSize) {
                    const batch = Array.from(elements).slice(i, i + batchSize);

                    await Promise.all(batch.map(async (element) => {
                        const content = element.getAttribute('data-content') || '';
                        
                        // Skip rendering if we have the full markdown and this block shouldn't be rendered
                        if (fullMarkdown && !this.shouldRenderCodeBlock(fullMarkdown, content)) {
                            return;
                        }

                        try {
                            // The render method doesn't exist on the renderers - they use React components
                            // This renderAll method seems to be for non-React renderers or legacy code
                            // Since we're dealing with React components, we don't need to do anything here
                            // The rendering is handled by the React components themselves
                        } catch (error) {
                            console.error('Failed to render diagram:', error);
                        }
                    }));

                    if (i + batchSize < elements.length) {
                        await new Promise(resolve => setTimeout(resolve, 10));
                    }
                }
            }
        }
    }
}

export const codeBlockRendererRegistry = new CodeBlockRendererRegistry();
[MermaidRenderer, JsonRenderer, HtmlRenderer, DotRenderer, TxtRenderer, CodeDiffRenderer, ModifyFilesRenderer, GitCommitRenderer, AbcRenderer, ThinkAndAwaitUserInputRenderer].forEach((renderer) => codeBlockRendererRegistry.register(renderer)); // Update this line