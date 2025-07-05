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
import { MarkdownMarpRenderer } from '@/components/diagrams/markdown-marp-renderer';
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
        // Use nested-aware parsing to check completeness
        return this.parseNestedCodeBlocks(markdown).isComplete;
    }
    
    // Helper function to parse nested code blocks
    private parseNestedCodeBlocks(markdown: string): { isComplete: boolean; blocks: Array<{ start: number; end: number; backtickCount: number }> } {
        const blocks: Array<{ start: number; end: number; backtickCount: number }> = [];
        let currentIndex = 0;
        
        while (currentIndex < markdown.length) {
            const codeBlockStart = this.findNextCodeBlockStart(markdown, currentIndex);
            
            if (codeBlockStart.index === -1) {
                break;
            }
            
            const codeBlockEnd = this.findMatchingCodeBlockEnd(markdown, codeBlockStart.index, codeBlockStart.backtickCount);
            
            if (codeBlockEnd === -1) {
                // Incomplete code block found
                return { isComplete: false, blocks };
            }
            
            blocks.push({
                start: codeBlockStart.index,
                end: codeBlockEnd,
                backtickCount: codeBlockStart.backtickCount
            });
            
            currentIndex = codeBlockEnd;
        }
        
        return { isComplete: true, blocks };
    }
    
    // Helper function to find the next code block start with backtick count
    private findNextCodeBlockStart(markdown: string, startIndex: number): { index: number; backtickCount: number } {
        let index = startIndex;
        
        while (index < markdown.length) {
            const backtickMatch = markdown.slice(index).match(/^```+/m);
            if (!backtickMatch) {
                return { index: -1, backtickCount: 0 };
            }
            
            const matchIndex = index + markdown.slice(index).indexOf(backtickMatch[0]);
            
            // Check if this is at the start of a line
            if (matchIndex === 0 || markdown[matchIndex - 1] === '\n') {
                return { 
                    index: matchIndex, 
                    backtickCount: backtickMatch[0].length 
                };
            }
            
            index = matchIndex + 1;
        }
        
        return { index: -1, backtickCount: 0 };
    }
    
    // Helper function to find the matching closing fence
    private findMatchingCodeBlockEnd(markdown: string, startIndex: number, backtickCount: number): number {
        const openingFence = markdown.slice(startIndex);
        const firstNewlineIndex = openingFence.indexOf('\n');
        
        if (firstNewlineIndex === -1) {
            return -1; // No content after opening fence
        }
        
        let searchIndex = startIndex + firstNewlineIndex + 1;
        
        while (searchIndex < markdown.length) {
            const remainingContent = markdown.slice(searchIndex);
            const lineStart = remainingContent.match(/^```+/m);
            
            if (!lineStart) {
                return -1; // No closing fence found
            }
            
            const lineStartIndex = searchIndex + remainingContent.indexOf(lineStart[0]);
            
            // Check if this is at the start of a line and has enough backticks
            if ((lineStartIndex === 0 || markdown[lineStartIndex - 1] === '\n') && 
                lineStart[0].length >= backtickCount) {
                
                // Find the end of this line
                const lineEndIndex = markdown.indexOf('\n', lineStartIndex);
                return lineEndIndex === -1 ? markdown.length : lineEndIndex + 1;
            }
            
            searchIndex = lineStartIndex + 1;
        }
        
        return -1; // No matching closing fence found
    }

    shouldRenderCodeBlock(fullMarkdown: string, codeBlockContent: string): boolean {
        const parseResult = this.parseNestedCodeBlocks(fullMarkdown);
        
        // Always render if markdown is complete
        if (parseResult.isComplete) {
            return true;
        }

        // Find the last incomplete code block using nested-aware parsing
        const lastCodeBlockStart = this.findNextCodeBlockStart(fullMarkdown, 0);
        let currentIndex = 0;
        let lastIncompleteStart = -1;
        
        while (currentIndex < fullMarkdown.length) {
            const codeBlockStart = this.findNextCodeBlockStart(fullMarkdown, currentIndex);
            
            if (codeBlockStart.index === -1) {
                break;
            }
            
            const codeBlockEnd = this.findMatchingCodeBlockEnd(fullMarkdown, codeBlockStart.index, codeBlockStart.backtickCount);
            
            if (codeBlockEnd === -1) {
                // This is an incomplete code block
                lastIncompleteStart = codeBlockStart.index;
                break;
            }
            
            currentIndex = codeBlockEnd;
        }
        
        if (lastIncompleteStart !== -1) {
            // Extract the content of the incomplete block
            const openingFence = fullMarkdown.slice(lastIncompleteStart);
            const firstNewlineIndex = openingFence.indexOf('\n');
            
            if (firstNewlineIndex !== -1) {
                const incompleteContent = openingFence.slice(firstNewlineIndex + 1);
                // Don't render if this is the incomplete block at the end
                return incompleteContent !== codeBlockContent;
            }
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
[MermaidRenderer, JsonRenderer, HtmlRenderer, DotRenderer, TxtRenderer, CodeDiffRenderer, ModifyFilesRenderer, GitCommitRenderer, AbcRenderer, ThinkAndAwaitUserInputRenderer, MarkdownMarpRenderer].forEach((renderer) => codeBlockRendererRegistry.register(renderer)); // Update this line