import React from 'react';
import { Button } from '@/components/ui/button';
import { ChevronLeft } from 'lucide-react';
import ReactFlow, { Node, Edge, Position } from 'reactflow';
import 'reactflow/dist/style.css';

interface TreeViewProps {
    onClose?: () => void;
    conversationId: string;
    messages: {
        id: string;
        text: string;
        children: Array<{
            id: string;
            text: string;
            children: any[];
        }>;
    };
    isPinned?: boolean;
}

import { store } from '@/store/store';
import { setActiveConversation } from '@/store/conversationSlice';

export const ConversationTreeView: React.FC<TreeViewProps> = ({ onClose, conversationId, messages }) => {
    const onNodeClick = (_: React.MouseEvent, node: Node) => {
        // Dispatch action to update active conversation and selected message
        console.log('Tree Node clicked:', {
            node: node.id,
            conversationId: conversationId
        });
        store.dispatch(setActiveConversation({
            conversationId: conversationId,
            selectedMessageId: node.id
        }));
    };
    const [nodes, setNodes] = React.useState<Node[]>([]);
    const [edges, setEdges] = React.useState<Edge[]>([]);

    React.useEffect(() => {
        console.log('Incoming messages data:', JSON.stringify(messages, null, 2));
        if (!messages) return;

        try {
            // Helper function to create a unique vertical layout
            const createNodesAndEdges = (node: any, parentId: string | null = null, level = 0, index = 0, totalNodesAtLevel: Map<number, number>) => {
                if (!node) return { nodes: [], edges: [] };

                // Get total nodes at this level for centering
                if (!totalNodesAtLevel.has(level)) {
                    totalNodesAtLevel.set(level, 0);
                }
                const nodeIndex = totalNodesAtLevel.get(level)!;
                totalNodesAtLevel.set(level, nodeIndex + 1);

                // Calculate position
                const xSpacing = 250; // Horizontal spacing between nodes
                const ySpacing = 100; // Vertical spacing between levels
                const x = nodeIndex * xSpacing;
                const y = level * ySpacing;

                const currentNode: Node = {
                    id: node.id,
                    position: { x, y },
                    data: { label: node.text?.substring(0, 20) + (node.text?.length > 20 ? '...' : '') },
                    style: {
                        background: '#3b82f6',
                        color: '#ffffff',
                        border: '1px solid #2563eb',
                        borderRadius: '8px',
                        padding: '10px',
                        width: 'auto',
                        minWidth: '150px',
                    }
                };

                let nodes: Node[] = [currentNode];
                let edges: Edge[] = [];

                if (parentId) {
                    edges.push({
                        id: `${parentId}-${node.id}`,
                        source: parentId,
                        target: node.id,
                        type: 'smoothstep',
                        style: { stroke: '#4b5563' }
                    });
                }

                if (node.children) {
                    const childrenArray = Array.isArray(node.children) ? node.children : [node.children];
                    childrenArray.forEach((child: any, childIndex: number) => {
                        if (child && child.id) {
                            const childResults = createNodesAndEdges(
                                child,
                                node.id,
                                level + 1,
                                childIndex,
                                totalNodesAtLevel
                            );
                            nodes = [...nodes, ...childResults.nodes];
                            edges = [...edges, ...childResults.edges];
                        }
                    });
                }

                return { nodes, edges };
            };

            // Create a Map to track the number of nodes at each level
            const totalNodesAtLevel = new Map<number, number>();
            const { nodes: newNodes, edges: newEdges } = createNodesAndEdges(messages, null, 0, 0, totalNodesAtLevel);
            setNodes(newNodes);
            setEdges(newEdges);

        } catch (error) {
            console.error('Error creating tree visualization:', error);
        }

    }, [messages]);

    return (
        <div className="h-[calc(100vh-70px)] w-full overflow-auto">
            <div className="h-[calc(100vh-70px)]">
                {!messages ? (
                    <div className="text-gray-400 text-center p-4">No messages to display</div>
                ) : (
                        <ReactFlow
                            nodes={nodes}
                            edges={edges}
                            fitView
                            className="bg-[#1f2937]"
                            minZoom={0.1}
                            maxZoom={1.5}
                            defaultZoom={0.8}
                            attributionPosition="bottom-left"
                            onNodeClick={onNodeClick}
                        />
                )}
            </div>
        </div>
    );
};