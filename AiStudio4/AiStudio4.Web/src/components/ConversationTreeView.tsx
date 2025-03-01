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
import { cn } from '@/lib/utils';

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
                const xSpacing = 280; // Horizontal spacing between nodes
                const ySpacing = 150; // Increased vertical spacing between levels for top/bottom connections
                const x = nodeIndex * xSpacing;
                const y = level * ySpacing;

                // Determine if this is a user or AI message based on position in tree
                const isUserMessage = level % 2 === 0;
                
                const currentNode: Node = {
                    id: node.id,
                    position: { x, y },
                    data: { 
                        label: (
                            <div className="flex flex-col gap-1">
                                <div className="text-xs font-semibold">{isUserMessage ? 'You' : 'AI'}</div>
                                <div>{node.text?.substring(0, 30) + (node.text?.length > 30 ? '...' : '')}</div>
                            </div>
                        ) 
                    },
                    style: {
                        background: isUserMessage ? '#1e40af' : '#4f46e5',
                        color: '#ffffff',
                        border: isUserMessage ? '1px solid #1e3a8a' : '1px solid #4338ca',
                        borderRadius: '10px',
                        padding: '12px',
                        width: '180px',
                        boxShadow: '0 4px 6px -1px rgba(0, 0, 0, 0.1), 0 2px 4px -1px rgba(0, 0, 0, 0.06)',
                        fontSize: '0.9rem',
                    },
                    sourcePosition: Position.Bottom,
                    targetPosition: Position.Top
                };

                let nodes: Node[] = [currentNode];
                let edges: Edge[] = [];

                if (parentId) {
                    edges.push({
                        id: `${parentId}-${node.id}`,
                        source: parentId,
                        target: node.id,
                        type: 'smoothstep',
                        animated: true,
                        style: { stroke: '#6b7280', strokeWidth: 2 }
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
        <div className="flex flex-col h-[calc(100vh-70px)] w-full">
                {onClose && (
                    <Button 
                        variant="ghost" 
                        size="sm" 
                        onClick={onClose}
                        className="hover:bg-gray-700"
                    >
                        <ChevronLeft className="h-5 w-5" />
                        <span className="ml-1">Back</span>
                    </Button>
                )}
            
            <div className={cn(
                "flex-1 overflow-hidden",
                !messages && "flex items-center justify-center"
            )}>
                {!messages ? (
                    <div className="text-gray-400 text-center p-4 bg-gray-900 rounded-md shadow-inner mx-auto my-8 max-w-md border border-gray-800">
                        <p>No conversation history to display</p>
                        <p className="text-sm mt-2 text-gray-500">Start a new conversation to see the tree view</p>
                    </div>
                ) : (
                    <ReactFlow
                        nodes={nodes}
                        edges={edges}
                        fitView
                        className="bg-[#111827]"
                        minZoom={0.1}
                        maxZoom={1.5}
                        defaultZoom={0.8}
                        attributionPosition="bottom-left"
                        onNodeClick={onNodeClick}
                        nodesDraggable={true}
                        zoomOnScroll={true}
                        panOnScroll={true}
                        panOnDrag={true}
                    />
                )}
            </div>
        </div>
    );
};