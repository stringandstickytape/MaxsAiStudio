import { CodeBlockRenderer } from '@/components/diagrams/types';
import { MermaidRenderer } from '@/components/diagrams/mermaid-renderer';
import { JsonRenderer } from '@/components/diagrams/json-renderer';

class CodeBlockRendererRegistry {
    private renderers: Map<string, CodeBlockRenderer> = new Map();

    register(renderer: CodeBlockRenderer) {
        this.renderers.set(renderer.type, renderer);
        renderer.initialize?.();
    }

    get(type: string): CodeBlockRenderer | undefined {
        return this.renderers.get(type);
    }

    async renderAll() {
        for (const renderer of this.renderers.values()) {
            const elements = document.querySelectorAll(`.${renderer.type}`);
            elements.forEach(async (element) => {
                const content = element.getAttribute('data-content') || '';
                await renderer.render(content);
            });
        }
    }
}

export const codeBlockRendererRegistry = new CodeBlockRendererRegistry();
codeBlockRendererRegistry.register(MermaidRenderer);
codeBlockRendererRegistry.register(JsonRenderer);