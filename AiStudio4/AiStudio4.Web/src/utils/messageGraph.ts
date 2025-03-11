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
    // Sort messages by timestamp to ensure parents are processed first
    const sortedMessages = [...messages].sort((a, b) => a.timestamp - b.timestamp);

    for (const message of sortedMessages) {
      this.addMessage(message);
    }
  }

  addMessage(message: Message): void {
    // Store the message
    this.messages.set(message.id, message);

    // If it has no parent, it's a root message
    if (!message.parentId) {
      this.rootMessages.add(message.id);
    } else {
      // Make sure the parent exists in our messages
      if (this.messages.has(message.parentId)) {
        // Add this message as a child of its parent
        if (!this.parentChildMap.has(message.parentId)) {
          this.parentChildMap.set(message.parentId, new Set());
        }
        this.parentChildMap.get(message.parentId)!.add(message.id);
      } else {
        // Parent doesn't exist (yet), treat as root for now
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

  // Get the path from a message back to root
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

  // Get a flat list of messages with their relationships
  getMessageRelationships(): { messageId: string; childIds: string[] }[] {
    return Array.from(this.messages.keys()).map((messageId) => ({
      messageId,
      childIds: Array.from(this.parentChildMap.get(messageId) || new Set()),
    }));
  }

  // Get all messages as a flat array
  getAllMessages(): Message[] {
    return Array.from(this.messages.values());
  }
}
