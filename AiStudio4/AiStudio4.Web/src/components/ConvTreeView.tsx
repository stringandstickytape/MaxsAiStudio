// src/components/ConvTreeView.tsx
import React, { useState, useEffect, useRef, useMemo } from 'react';
import * as d3 from 'd3';
import { cn } from '@/lib/utils';
import { Message } from '@/types/conv';
import { MessageGraph } from '@/utils/messageGraph';
import { useConvStore } from '@/stores/useConvStore';

interface TreeViewProps {
  convId: string;
  messages: Message[];
}

interface TreeNode {
  id: string;
  content: string;
  source: string;
  children: TreeNode[];
  parentId?: string;
  depth?: number;
  x?: number;
  y?: number;
}

export const ConvTreeView: React.FC<TreeViewProps> = ({ convId, messages }) => {
  const [updateKey, setUpdateKey] = useState(0);
  const { setActiveConv } = useConvStore();
  const svgRef = useRef<SVGSVGElement>(null);
  const containerRef = useRef<HTMLDivElement>(null);
  const zoomRef = useRef<d3.ZoomBehavior<SVGSVGElement, unknown> | null>(null);

  // Force a refresh when convId changes
  useEffect(() => {
    setUpdateKey((prev) => prev + 1);
  }, [convId]);

  // Process the message data into a hierarchical structure for D3
  const hierarchicalData = useMemo(() => {
    if (!messages || messages.length === 0) return null;

    try {
      // Create a message graph from the messages
      const graph = new MessageGraph(messages);

      // Get root messages
      const rootMessages = graph.getRootMessages();
      if (rootMessages.length === 0) return null;

      // Start with the first root message
      const rootMessage = rootMessages[0];

      // Create a recursive function to build the tree
      const buildTree = (message: Message, depth: number = 0): TreeNode => {
        const node: TreeNode = {
          id: message.id,
          content: message.content,
          source: message.source,
          children: [],
          parentId: message.parentId,
          depth: depth,
        };

        // Get children from the graph
        const childMessages = graph.getChildren(message.id);
        node.children = childMessages.map((child) => buildTree(child, depth + 1));

        return node;
      };

      // Build the tree starting from the root
      return buildTree(rootMessage);
    } catch (error) {
      console.error('Error creating tree data:', error);
      return null;
    }
  }, [messages, updateKey]);

  // Handle node click
  const handleNodeClick = (nodeId: string) => {
    console.log('Tree Node clicked:', {
      node: nodeId,
      convId: convId,
    });
    setActiveConv({
      convId: convId,
      slctdMsgId: nodeId,
    });
  };

  // Handle zoom controls
  const handleZoomIn = () => {
    if (svgRef.current && zoomRef.current) {
      d3.select(svgRef.current).transition().call(zoomRef.current.scaleBy, 1.3);
    }
  };

  const handleZoomOut = () => {
    if (svgRef.current && zoomRef.current) {
      d3.select(svgRef.current).transition().call(zoomRef.current.scaleBy, 0.7);
    }
  };

  const handleCenter = () => {
    if (svgRef.current && zoomRef.current && containerRef.current) {
      const containerWidth = containerRef.current.clientWidth;

      // Get the root node's position from the current tree data
      const svg = d3.select(svgRef.current);
      const rootNode = svg.select('.node').datum() as any;

      if (rootNode) {
        // Calculate the center offset based on the root node position
        const rootX = rootNode.x || containerWidth / 2;
        const centerX = containerWidth / 2 - rootX;

        // Apply the transform with the calculated offset
        svg.transition().call(zoomRef.current.transform, d3.zoomIdentity.translate(centerX, 50));
      } else {
        // Fallback if we can't find the root node
        svg.transition().call(zoomRef.current.transform, d3.zoomIdentity.translate(containerWidth / 2, 50));
      }
    }
  };

  // Render D3 visualization
  useEffect(() => {
    if (!svgRef.current || !containerRef.current || !hierarchicalData) return;

    // Clear previous visualization
    d3.select(svgRef.current).selectAll('*').remove();

    // Get container dimensions
    const containerWidth = containerRef.current.clientWidth;
    const containerHeight = containerRef.current.clientHeight || 600;

    // Create SVG element
    const svg = d3.select(svgRef.current).attr('width', containerWidth).attr('height', containerHeight);

    // Create a group for the tree
    const g = svg.append('g');

    // Create a tree layout
    const treeLayout = d3.tree<TreeNode>().size([containerWidth - 100, containerHeight - 150]);

    // Create hierarchy from data
    const root = d3.hierarchy(hierarchicalData);

    // Compute the tree layout
    const treeData = treeLayout(root);

    // Add zoom behavior
    const zoom = d3
      .zoom<SVGSVGElement, unknown>()
      .scaleExtent([0.5, 3])
      .on('zoom', (event) => {
        g.attr('transform', event.transform);
      });

    // Store zoom reference for external controls
    zoomRef.current = zoom;

    svg.call(zoom);

    // Center the tree initially
    const rootX = treeData.x || containerWidth / 2;
    const centerX = containerWidth / 2 - rootX;
    svg.call(zoom.transform, d3.zoomIdentity.translate(centerX, 50));

    // Create links
    g.selectAll('.link')
      .data(treeData.links())
      .enter()
      .append('path')
      .attr('class', 'link')
      .attr(
        'd',
        d3
          .linkVertical<d3.HierarchyPointLink<TreeNode>, d3.HierarchyPointNode<TreeNode>>()
          .x((d) => d.x) // Use standard x and y for vertical layout
          .y((d) => d.y),
      )
      .attr('fill', 'none')
      .attr('stroke', '#6b7280')
      .attr('stroke-width', 2)
      .attr('stroke-opacity', 0.6);

    // Create node groups
    const nodeGroups = g
      .selectAll('.node')
      .data(treeData.descendants())
      .enter()
      .append('g')
      .attr('class', 'node')
      .attr('transform', (d) => `translate(${d.x},${d.y})`) // Standard coordinates for vertical layout
      .attr('cursor', 'pointer')
      .on('click', (_, d) => handleNodeClick(d.data.id));

    // Add node rectangles
    nodeGroups
      .append('rect')
      .attr('width', 200)
      .attr('height', 70)
      .attr('x', -100)
      .attr('y', -35)
      .attr('rx', 10)
      .attr('ry', 10)
      .attr('fill', (d) => {
        const source = d.data.source;
        if (source === 'user') return '#1e40af'; // User blue
        if (source === 'system') return '#4B5563'; // System gray
        return '#4f46e5'; // AI purple
      })
      .attr('stroke', (d) => {
        const source = d.data.source;
        if (source === 'user') return '#1e3a8a';
        if (source === 'system') return '#374151';
        return '#4338ca';
      })
      .attr('stroke-width', 1);

    // Add node labels
    const nodeLabels = nodeGroups.append('g').attr('transform', 'translate(-90, -20)');

    // Add message source label
    nodeLabels
      .append('text')
      .attr('dy', '0.5em')
      .attr('font-size', '10px')
      .attr('font-weight', 'bold')
      .attr('fill', 'white')
      .text((d) => {
        const source = d.data.source;
        if (source === 'user') return 'You';
        if (source === 'system') return 'System';
        return 'AI';
      });

    // Add message content preview
    nodeLabels
      .append('text')
      .attr('dy', '2em')
      .attr('font-size', '10px')
      .attr('fill', 'white')
      .text((d) => {
        // Truncate content
        const content = d.data.content || '';
        return content.length > 30 ? content.substring(0, 30) + '...' : content;
      });

    return () => {
      // Cleanup
      d3.select(svgRef.current).selectAll('*').remove();
    };
  }, [hierarchicalData, convId, handleNodeClick]);

  if (!messages.length) {
    return (
      <div className="text-gray-400 text-center p-4 bg-gray-900 rounded-md shadow-inner mx-auto my-8 max-w-md border border-gray-800">
        <p>No conv history to display</p>
        <p className="text-sm mt-2 text-gray-500">Start a new conv to see the tree view</p>
      </div>
    );
  }

  return (
    <div className="flex flex-col h-[calc(100vh-70px)] w-full">
      <div
        className={cn('flex-1 overflow-hidden relative', !messages.length && 'flex items-center justify-center')}
        ref={containerRef}
      >
        <svg ref={svgRef} className="w-full h-full bg-[#111827]" key={`tree-${convId}-${updateKey}`} />

        {/* Zoom Controls */}
        <div className="absolute bottom-4 right-4 flex flex-col gap-2">
          <button
            onClick={handleCenter}
            className="bg-gray-800 hover:bg-gray-700 text-white p-2 rounded-full shadow-lg"
            title="Center View"
          >
            <svg xmlns="http://www.w3.org/2000/svg" className="h-5 w-5" viewBox="0 0 20 20" fill="currentColor">
              <path
                fillRule="evenodd"
                d="M10 18a8 8 0 100-16 8 8 0 000 16zm0-2a6 6 0 100-12 6 6 0 000 12zm0-8a2 2 0 11-4 0 2 2 0 014 0z"
                clipRule="evenodd"
              />
            </svg>
          </button>
          <button
            onClick={handleZoomIn}
            className="bg-gray-800 hover:bg-gray-700 text-white p-2 rounded-full shadow-lg"
            title="Zoom In"
          >
            <svg xmlns="http://www.w3.org/2000/svg" className="h-5 w-5" viewBox="0 0 20 20" fill="currentColor">
              <path
                fillRule="evenodd"
                d="M10 5a1 1 0 011 1v3h3a1 1 0 110 2h-3v3a1 1 0 11-2 0v-3H6a1 1 0 110-2h3V6a1 1 0 011-1z"
                clipRule="evenodd"
              />
            </svg>
          </button>
          <button
            onClick={handleZoomOut}
            className="bg-gray-800 hover:bg-gray-700 text-white p-2 rounded-full shadow-lg"
            title="Zoom Out"
          >
            <svg xmlns="http://www.w3.org/2000/svg" className="h-5 w-5" viewBox="0 0 20 20" fill="currentColor">
              <path fillRule="evenodd" d="M5 10a1 1 0 011-1h8a1 1 0 110 2H6a1 1 0 01-1-1z" clipRule="evenodd" />
            </svg>
          </button>
        </div>
      </div>
    </div>
  );
};
