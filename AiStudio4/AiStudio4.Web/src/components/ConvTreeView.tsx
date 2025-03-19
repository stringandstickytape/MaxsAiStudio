
import React, { useState, useEffect, useRef, useMemo, useCallback } from 'react';
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
  timestamp?: number;
  durationMs?: number;
}

export const ConvTreeView: React.FC<TreeViewProps> = ({ convId, messages }) => {
  const [updateKey, setUpdateKey] = useState(0);
  const { setActiveConv, convs } = useConvStore();
  const svgRef = useRef<SVGSVGElement>(null);
  const containerRef = useRef<HTMLDivElement>(null);
  const zoomRef = useRef<d3.ZoomBehavior<SVGSVGElement, unknown> | null>(null);

  
  useEffect(() => {
    setUpdateKey((prev) => prev + 1);
  }, [convId]);

  
  const hierarchicalData = useMemo(() => {
    if (!messages || messages.length === 0) return null;

    try {
      
      const graph = new MessageGraph(messages);

      
      const rootMessages = graph.getRootMessages();
      if (rootMessages.length === 0) return null;

      
      const rootMessage = rootMessages[0];

      
      const buildTree = (message: Message, depth: number = 0): TreeNode => {
        const node: TreeNode = {
          id: message.id,
          content: message.content,
          source: message.source,
          children: [],
          parentId: message.parentId,
          depth: depth,
          timestamp: message.timestamp,
          durationMs: message.durationMs,
        };

        
        const childMessages = graph.getChildren(message.id);
        node.children = childMessages.map((child) => buildTree(child, depth + 1));

        return node;
      };

      
      return buildTree(rootMessage);
    } catch (error) {
      console.error('Error creating tree data:', error);
      return null;
    }
  }, [messages, updateKey]);

  
    const handleNodeClick = (nodeId: string, nodeSource: string, nodeContent: string) => {
        console.log('Tree Node clicked:', {
            node: nodeId,
            convId: convId,
            source: nodeSource
        });

        if (nodeSource === 'user') {
            window.setPrompt(nodeContent);

            const conv = convs[convId];
            if (conv) {
                const message = conv.messages.find(msg => msg.id === nodeId);
                if (message && message.parentId) {
                    setActiveConv({
                        convId: convId,
                        slctdMsgId: message.parentId,
                    });
                    return;
                }
            }
        } else {

            window.setPrompt("");
        }

        setActiveConv({
            convId: convId,
            slctdMsgId: nodeId,
        });
    };

    const handleFocusOnLatest = useCallback(() => {
        if (svgRef.current && zoomRef.current && containerRef.current && messages.length > 0) {
            // Get the most recent message
            const latestMessage = messages.reduce((latest, current) => {
                // Compare message timestamps or IDs depending on what's available
                // If you have timestamps, use those for comparison
                return !latest || (current.timestamp && latest.timestamp && current.timestamp > latest.timestamp) ? current : latest;
            }, messages[0]);

            if (latestMessage) {
                // Find the node in the D3 visualization
                const svg = d3.select(svgRef.current);
                const nodes = svg.selectAll('.node');

                // Find the node that corresponds to the latest message
                const latestNode = nodes.filter((d: any) => d.data.id === latestMessage.id);

                if (!latestNode.empty()) {
                    const nodeData = latestNode.datum() as any;
                    const containerWidth = containerRef.current.clientWidth;

                    const containerHeight = containerRef.current.clientHeight;
                    // Calculate the center position of the viewport
                    svg.transition()
                        .duration(750)
                        .call(
                            zoomRef.current.transform,
                            d3.zoomIdentity
                                .translate(containerWidth / 2, containerHeight / 2)
                                .scale(1.2)
                                .translate(-nodeData.x, -nodeData.y)
                        );
                }
            }
        }
    }, [messages]);

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

      
      const svg = d3.select(svgRef.current);
      const rootNode = svg.select('.node').datum() as any;

        if (rootNode) {
            
            const centerX = containerWidth / 2;

            
            svg.transition().call(zoomRef.current.transform, d3.zoomIdentity.translate(centerX, 50));
      } else {
        
        svg.transition().call(zoomRef.current.transform, d3.zoomIdentity.translate(containerWidth / 2, 50));
      }
    }
  };

  
  useEffect(() => {
    if (!svgRef.current || !containerRef.current || !hierarchicalData) return;

    
    d3.select(svgRef.current).selectAll('*').remove();

    
    const containerWidth = containerRef.current.clientWidth;
    const containerHeight = containerRef.current.clientHeight || 600;

    
    const svg = d3.select(svgRef.current).attr('width', containerWidth).attr('height', containerHeight);

    
    const g = svg.append('g');

    
    // Adjust node size for better fit in smaller sidebar space
    const nodeSizeWidth = containerWidth < 400 ? 120 : 135;
    const nodeSizeHeight = containerWidth < 400 ? 100 : 130;
    const treeLayout = d3.tree<TreeNode>().size([containerWidth - 80, containerHeight - 120]).nodeSize([nodeSizeWidth, nodeSizeHeight]);

    
    const root = d3.hierarchy(hierarchicalData);

    
    const treeData = treeLayout(root);

    
    const zoom = d3
      .zoom<SVGSVGElement, unknown>()
      .scaleExtent([0.1, 3])
      .on('zoom', (event) => {
        g.attr('transform', event.transform);
      });

    
    zoomRef.current = zoom;

    svg.call(zoom);

    
      const rootX = treeData.x;
      const centerX = containerWidth / 2;
      svg.call(zoom.transform, d3.zoomIdentity.translate(centerX, 50));

    
    g.selectAll('.link')
      .data(treeData.links())
      .enter()
      .append('path')
      .attr('class', 'link')
      .attr(
        'd',
        d3
          .linkVertical<d3.HierarchyPointLink<TreeNode>, d3.HierarchyPointNode<TreeNode>>()
          .x((d) => d.x) 
          .y((d) => d.y),
      )
      .attr('fill', 'none')
      .attr('stroke', '#6b7280')
      .attr('stroke-width', 2)
      .attr('stroke-opacity', 0.6);

    
    const nodeGroups = g
      .selectAll('.node')
      .data(treeData.descendants())
      .enter()
      .append('g')
      .attr('class', 'node')
      .attr('transform', (d) => `translate(${d.x},${d.y})`) 
      .attr('cursor', 'pointer')
      .on('click', (_, d) => handleNodeClick(d.data.id, d.data.source, d.data.content));

    
    nodeGroups
      .append('rect')
      .attr('width', containerWidth < 400 ? 200 : 240)
      .attr('height', containerWidth < 400 ? 80 : 100)  
      .attr('x', containerWidth < 400 ? -100 : -120)
      .attr('y', -40)
      .attr('rx', 10)
      .attr('ry', 10)
      .attr('fill', (d) => {
        const source = d.data.source;
        if (source === 'user') return '#1e40af'; 
        if (source === 'system') return '#4B5563'; 
        return '#4f46e5'; 
      })
      .attr('stroke', (d) => {
        const source = d.data.source;
        if (source === 'user') return '#1e3a8a';
        if (source === 'system') return '#374151';
        return '#4338ca';
      })
      .attr('stroke-width', 1);

    
    
    const nodeLabels = nodeGroups.append('g');

    
    nodeLabels
      .append('text')
      .attr('x', -110)
      .attr('y', -25)
      .attr('font-size', containerWidth < 400 ? '9px' : '10px')
      .attr('font-weight', 'bold')
      .attr('fill', 'white')
      .text((d) => {
        const source = d.data.source;
        if (source === 'user') return 'You';
        if (source === 'system') return 'System';
        return 'AI';
      });

    
    nodeLabels
      .append('foreignObject')
      .attr('x', -110)
      .attr('y', -20)  
      .attr('width', containerWidth < 400 ? 180 : 220)
      .attr('height', containerWidth < 400 ? 60 : 75)
      .append('xhtml:div')
      .style('color', 'white')
      .style('font-size', '10px')
      .style('overflow', 'hidden')
      .style('text-overflow', 'ellipsis')
      .style('display', '-webkit-box')
      .style('-webkit-line-clamp', containerWidth < 400 ? '3' : '5')
      .style('-webkit-box-orient', 'vertical')
      .style('word-wrap', 'break-word')
      .style('padding', '0 2px')  
      .html((d) => {
        const content = d.data.content || '';
        
        return content
          .replace(/&/g, '&amp;')
          .replace(/</g, '&lt;')
          .replace(/>/g, '&gt;')
          .replace(/\"/g, '&quot;')
          .replace(/'/g, '&#039;');
      });

    return () => {
      
      d3.select(svgRef.current).selectAll('*').remove();
    };
  }, [hierarchicalData, convId]);
  
  // Auto-focus on latest message when a new message is added
  useEffect(() => {
    // We need a slight delay to let the tree render after message changes
    const timer = setTimeout(() => {
      if (messages.length > 0) {
        handleFocusOnLatest();
      }
    }, 300);
    
    return () => clearTimeout(timer);
  }, [messages.length]);

  if (!messages.length) {
    return (
      <div className="text-gray-400 text-center p-4 bg-gray-900 rounded-md shadow-inner mx-auto my-8 max-w-md border border-gray-800">
        <p>No conv history to display</p>
        <p className="text-sm mt-2 text-gray-500">Start a new conv to see the tree view</p>
      </div>
    );
  }

  return (
    <div className="flex flex-col h-full w-full">
      <div
        className={cn('flex-1 relative', !messages.length && 'flex items-center justify-center')}
        ref={containerRef}
        style={{  height: '100%' }}
      >
        <svg ref={svgRef} className="w-full h-full bg-[#111827]" key={`tree-${convId}-${updateKey}`}  />

        
              <div className="absolute bottom-4 right-4 flex flex-col gap-2">
                  <button
                      onClick={handleFocusOnLatest}
                      className="bg-gray-800 hover:bg-gray-700 text-white p-2 rounded-full shadow-lg"
                      title="Focus on Latest Message"
                  >
                      <svg xmlns="http://www.w3.org/2000/svg" className="h-5 w-5" viewBox="0 0 20 20" fill="currentColor">
                          <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm1-11a1 1 0 10-2 0v3.586L7.707 9.293a1 1 0 00-1.414 1.414l3 3a1 1 0 001.414 0l3-3a1 1 0 10-1.414-1.414L11 10.586V7z" clipRule="evenodd" />
                      </svg>
                  </button>

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