import { Message, ContentBlock } from '../types/conv';

export const MessageUtils = {
  // Helper to check if a block is text content
  isVisibleToUser(block: ContentBlock): boolean {
        return block.contentType === 'text' || block.contentType === 'system' || block.contentType === 'toolresponse';
  },

  // Primary method: Get content as string when needed
  getContent(message: Message): string {
    return message.contentBlocks?.map(cb => cb.content).join('\n\n') ?? '';
  },

  // Create message with text content
  createTextMessage(content: string, source: Message['source']): Omit<Message, 'id' | 'timestamp'> {
    return {
      contentBlocks: [{ content, contentType: 'text' }],
      source,
      parentId: null,
    };
  },

  // Create message with multiple content blocks
  createMessage(contentBlocks: ContentBlock[], source: Message['source']): Omit<Message, 'id' | 'timestamp'> {
    return {
      contentBlocks,
      source,
      parentId: null,
    };
  },

  // Helper for streaming content updates
  updateContentBlock(message: Message, blockIndex: number, newContent: string): Message {
    const updatedBlocks = [...message.contentBlocks];
    updatedBlocks[blockIndex] = { ...updatedBlocks[blockIndex], content: newContent };
    return { ...message, contentBlocks: updatedBlocks };
  },

  // Helper for appending to last block (streaming)
  appendToLastBlock(message: Message, additionalContent: string): Message {
    if (!message.contentBlocks?.length) {
      return { ...message, contentBlocks: [{ content: additionalContent, contentType: 'text' }] };
    }
    
    const updatedBlocks = [...message.contentBlocks];
    const lastIndex = updatedBlocks.length - 1;
    updatedBlocks[lastIndex] = {
      ...updatedBlocks[lastIndex],
      content: updatedBlocks[lastIndex].content + additionalContent
    };
    
    return { ...message, contentBlocks: updatedBlocks };
  },

  // Helper to check if message has text content
  hasTextContent(message: Message): boolean {
    return message.contentBlocks?.some(block => this.isVisibleToUser(block) && block.content.trim().length > 0) ?? false;
  },

  // Helper to get only text content blocks
  getTextBlocks(message: Message): ContentBlock[] {
    return message.contentBlocks?.filter(block => this.isVisibleToUser(block)) ?? [];
  },

  // Helper to get content of specific type
  getContentByType(message: Message, contentType: ContentBlock['contentType']): string {
    return message.contentBlocks
      ?.filter(block => block.contentType === contentType)
      .map(block => block.content)
      .join('\n\n') ?? '';
  }
};