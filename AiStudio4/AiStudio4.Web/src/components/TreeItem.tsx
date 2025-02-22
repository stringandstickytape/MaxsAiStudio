import { wsManager } from '@/services/websocket/WebSocketManager';

interface TreeNode {
    id: string;
    text: string;
    children?: TreeNode[];
}

interface TreeItemProps {
    node: TreeNode;
}

export const TreeItem = ({ node }: TreeItemProps) => {
    const [isCollapsed, setIsCollapsed] = useState(false);

    const handleNodeClick = async () => {
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
                const conversationData = {
                    messageType: 'loadConversation',
                    content: {
                        conversationId: data.conversationId,
                        messages: data.messages
                    }
                };
                console.log('Dispatching conversation data:', conversationData);
                wsManager.send(conversationData);
            }
        } catch (error) {
            console.error('Error loading conversation:', error);
        }
    };

    return (
        <div key={node.id} className="py-1">
            <div className="flex items-center">
                {node.children && node.children.length > 0 ? (
                    <div 
                        className="w-2 h-2 bg-gray-500 rounded-full mr-2 cursor-pointer hover:bg-gray-400 transition-colors"
                        onClick={() => setIsCollapsed(!isCollapsed)}
                    ></div>
                ) : (
                    <div className="w-2 h-2 bg-gray-500 rounded-full mr-2"></div>
                )}
                <div 
                    className="text-sm text-gray-300 hover:text-white cursor-pointer"
                    onClick={handleNodeClick}
                >
                    {node.text}
                </div>
            </div>
            {node.children && node.children.length > 0 && !isCollapsed && (
                <div className="pl-4 mt-1">
                    {node.children.map(child => <TreeItem key={child.id} node={child} />)}
                </div>
            )}
        </div>
    );
};