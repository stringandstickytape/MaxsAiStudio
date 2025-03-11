import { CodeBlockRenderer } from '@/components/diagrams/types';
import { MermaidRenderer } from '@/components/diagrams/mermaid-renderer';
import { JsonRenderer } from '@/components/diagrams/json-renderer';
import { HtmlRenderer } from '@/components/diagrams/html-renderer';

class CodeBlockRendererRegistry {
  private renderers: Map<string, CodeBlockRenderer> = new Map();

  register(renderer: CodeBlockRenderer) {
    
    renderer.type.forEach((type) => {
      this.renderers.set(type, renderer);
    });
    renderer.initialize?.();
  }

  get(type: string): CodeBlockRenderer | undefined {
    return this.renderers.get(type);
  }

  async renderAll() {
    
    const uniqueRenderers = new Set(this.renderers.values());

    for (const renderer of uniqueRenderers) {
      
      const typeSelectors = renderer.type.map((type) => `.${type}`).join(', ');
      const elements = document.querySelectorAll(typeSelectors);

      elements.forEach(async (element) => {
        const content = element.getAttribute('data-content') || '';
        await renderer.render(content);
      });
    }
  }
}

export const codeBlockRendererRegistry = new CodeBlockRendererRegistry();
[MermaidRenderer, JsonRenderer, HtmlRenderer].forEach((renderer) => codeBlockRendererRegistry.register(renderer));

