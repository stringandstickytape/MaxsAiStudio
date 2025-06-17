import React from 'react';
import { ContentBlock } from '@/types/conv';
import { TextContentRenderer } from './TextContentRenderer';
import { SystemContentRenderer } from './SystemContentRenderer';
import { AiHiddenContentRenderer } from './AiHiddenContentRenderer';
import { ToolContentRenderer } from './ToolContentRenderer';

export interface ContentBlockRendererProps {
  block: ContentBlock;
}

export type ContentBlockRendererComponent = React.FC<ContentBlockRendererProps>;

class ContentBlockRendererRegistry {
  private renderers = new Map<string, ContentBlockRendererComponent>();
  private defaultRenderer: ContentBlockRendererComponent = TextContentRenderer;

  constructor() {
    this.register('text', TextContentRenderer);
    this.register('system', SystemContentRenderer);
    this.register('aihidden', AiHiddenContentRenderer);
    this.register('tool', ToolContentRenderer);
    this.register('toolresponse', ToolContentRenderer); // Reuse for now, can create specific renderer later
  }

  register(type: string, component: ContentBlockRendererComponent) {
    this.renderers.set(type.toLowerCase(), component);
  }

  get(type: string | number): ContentBlockRendererComponent {
    // Convert C# enum number to string if necessary
    const typeKey = (typeof type === 'number' ? ContentType[type] : type).toLowerCase();
    return this.renderers.get(typeKey) || this.defaultRenderer;
  }
}

// C# ContentType enum mapping
const ContentType = {
  0: 'text',
  1: 'system',
  2: 'aihidden',
  3: 'tool',
  4: 'toolresponse',
} as const;

export const contentBlockRendererRegistry = new ContentBlockRendererRegistry();