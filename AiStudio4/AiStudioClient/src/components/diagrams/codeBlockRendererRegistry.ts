﻿// src/components/diagrams/codeBlockRendererRegistry.ts
import { CodeBlockRenderer } from '@/components/diagrams/types';
import { MermaidRenderer } from '@/components/diagrams/mermaid-renderer';
import { JsonRenderer } from '@/components/diagrams/json-renderer';
import { HtmlRenderer } from '@/components/diagrams/html-renderer';
import { DotRenderer } from '@/components/diagrams/dot-renderer'; // Add this line
import { TxtRenderer } from '@/components/diagrams/txt-renderer'; // Add this line
import { CodeDiffRenderer } from '@/components/diagrams/codeDiff-renderer'; // Add this line
import { ModifyFilesRenderer } from '@/components/diagrams/modifyFiles-renderer'; // Add this line
import { GitCommitRenderer } from '@/components/diagrams/gitCommit-renderer'; // Add this line
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

    async renderAll() {
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
                        try {
                            await renderer.render(content, element as HTMLElement);
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
[MermaidRenderer, JsonRenderer, HtmlRenderer, DotRenderer, TxtRenderer, CodeDiffRenderer, ModifyFilesRenderer, GitCommitRenderer].forEach((renderer) => codeBlockRendererRegistry.register(renderer)); // Update this line