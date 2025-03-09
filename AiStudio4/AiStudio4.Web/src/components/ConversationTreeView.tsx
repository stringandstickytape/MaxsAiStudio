// src/components/ConversationTreeView.tsx
import React, { useState, useEffect, useMemo } from 'react';
import ReactFlow, {
    Node,
    Edge,
    Position,
    MarkerType,
    ReactFlowProvider,
    Controls
} from 'reactflow';
import 'reactflow/dist/style.css';
import { cn } from '@/lib/utils';
import { Message } from '@/types/conversation';
import { MessageGraph } from '@/utils/messageGraph';
import { useConversationStore } from '@/stores/useConversationStore';

interface TreeViewProps {
    conversationId: string;
    messages: Message[];
}

export const ConversationTreeView: React.FC<TreeViewProps> = ({
    conversationId,
    messages
}) => {
    const [nodes, setNodes] = useState<Node[]>([]);
    const [edges, setEdges] = useState<Edge[]>([]);
    const { setActiveConversation, getConversation } = useConversationStore();

    // Add this key state to force re-renders when needed
    const [updateKey, setUpdateKey] = useState(0);

    const onNodeClick = (_: React.MouseEvent, node: Node) => {
        // Set active conversation and selected message
        console.log('Tree Node clicked:', {
            node: node.id,
            conversationId: conversationId
        });
        setActiveConversation({
            conversationId: conversationId,
            selectedMessageId: node.id
        });
    };

    // Get the most up-to-date messages from the conversation store
    const currentMessages = useMemo(() => {
        const conversation = getConversation(conversationId);
        return conversation?.messages || messages;
    }, [getConversation, conversationId, messages, updateKey]);

    // Force a refresh when conversationId changes
    useEffect(() => {
        setUpdateKey(prev => prev + 1);
    }, [conversationId]);

    useEffect(() => {
        console.log('Conversation tree building with message count:', currentMessages.length);
        if (!currentMessages || currentMessages.length === 0) return;

        try {
            // Create a message graph from the messages
            const graph = new MessageGraph(currentMessages);

            // Call transformToReactFlow directly with the flat message array and relationships
            const { nodes: flowNodes, edges: flowEdges } = transformToReactFlow(
                graph.getAllMessages(),
                graph
            );

            console.log('Tree transformation complete:', {
                nodeCount: flowNodes.length,
                edgeCount: flowEdges.length
            });

            setNodes(flowNodes);
            setEdges(flowEdges);
        } catch (error) {
            console.error('Error creating tree visualization:', error);
        }
    }, [currentMessages, conversationId, updateKey]);

    if (!currentMessages.length) {
        return (
            <div className="text-gray-400 text-center p-4 bg-gray-900 rounded-md shadow-inner mx-auto my-8 max-w-md border border-gray-800">
                <p>No conversation history to display</p>
                <p className="text-sm mt-2 text-gray-500">Start a new conversation to see the tree view</p>
            </div>
        );
    }

    return (
        <div className="flex flex-col h-[calc(100vh-70px)] w-full">
            <div className={cn(
                "flex-1 overflow-hidden",
                !currentMessages.length && "flex items-center justify-center"
            )}>
                <ReactFlowProvider>
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
                        key={`flow-${conversationId}-${updateKey}`}
                    >
                        <Controls />
                    </ReactFlow>
                </ReactFlowProvider>
            </div>
        </div>
    );
};

