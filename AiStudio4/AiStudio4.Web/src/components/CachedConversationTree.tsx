import * as React from 'react';
import { wsManager } from '../services/websocket/WebSocketManager';

export interface TreeNode {
    id: string;
    text: string;
    children: TreeNode[];
}

interface CachedConversationTreeProps {
    treeData: TreeNode;
}

export const CachedConversationTree: React.FC<CachedConversationTreeProps> = ({ treeData }) => {

    const handleNodeClick = async (node: TreeNode) => {
        try {
            const response = await fetch('/api/conversationmessages', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'X-Client-Id': wsManager.getClientId() || ''
                },
                body: JSON.stringify({
                    messageId: node.id
                })
            });
            if (!response.ok) throw new Error('Failed to fetch conversation messages');
            const data = await response.json();
            console.log('Fetched conversation data:', data);
            if (data.success && data.messages && data.conversationId) {
                // Send the data through WebSocket manager for proper handling
                wsManager.send({
                    messageType: 'loadConversation',
                    content: {
                        conversationId: data.conversationId,
                        messages: data.messages
                    }
                });
            }
        } catch (error) {
            console.error('Error loading conversation:', error);
        }
    };

    const renderTree = (node: TreeNode): JSX.Element => (
        <div key={node.id} className="py-1">
            <div className="flex items-center">
                <div className="w-2 h-2 bg-gray-500 rounded-full mr-2"></div>
                <div 
                    className="text-sm text-gray-300 hover:text-white cursor-pointer whitespace-nowrap overflow-hidden text-ellipsis max-w-[calc(100%-1rem)]"
                    onClick={() => handleNodeClick(node)}
                >
                    {node.text}
                </div>
            </div>
            {node.children && node.children.length > 0 && (
                <div className="pl-4 mt-1">
                    {node.children.map(child => (
                        renderTree(child)
                    ))}
                </div>
            )}
        </div>
    );

    return <div>{renderTree(treeData)}</div>;
};