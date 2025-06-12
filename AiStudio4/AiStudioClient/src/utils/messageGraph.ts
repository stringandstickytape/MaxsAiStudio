
import { Message } from '@/types/conv';

export class MessageGraph {
  private messages: Map<string, Message> = new Map();
  private parentChildMap: Map<string, Set<string>> = new Map();
  private rootMessages: Set<string> = new Set();

  constructor(messages: Message[] = []) {
    this.addMessages(messages);
  }

  addMessages(messages: Message[]): void {
    // First sort messages by timestamp
    const sortedMessages = [...messages].sort((a, b) => a.timestamp - b.timestamp);


    // Add each message individually
    for (const message of sortedMessages) {
      this.addMessage({ ...message }); // Clone to ensure properties are preserved
    }
  }

  addMessage(message: Message): void {
    // Explicitly ensure all properties are copied
    const messageWithAllProps = { ...message };
    
    // Add message to map
    this.messages.set(message.id, messageWithAllProps);

    // Handle parentId relationships
    if (!message.parentId) {
      this.rootMessages.add(message.id);
    } else {
      // If parent exists in our map
      if (this.messages.has(message.parentId)) {
        // Set up parent-child relationship
        if (!this.parentChildMap.has(message.parentId)) {
          this.parentChildMap.set(message.parentId, new Set());
        }
        this.parentChildMap.get(message.parentId)!.add(message.id);
      } else {
        // If parent doesn't exist, treat as a root message
        this.rootMessages.add(message.id);
      }
    }
    
  }

  getMessage(id: string): Message | undefined {
    return this.messages.get(id);
  }

  getChildren(id: string): Message[] {
    const childIds = this.parentChildMap.get(id) || new Set();
    return Array.from(childIds).map((childId) => this.messages.get(childId)!);
  }

  getRootMessages(): Message[] {
    return Array.from(this.rootMessages).map((id) => this.messages.get(id)!);
  }

  
  getMessagePath(messageId: string): Message[] {
    const path: Message[] = [];
    let currentId = messageId;


    while (currentId && this.messages.has(currentId)) {
      // Get the message from the map
      const originalMessage = this.messages.get(currentId)!;
      
      // Create a new object with all properties to ensure proper cloning
      const message: Message = {
        ...originalMessage,
        // Explicitly include critical properties to ensure they're not lost
        id: originalMessage.id,        content: originalMessage.content ?? (originalMessage.contentBlocks ? originalMessage.contentBlocks.map((cb:any)=>cb.content).join('\n\n') : undefined),
        source: originalMessage.source,
        timestamp: originalMessage.timestamp,
        parentId: originalMessage.parentId,
        durationMs: originalMessage.durationMs
      };
      
      // Add to the path
      path.unshift(message);


      if (!message.parentId || !this.messages.has(message.parentId)) {
        break;
      }

      currentId = message.parentId;
    }


    return path;
  }
  
  getAllMessages(): Message[] {
    return Array.from(this.messages.values());
  }
}
