import { DiagramRenderer } from './types';
import { MermaidRenderer } from './mermaid-renderer';

class DiagramRegistry {
    private renderers: Map<string, DiagramRenderer> = new Map();

    register(renderer: DiagramRenderer) {
        this.renderers.set(renderer.type, renderer);
        renderer.initialize?.();
    }

    get(type: string): DiagramRenderer | undefined {
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

export const diagramRegistry = new DiagramRegistry();
diagramRegistry.register(MermaidRenderer);