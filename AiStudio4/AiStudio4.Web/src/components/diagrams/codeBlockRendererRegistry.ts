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
    // Optimization with fallback to ensure layout rendering
    const uniqueRenderers = new Set(this.renderers.values());
    
    for (const renderer of uniqueRenderers) {
      // Support both class-based and data-attribute selectors for compatibility
      const typeSelectors = renderer.type.map((type) => `.${type}, [data-type="${type}"]`).join(', ');
      const elements = document.querySelectorAll(typeSelectors);
      
      if (elements.length > 0) {
        // Process in batches to avoid blocking the main thread
        const batchSize = 5;
        for (let i = 0; i < elements.length; i += batchSize) {
          const batch = Array.from(elements).slice(i, i + batchSize);
          
          // Process batch
          await Promise.all(batch.map(async (element) => {
            const content = element.getAttribute('data-content') || '';
            try {
              await renderer.render(content, element as HTMLElement);
            } catch (error) {
              console.error('Failed to render diagram:', error);
            }
          }));
          
          // Small delay to allow UI to breathe
          if (i + batchSize < elements.length) {
            await new Promise(resolve => setTimeout(resolve, 10));
          }
        }
      }
    }
  }
}

export const codeBlockRendererRegistry = new CodeBlockRendererRegistry();
[MermaidRenderer, JsonRenderer, HtmlRenderer].forEach((renderer) => codeBlockRendererRegistry.register(renderer));