// Helper function that works directly with flat message arrays
function transformToReactFlow(messages: Message[], graph: MessageGraph): { nodes: Node[]; edges: Edge[] } {
    const nodes: Node[] = [];
    const edges: Edge[] = [];

    // Get root messages to establish top level
    const rootMessages = graph.getRootMessages();

    // Used to track node positions
    const levelDepths: Map<string, number> = new Map();
    const horizontalPositions: Map<string, number> = new Map();

    // Calculate depth for each message using graph traversal
    function calculateDepths() {
        // Set depth 0 for root messages
        rootMessages.forEach(root => {
            levelDepths.set(root.id, 0);
        });

        // Process each message - if parent has depth, child has depth+1
        let changed = true;
        while (changed) {
            changed = false;
            messages.forEach(message => {
                // Skip if no parent or depth already set
                if (!message.parentId || levelDepths.has(message.id)) return;

                // If parent has depth, set child depth
                const parentDepth = levelDepths.get(message.parentId);
                if (parentDepth !== undefined) {
                    levelDepths.set(message.id, parentDepth + 1);
                    changed = true;
                }
            });
        }
    }

    // Assign horizontal positions to prevent overlaps
    function assignHorizontalPositions() {
        // Group messages by depth
        const messagesByDepth: Map<number, Message[]> = new Map();

        messages.forEach(message => {
            const depth = levelDepths.get(message.id);
            if (depth === undefined) return;

            if (!messagesByDepth.has(depth)) {
                messagesByDepth.set(depth, []);
            }
            messagesByDepth.get(depth)?.push(message);
        });

        // For each depth level, assign horizontal positions
        const horizontalSpacing = 200;

        messagesByDepth.forEach((messagesAtDepth, depth) => {
            const levelWidth = messagesAtDepth.length * horizontalSpacing;
            const startX = -levelWidth / 2;

            messagesAtDepth.forEach((message, index) => {
                horizontalPositions.set(message.id, startX + (index + 0.5) * horizontalSpacing);
            });
        });
    }

    // Calculate positions
    calculateDepths();
    assignHorizontalPositions();

    // Create ReactFlow nodes
    const verticalSpacing = 150;

    messages.forEach(message => {
        const depth = levelDepths.get(message.id);
        const horizontalPos = horizontalPositions.get(message.id);

        if (depth === undefined || horizontalPos === undefined) {
            console.warn(`Missing position data for message ${message.id}`);
            return;
        }

        // Determine if user or AI message based on the source
        const isUserMessage = message.source === 'user';
        const isSystemMessage = message.source === 'system';

        // Create the node with appropriate styling
        const newNode: Node = {
            id: message.id,
            type: 'default',
            position: {
                x: horizontalPos,
                y: depth * verticalSpacing
            },
            data: {
                label: (
                    <div className="flex flex-col gap-1 max-w-[150px]">
                        <div className="text-xs font-semibold">
                            {isUserMessage ? 'You' : isSystemMessage ? 'System' : 'AI'}
                        </div>
                        <div className="text-sm truncate">
                            {message.content?.substring(0, 30) + (message.content?.length > 30 ? '...' : '')}
                        </div>
                    </div>
                )
            },
            style: {
                background: isUserMessage ? '#1e40af' : isSystemMessage ? '#4B5563' : '#4f46e5',
                color: '#ffffff',
                border: isUserMessage ? '1px solid #1e3a8a' :
                    isSystemMessage ? '1px solid #374151' : '1px solid #4338ca',
                borderRadius: '10px',
                padding: '12px',
                width: '180px',
                boxShadow: '0 4px 6px -1px rgba(0, 0, 0, 0.1), 0 2px 4px -1px rgba(0, 0, 0, 0.06)',
                fontSize: '0.9rem',
            },
            sourcePosition: Position.Bottom,
            targetPosition: Position.Top
        };

        nodes.push(newNode);
    });

    // Create edges based on parent-child relationships
    messages.forEach(message => {
        if (message.parentId) {
            const edge: Edge = {
                id: `${message.parentId}-${message.id}`,
                source: message.parentId,
                target: message.id,
                type: 'smoothstep',
                animated: true,
                style: { stroke: '#6b7280', strokeWidth: 2 },
                markerEnd: {
                    type: MarkerType.ArrowClosed,
                    width: 15,
                    height: 15,
                    color: '#6b7280',
                },
            };

            edges.push(edge);
        }
    });

    return { nodes, edges };
}