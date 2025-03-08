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
            <div key={node.id} className="">
                <div className="flex items-center">
                    <div className="w-2 h-2 bg-gray-500 rounded-full mr-2"></div>
                    <div
                        className="text-sm text-gray-300 hover:text-white cursor-pointer whitespace-pre overflow-hidden line-clamp-3 max-w-[calc(100%-1rem)]"
                        onClick={() => handleNodeClick(node)}
                    >
                        {node.text}
                    </div>
                </div>
                {children.length > 0 && (
                    <div className="pl-4 mt-1">
                        {children.map(child => (
                            renderTree(child)
                        ))}
                    </div>
                )}
            </div>
        );
    };

    return <div>{renderTree(treeData)}</div>;
}