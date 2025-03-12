import { Message } from '@/types/conv';


export function buildMessageTree(messages: Message[], includeContent: boolean = false) {
  
  const sortedMessages = [...messages].sort((a, b) => a.timestamp - b.timestamp);

  const messageMap = new Map<string, any>();
  let rootMessage: any = null;

  
  sortedMessages.forEach((msg) => {
    const node = {
      id: msg.id,
      text: includeContent ? msg.content : msg.content?.substring(0, 50) + (msg.content?.length > 50 ? '...' : ''),
      children: [] as any[],
      source: msg.source,
      timestamp: msg.timestamp,
        parentId: msg.parentId,
      costInfo: msg.costInfo
    };
    messageMap.set(msg.id, node);

    
    if (!rootMessage && msg.source === 'system') {
      rootMessage = node;
    }
  });

  
  if (!rootMessage) {
    rootMessage = {
      id: 'root',
      text: 'Conv Root',
      children: [],
      source: 'system',
      timestamp: Date.now(),
    };
  }

  
  sortedMessages.forEach((msg) => {
    const node = messageMap.get(msg.id);
    
    if (node === rootMessage) return;

    
    let parentNode;
    if (msg.parentId && messageMap.has(msg.parentId)) {
      parentNode = messageMap.get(msg.parentId);
    } else {
      
      parentNode = rootMessage;
    }

    if (parentNode) {
      parentNode.children.push(node);
    }
  });

  return rootMessage;
}


export function buildDebugTree(messages: Message[]) {
  
  return buildMessageTree(messages, true);
}

