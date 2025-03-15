// src/utils/messageGraph.ts
import { Message } from '@/types/conv';

export class MessageGraph {
  private messages: Map<string, Message> = new Map();
  private parentChildMap: Map<string, Set<string>> = new Map();
  private rootMessages: Set<string> = new Set();

  constructor(messages: Message[] = []) {
    this.addMessages(messages);
  }

  addMessages(messages: Message[]): void {
    
    const sortedMessages = [...messages].sort((a, b) => a.timestamp - b.timestamp);

    for (const message of sortedMessages) {
      this.addMessage(message);
    }
  }

  addMessage(message: Message): void {
    
    this.messages.set(message.id, message);

    
    if (!message.parentId) {
      this.rootMessages.add(message.id);
    } else {
      
      if (this.messages.has(message.parentId)) {
        
        if (!this.parentChildMap.has(message.parentId)) {
          this.parentChildMap.set(message.parentId, new Set());
        }
        this.parentChildMap.get(message.parentId)!.add(message.id);
      } else {
        
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
      const message = this.messages.get(currentId)!;
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
