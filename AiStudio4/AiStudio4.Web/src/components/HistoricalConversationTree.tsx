import * as React from 'react';
import { useWebSocketStore } from '@/stores/useWebSocketStore';

export interface TreeNode {
    id: string;
    text: string;
    children: TreeNode[] | TreeNode;
}

interface HistoricalConversationTreeProps {
    treeData: TreeNode;
    onNodeClick: (nodeId: string) => void;
}

export const HistoricalConversationTree: React.FC<HistoricalConversationTreeProps> = ({ treeData, onNodeClick }) => {
    // Use Zustand store
    const { clientId } = useWebSocketStore();

    const handleNodeClick = async (node: TreeNode) => {
        // Add messageId to URL when loading conversation
        window.history.pushState({}, '', `?messageId=${node.id}`);

        // Call the parent's onNodeClick handler
        onNodeClick(node.id);
    };

    const renderTree = (node: TreeNode): JSX.Element => {
        // Ensure children is always an array
        const children = node.children ? (Array.isArray(node.children) ? node.children : [node.children]) : [];

        return (
            <div key={node.id} className="my-1 transition-all duration-200 ease-in-out">
                <div className="flex items-center p-1 rounded-md hover:bg-gray-700">
                    <div className="w-2.5 h-2.5 bg-blue-500 rounded-full mr-3 flex-shrink-0"></div>
                    <div
                        className="text-sm text-gray-300 hover:text-white cursor-pointer overflow-hidden line-clamp-2 max-w-[calc(100%-1.5rem)] transition-colors duration-150"
                        onClick={() => handleNodeClick(node)}
                        title={node.text}
                    >
                        {node.text}
                    </div>
                </div>
                {children.length > 0 && (
                    <div className="pl-5 mt-1 border-l border-gray-700">
                        {children.map(child => (
                            renderTree(child)
                        ))}
                    </div>
                )}
            </div>
        );
    };

    return <div className="px-2 py-3">{renderTree(treeData)}</div>;
}