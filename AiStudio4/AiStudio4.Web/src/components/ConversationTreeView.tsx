import React from 'react';
import { Button } from '@/components/ui/button';
import { ChevronLeft } from 'lucide-react';
import * as d3 from 'd3';

interface TreeViewProps {
    onClose: () => void;
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
}
export const ConversationTreeView: React.FC<TreeViewProps> = ({ onClose, conversationId, messages }) => {
    const treeContainerRef = React.useRef<HTMLDivElement>(null);

    React.useEffect(() => {
        if (!treeContainerRef.current || !messages) return;

        // Clear previous content
        d3.select(treeContainerRef.current).selectAll('*').remove();

        // Set up dimensions
        const width = 280;
        const height = 500;
        const margin = { top: 20, right: 40, bottom: 20, left: 40 };
        console.log('Container dimensions:', { width, height, margin });

        // Create SVG with a group for transformation
        const svg = d3.select(treeContainerRef.current)
            .append('svg')
            .attr('width', width)
            .attr('height', height)
            .append('g')
            .attr('transform', `translate(${margin.left},${margin.top})`); // Position the tree with left margin

        try {
            // Create hierarchy directly from messages object
            console.log('Messages object received:', JSON.stringify(messages, null, 2));
            // Normalize the data structure
            const normalizeData = (node: any) => {
                if (!node) return null;
                
                let children = [];
                if (node.children) {
                    // If children is an array, process each child
                    if (Array.isArray(node.children)) {
                        children = node.children.map((child: any) => normalizeData(child)).filter(Boolean);
                    } 
                    // If children is a single object (not an array), process it as a single child
                    else if (typeof node.children === 'object' && node.children.id) {
                        const normalizedChild = normalizeData(node.children);
                        if (normalizedChild) {
                            children = [normalizedChild];
                        }
                    }
                }
                
                return {
                    id: node.id || 'unknown',
                    text: node.text || '',
                    children: children
                };
            };
            const normalizedData = normalizeData(messages);
            console.log('Raw messages:', messages);
            console.log('Normalized data:', JSON.stringify(normalizedData, null, 2));
            const hierarchyData = d3.hierarchy(normalizedData);
            console.log('D3 hierarchy data:', hierarchyData);
            console.log('Number of children:', hierarchyData.children?.length);
            console.log('All descendants:', hierarchyData.descendants().length);
            console.log('Tree structure:', hierarchyData.descendants().map(d => ({ 
                depth: d.depth,
                data: d.data,
                hasChildren: d.children?.length > 0
            })));

            // Create tree layout
            const treeLayout = d3.tree()
                .size([height - margin.top - margin.bottom, width - margin.left - margin.right])
                .separation((a, b) => a.parent === b.parent ? 1 : 1.5);

            // Process the data
            const root = treeLayout(hierarchyData);
            console.log('Tree layout root:', root);

            // Normalize for fixed-depth
            root.descendants().forEach((d: any) => {
                d.y = d.depth * 60; // Reduced spacing between levels
                console.log(`Node position:`, { 
                    id: d.data.id,
                    text: d.data.text,
                    x: d.x,
                    y: d.y,
                    depth: d.depth
                });
            });

            // Rest of the visualization code remains the same...
            // Add links
            const link = svg.selectAll('path.link')
                .data(root.links())
                .enter()
                .append('path')
                .attr('class', 'link')
                .attr('fill', 'none')
                .attr('stroke', '#4b5563')
                .attr('d', d3.linkHorizontal()
                    .x((d: any) => d.y)
                    .y((d: any) => d.x));

            const node = svg.selectAll('g.node')
                .data(root.descendants())
                .enter()
                .append('g')
                .attr('class', 'node')
                .attr('transform', (d: any) => `translate(${d.y},${d.x})`); // Swap x and y for horizontal layout

            // Add circles for nodes
            node.append('circle')
                .attr('r', 5)
                .attr('fill', '#3b82f6')
                .attr('cursor', 'pointer')
                .on('click', (event, d: any) => {
                    console.log('Node clicked:', d.data);
                });

            // Add text labels
            node.append('text')
                .attr('dy', '0.31em')
                .attr('x', 8)
                .attr('text-anchor', 'start')
                .text((d: any) => {
                    const text = d.data.text || '';
                    return text.length > 20 ? text.substring(0, 20) + '...' : text;
                })
                .attr('font-size', '10px')
                .attr('fill', '#e5e7eb')
                .each(function(this: SVGTextElement) {
                    // Wrap text if too long
                    const text = d3.select(this);
                    const words = text.text().split(/\s+/);
                    const maxWidth = 100;
                    let line = [];
                    let lineNumber = 0;
                    const lineHeight = 1.1;
                    const y = text.attr('y');
                    const dy = parseFloat(text.attr('dy'));
                    let tspan = text.text(null).append('tspan').attr('x', 8).attr('y', y).attr('dy', dy + 'em');

                    words.forEach(word => {
                        line.push(word);
                        tspan.text(line.join(' '));
                        if ((tspan.node()?.getComputedTextLength() || 0) > maxWidth) {
                            line.pop();
                            tspan.text(line.join(' '));
                            line = [word];
                            tspan = text.append('tspan')
                                .attr('x', 8)
                                .attr('y', y)
                                .attr('dy', ++lineNumber * lineHeight + dy + 'em')
                                .text(word);
                        }
                    });
                });

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
                {!messages && (
                    <div className="text-gray-400 text-center">No messages to display</div>
                )}
            </div>
        </div>
    );
};