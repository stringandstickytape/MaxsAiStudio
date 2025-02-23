import React from 'react';
import { Button } from '@/components/ui/button';
import { ChevronLeft } from 'lucide-react';
import * as d3 from 'd3';

interface TreeViewProps {
    onClose: () => void;
    conversationId: string;
    messages: any[];
}

export const ConversationTreeView: React.FC<TreeViewProps> = ({ onClose, conversationId, messages }) => {
    const treeContainerRef = React.useRef<HTMLDivElement>(null);

    React.useEffect(() => {
        console.log('Messages:', messages);
        if (!treeContainerRef.current || !messages.length) return;

        // First ensure every message has a valid parentId, using 'root' for top-level messages
        const processedMessages = messages.map(msg => {
            const parentId = msg.parentId || 'root'; // If no parent, connect to root
            return {
                id: msg.id,
                parentId,
                content: msg.content || '',
                source: msg.source,
                timestamp: msg.timestamp
            };
        });

        // Create artificial root node that will be parent to all top-level messages
        const rootNode = {
            id: 'root',
            parentId: null,
            content: '',
            source: 'system',
            timestamp: 0
        };

        // Clear previous content
        d3.select(treeContainerRef.current).selectAll('*').remove();

        // Set up dimensions
        const width = 280;
        const height = 500;
        const margin = { top: 20, right: 20, bottom: 20, left: 20 };

        // Create SVG
        const svg = d3.select(treeContainerRef.current)
            .append('svg')
            .attr('width', width)
            .attr('height', height);

        // Debug logging
        console.log('Messages before processing:', messages.map(m => ({id: m.id, parentId: m.parentId})));
        
        try {
            console.log('Tree data:', {
                rootNode,
                processedMessages,
                parentChildPairs: processedMessages.map(m => `${m.id} -> ${m.parentId}`)
            });
            // Create data array with root node and messages
            const treeData = [rootNode, ...processedMessages];
            console.log('Tree data array:', treeData);

            // Create hierarchy with explicit root handling
            const hierarchyData = d3.stratify()
                .id((d: any) => d.id)
                .parentId((d: any) => {
                    if (d.id === 'root') return null;
                    return d.parentId;
                })
                (treeData);
            console.log('Hierarchy data:', hierarchyData);

            // Create tree layout
            const treeLayout = d3.tree()
                .nodeSize([30, 100])
                .separation((a, b) => a.parent === b.parent ? 1 : 2);

            const root = treeLayout(hierarchyData);

            // Add links
            svg.append('g')
                .attr('fill', 'none')
                .attr('stroke', '#4b5563')
                .selectAll('path')
                .data(root.links())
                .join('path')
                .attr('d', d3.linkVertical()
                    .x((d: any) => d.x)
                    .y((d: any) => d.y));

            // Add nodes
            const nodes = svg.append('g')
                .selectAll('circle')
                .data(root.descendants())
                .join('circle')
                .attr('cx', (d: any) => d.x)
                .attr('cy', (d: any) => d.y)
                .attr('r', 5)
                .attr('fill', '#3b82f6')
                .attr('cursor', 'pointer')
                .on('click', (event, d: any) => {
                    // Handle node click - could dispatch an action to focus on this message
                    console.log('Node clicked:', d.data);
                });

            // Add labels
            svg.append('g')
                .selectAll('text')
                .data(root.descendants())
                .join('text')
                .attr('x', (d: any) => d.x + 8)
                .attr('y', (d: any) => d.y)
                .text((d: any) => d.data.id === 'root' ? '' : d.data.content.substring(0, 20) + '...')
                .attr('font-size', '10px')
                .attr('fill', '#e5e7eb');

        } catch (error) {
            console.error('Error creating tree visualization:', error);
            if (treeContainerRef.current) {
                const errorDiv = document.createElement('div');
                errorDiv.className = 'text-red-500 text-center';
                errorDiv.textContent = 'Error creating visualization';
                treeContainerRef.current.appendChild(errorDiv);
            }
        }

    }, [messages]);

    return (
        <div className="fixed right-0 top-0 h-screen w-[280px] bg-[#1f2937] border-l border-gray-700 shadow-lg transform transition-transform duration-300 z-40">
            <div className="p-4 border-b border-gray-700 flex items-center">
                <Button
                    variant="ghost"
                    size="icon"
                    onClick={onClose}
                    className="mr-2"
                >
                    <ChevronLeft className="h-4 w-4" />
                </Button>
                <h2 className="text-gray-100 text-lg font-semibold">Conversation Tree</h2>
            </div>
            <div ref={treeContainerRef} className="p-4">
                {messages.length === 0 && (
                    <div className="text-gray-400 text-center">No messages to display</div>
                )}
            </div>
        </div>
    );
};