import { Message } from '@/types/conversation';

/**
 * Builds a tree structure from an array of messages
 * @param messages Array of messages to build tree from
 * @param includeContent Whether to include full message content in tree nodes
 * @returns Tree structure with parent-child relationships
 */
export function buildMessageTree(messages: Message[], includeContent: boolean = false) {
    // Sort messages by timestamp to ensure parents are processed before children
    const sortedMessages = [...messages].sort((a, b) => a.timestamp - b.timestamp);
    
    const messageMap = new Map<string, any>();
    let rootMessage: any = null;

    // First pass: Create all nodes and identify root
    sortedMessages.forEach(msg => {
        const node = {
            id: msg.id,
            text: includeContent ? msg.content : 
                  (msg.content?.substring(0, 50) + (msg.content?.length > 50 ? '...' : '')),
            children: [] as any[],
            source: msg.source,
            timestamp: msg.timestamp,
            parentId: msg.parentId
        };
        messageMap.set(msg.id, node);

        // The first message is typically the root
        if (!rootMessage && msg.source === 'system') {
            rootMessage = node;
        }
    });

    // If no explicit root was found, create one
    if (!rootMessage) {
        rootMessage = {
            id: 'root',
            text: 'Conversation Root',
            children: [],
            source: 'system',
            timestamp: Date.now()
        };
    }

    // Second pass: Build tree structure
    sortedMessages.forEach(msg => {
        const node = messageMap.get(msg.id);
        // Skip the root message
        if (node === rootMessage) return;

        // Find parent - either the specified parent or the previous message
        let parentNode;
        if (msg.parentId && messageMap.has(msg.parentId)) {
            parentNode = messageMap.get(msg.parentId);
        } else {
            // If no parent specified, add to root
            parentNode = rootMessage;
        }

        if (parentNode) {
            parentNode.children.push(node);
        }
    });
    
    return rootMessage;
}

/**
 * Create a debug-friendly tree representation for console output
 * @param messages Array of messages to visualize
 * @returns Tree structure suitable for console debugging
 */
export function buildDebugTree(messages: Message[]) {
    // Use the same implementation with different formatting
    return buildMessageTree(messages, true);
}