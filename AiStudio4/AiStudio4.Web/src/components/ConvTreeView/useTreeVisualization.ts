// AiStudio4.Web\src\components\ConvTreeView\useTreeVisualization.ts
import { useEffect, useRef, RefObject, useCallback, useState } from 'react';
import * as d3 from 'd3';
import { TreeNode } from './types';
import { getModelFriendlyName } from '@/utils/modelUtils';

interface UseTreeVisualizationParams {
  svgRef: RefObject<SVGSVGElement>;
  containerRef: RefObject<HTMLDivElement>;
  hierarchicalData: TreeNode | null;
  onNodeClick: (nodeId: string, nodeSource: string, nodeContent: string) => void;
  onNodeMiddleClick: (event: any, nodeId: string) => void;
  updateKey: number;
  selectedMessageId?: string | null;
}

export const useTreeVisualization = ({
  svgRef,
  containerRef,
  hierarchicalData,
  onNodeClick,
  onNodeMiddleClick,
  updateKey,
  selectedMessageId
}: UseTreeVisualizationParams) => {
  // Ref to store the zoom behavior
  const zoomRef = useRef<d3.ZoomBehavior<SVGSVGElement, unknown> | null>(null);

  // Setup zoom and pan handlers
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
      const containerHeight = containerRef.current.clientHeight;
      const svg = d3.select(svgRef.current);
      const nodes = svg.selectAll('.node');
      
      if (nodes.size() > 0) {
        // Get all nodes data to calculate bounds
        const nodesData = nodes.data() as any[];
        
        // Always use all nodes to show the entire tree
        const nodesToUse = nodesData;
        
        // Calculate bounds of the selected nodes
        let minX = Infinity, maxX = -Infinity, minY = Infinity, maxY = -Infinity;
        
        nodesToUse.forEach((d: any) => {
          // Account for node width and height
          const nodeWidth = containerWidth < 400 ? 200 : 240;
          const nodeHeight = containerWidth < 400 ? 85 : 110;
          
          const left = d.x - (nodeWidth / 2);
          const right = d.x + (nodeWidth / 2);
          const top = d.y - (nodeHeight / 2);
          const bottom = d.y + (nodeHeight / 2);
          
          minX = Math.min(minX, left);
          maxX = Math.max(maxX, right);
          minY = Math.min(minY, top);
          maxY = Math.max(maxY, bottom);
        });
        
        // Add padding
        const padding = 50;
        minX -= padding;
        maxX += padding;
        minY -= padding;
        maxY += padding;
        
        // Calculate scale to fit the nodes
        const width = maxX - minX;
        const height = maxY - minY;
        const scaleX = containerWidth / width;
        const scaleY = containerHeight / height;
        const scale = Math.min(scaleX, scaleY, 1.5); // Limit max zoom
        
        // Calculate center point of the bounds
        const centerX = (minX + maxX) / 2;
        const centerY = (minY + maxY) / 2;
        
        // Apply transform to center and scale the view
        svg.call(
          zoomRef.current.transform,
          d3.zoomIdentity
            .translate(containerWidth / 2, containerHeight / 2)
            .scale(scale)
            .translate(-centerX, -centerY)
        );
      } else {
        // Fallback if no nodes found
        svg.call(
          zoomRef.current.transform,
          d3.zoomIdentity.translate(containerWidth / 2, containerHeight / 2).scale(1)
        );
      }
    }
  };

  const handleFocusOnLatest = (messages: any[]) => {
    if (svgRef.current && zoomRef.current && containerRef.current && messages.length > 0) {
      // Get the most recent message
      const latestMessage = messages.reduce((latest, current) => {
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
          svg.call(
              zoomRef.current.transform,
              d3.zoomIdentity
                .translate(containerWidth / 2, containerHeight / 2)
                .scale(1.2)
                .translate(-nodeData.x, -nodeData.y)
            );
        }
      }
    }
  };

  // D3 visualization effect
    useEffect(() => {
    if (!svgRef.current || !containerRef.current || !hierarchicalData) return;
    
    // Get theme values from CSS variables
    const getThemeColor = (varName: string, fallback: string) => {
      // Try to get the CSS variable from the ConvTreeView container first
      let value = '';
      if (containerRef.current && document.body.contains(containerRef.current)) {
        value = getComputedStyle(containerRef.current).getPropertyValue(varName);
      }
      // Fallback to :root if not found or empty
      if (!value || value.trim() === '') {
        value = getComputedStyle(document.documentElement).getPropertyValue(varName);
      }
      return value && value.trim() !== '' ? value : fallback;
    };

    // Theme colors for tree elements
    const linkColor = getThemeColor('--convtree-link-color', '#6b7280');
    
    // Map ConvView theme values to tree nodes
    const userNodeColor = getThemeColor('--user-message-background', getThemeColor('--convtree-user-node-color', '#1e40af'));
    const systemNodeColor = getThemeColor('--convtree-system-node-color', '#4B5563');
    const aiNodeColor = getThemeColor('--ai-message-background', getThemeColor('--convtree-ai-node-color', '#4f46e5'));
    
    // Use ConvView border colors with fallback to tree-specific values
    const userNodeBorderColor = getThemeColor('--user-message-border-color', getThemeColor('--convtree-user-node-border', '#1e3a8a'));
    const systemNodeBorderColor = getThemeColor('--convtree-system-node-border', '#374151');
    const aiNodeBorderColor = getThemeColor('--ai-message-border-color', getThemeColor('--convtree-ai-node-border', '#4338ca'));
    
    // Get border widths and styles from ConvView theme
    const userNodeBorderWidth = getThemeColor('--user-message-border-width', '1px');
    const aiNodeBorderWidth = getThemeColor('--ai-message-border-width', '1px');
    const userNodeBorderStyle = getThemeColor('--user-message-border-style', 'solid');
    const aiNodeBorderStyle = getThemeColor('--ai-message-border-style', 'solid');

    // Clear previous SVG content
    d3.select(svgRef.current).selectAll('*').remove();

    // Get container dimensions
    const containerWidth = containerRef.current.clientWidth;
    const containerHeight = containerRef.current.clientHeight || 600;

    // Create SVG element
    const svg = d3.select(svgRef.current).attr('width', containerWidth).attr('height', containerHeight);

    // Create a group for all elements
    const g = svg.append('g');

    // Configure tree layout
    const nodeSizeWidth = containerWidth < 400 ? 120 : 135;
    const nodeSizeHeight = containerWidth < 400 ? 120 : 150;
    const treeLayout = d3.tree<TreeNode>()
      .size([containerWidth - 80, containerHeight - 120])
      .nodeSize([nodeSizeWidth, nodeSizeHeight]);

    // Create hierarchy from data
    const root = d3.hierarchy(hierarchicalData);

    // Apply tree layout
    const treeData = treeLayout(root);

    // Setup zoom behavior
    const zoom = d3
      .zoom<SVGSVGElement, unknown>()
      .scaleExtent([0.1, 3])
      .on('zoom', (event) => {
        g.attr('transform', event.transform);
      });

    // Store zoom behavior in ref
    zoomRef.current = zoom;

    // Apply zoom to SVG
    svg.call(zoom);

    // Center the tree initially
    const rootX = treeData.x;
    const centerX = containerWidth / 2;
    svg.call(zoom.transform, d3.zoomIdentity.translate(centerX, 50));

    // Draw links between nodes
    g.selectAll('.link')
      .data(treeData.links())
      .enter()
      .append('path')
      .attr('class', 'link ConvTreeView')
      .attr(
        'd',
        d3
          .linkVertical<d3.HierarchyPointLink<TreeNode>, d3.HierarchyPointNode<TreeNode>>()
          .x((d) => d.x)
          .y((d) => d.y),
      )
      .attr('fill', 'none')
      .attr('stroke', linkColor)
      .attr('stroke-width', 2)
      .attr('stroke-opacity', 0.6);

    // Create node groups
    const nodeGroups = g
      .selectAll('.node')
      .data(treeData.descendants())
      .enter()
      .append('g')
      .attr('class', 'node ConvTreeView')
      .attr('transform', (d) => `translate(${d.x},${d.y})`)
      .attr('cursor', 'pointer')
      .on('click', (e, d) => onNodeClick(d.data.id, d.data.source, d.data.content))
      .on('mousedown', (e, d) => onNodeMiddleClick(e, d.data.id));

    // Draw node rectangles
    nodeGroups
      .append('rect')
      .attr('width', containerWidth < 400 ? 200 : 240)
      .attr('height', containerWidth < 400 ? 85 : 110)
      .attr('x', containerWidth < 400 ? -100 : -120)
      .attr('y', -40)
      .attr('rx', 10)
      .attr('ry', 10)
      .attr('fill', (d) => {
        const source = d.data.source;
        if (source === 'user') return userNodeColor;
        if (source === 'system') return systemNodeColor;
        return aiNodeColor;
      })
      .attr('stroke', (d) => {
        const source = d.data.source;
        if (source === 'user') return userNodeBorderColor;
        if (source === 'system') return systemNodeBorderColor;
        return aiNodeBorderColor;
      })
      .attr('stroke-width', (d) => {
        const source = d.data.source;
        if (source === 'user') return userNodeBorderWidth;
        if (source === 'system') return '1px';
        return aiNodeBorderWidth;
      })
      .attr('stroke-dasharray', (d) => {
        const source = d.data.source;
        // Convert CSS border style to SVG stroke-dasharray
        if (source === 'user') {
          return userNodeBorderStyle === 'dashed' ? '3,3' : 
                userNodeBorderStyle === 'dotted' ? '1,1' : 
                'none';
        }
        if (source === 'system') return 'none';
        return aiNodeBorderStyle === 'dashed' ? '3,3' : 
              aiNodeBorderStyle === 'dotted' ? '1,1' : 
              'none';
      })
      .attr('class', (d) => {
        // Add selected class if this node matches the selectedMessageId
        return `node-rect ConvTreeView ${d.data.id === selectedMessageId ? 'selected' : ''}`;
      });

    // Create label groups
    const nodeLabels = nodeGroups.append('g');

    // Add source label (You/AI/System)
    nodeLabels
      .append('text')
      .attr('x', -95)
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

    // Add message content
    nodeLabels
      .append('foreignObject')
      .attr('x', -95)
      .attr('y', -20)
      .attr('width', containerWidth < 400 ? 180 : 220)
      .attr('height', containerWidth < 400 ? 55 : 65)
      .append('xhtml:div')
      .style('color', 'white')
      .style('font-size', '10px')
      .style('overflow', 'hidden')
      .style('text-overflow', 'ellipsis')
      .style('display', '-webkit-box')
      .style('-webkit-line-clamp', containerWidth < 400 ? '3' : '4')
      .style('-webkit-box-orient', 'vertical')
      .style('word-wrap', 'break-word')
      .style('padding', '0 5px')
      .style('margin-bottom', '3px')
      .html((d) => {
        const content = d.data.content || '';
        return content
          .replace(/&/g, '&amp;')
          .replace(/</g, '&lt;')
          .replace(/>/g, '&gt;')
          .replace(/"/g, '&quot;')
          .replace(/'/g, '&#039;');
      });

    // Add caption with model info and timestamp
    nodeLabels
      .append('foreignObject')
      .attr('x', -95)
      .attr('y', containerWidth < 400 ? 35 : 50)
      .attr('width', containerWidth < 400 ? 180 : 220)
      .attr('height', 20)
      .append('xhtml:div')
      .style('color', '#c7d2fe')
      .style('font-size', containerWidth < 400 ? '7px' : '8px')
      .style('text-align', 'right')
      .style('padding', '0 5px')
      .style('overflow', 'hidden')
      .style('text-overflow', 'ellipsis')
      .style('white-space', 'nowrap')
      .html((d) => {
        // Format timestamp
        let timeInfo = '';
        if (d.data.timestamp) {
          const date = new Date(d.data.timestamp);
          const dateOptions: Intl.DateTimeFormatOptions = { month: 'short', day: 'numeric' };
          const timeOptions: Intl.DateTimeFormatOptions = { hour: '2-digit', minute: '2-digit' };
          timeInfo = `${date.toLocaleDateString(undefined, dateOptions)} ${date.toLocaleTimeString(undefined, timeOptions)}`;
        }

        // Get model info - but only for AI messages
        let modelInfo = '';
        if (d.data.source === 'ai' && d.data.costInfo?.modelGuid) {
          const modelGuid = d.data.costInfo.modelGuid;
          const modelName = getModelFriendlyName(modelGuid);

          // Fallback if modelName is empty
          if (!modelName || modelName === 'Unknown Model') {
            modelInfo = modelGuid.split('-')[0] || 'AI';
          } else {
            modelInfo = modelName;
          }
        }

        const formatCaption = (text: string) => {
          const maxLength = containerWidth < 400 ? 50 : 60;
          return text.length > maxLength ? text.substring(0, maxLength) + '...' : text;
        };

        // Limit model info length
        const shortModelInfo = modelInfo.length > (containerWidth < 400 ? 35 : 40) ?
          modelInfo.substring(0, (containerWidth < 400 ? 35 : 40)) + '...' :
          modelInfo;

        // Create the final caption
        const caption = modelInfo && timeInfo ?
          `<span style=\"background-color: rgba(99, 102, 241, 0.2); border-radius: 4px; padding: 1px 3px;\">${shortModelInfo}</span> · ${timeInfo}` :
          modelInfo ?
            `<span style=\"background-color: rgba(99, 102, 241, 0.2); border-radius: 4px; padding: 1px 3px;\">${shortModelInfo}</span>` :
            timeInfo ? formatCaption(timeInfo) : '';

        return caption;
      });

    // Return cleanup function
    return () => {
      d3.select(svgRef.current).selectAll('*').remove();
    };
  }, [hierarchicalData, updateKey, onNodeClick, onNodeMiddleClick]);

  // Function to update the selected node without re-initializing the whole visualization
  const updateSelectedNode = useCallback((messageId: string) => {
    if (!svgRef.current) return;
    
    // Remove previous selection highlight
    d3.select(svgRef.current).selectAll('.node-rect').classed('selected', false);
    
    // Find and highlight the new selected node
    const nodes = d3.select(svgRef.current).selectAll('.node');
    const selectedNode = nodes.filter((d: any) => d.data.id === messageId);
    
    if (!selectedNode.empty()) {
      selectedNode.select('.node-rect').classed('selected', true);
    }
  }, [svgRef]);
  
  // Apply initial selection if selectedMessageId is provided
  useEffect(() => {
    if (selectedMessageId && svgRef.current) {
      updateSelectedNode(selectedMessageId);
    }
  }, [selectedMessageId, updateSelectedNode, updateKey]);

  // Track previous hierarchicalData to detect new messages
  const [prevHierarchicalData, setPrevHierarchicalData] = useState<TreeNode | null>(null);

  // Focus on newly added messages
  useEffect(() => {
    if (!hierarchicalData) {
      setPrevHierarchicalData(null);
      return;
    }

    // Function to count total nodes in a tree
    const countNodes = (node: TreeNode): number => {
      return 1 + (node.children?.reduce((sum, child) => sum + countNodes(child), 0) || 0);
    };

    // Function to find the most recently added node
    const findLatestMessages = (node: TreeNode): TreeNode[] => {
      const messages: TreeNode[] = [node];
      if (node.children) {
        node.children.forEach(child => {
          messages.push(...findLatestMessages(child));
        });
      }
      return messages;
    };

    // If we have previous data to compare with
    if (prevHierarchicalData) {
      const prevCount = countNodes(prevHierarchicalData);
      const currentCount = countNodes(hierarchicalData);

      // If new nodes were added
      if (currentCount > prevCount) {
        const messages = findLatestMessages(hierarchicalData);
        handleFocusOnLatest(messages);
      }
    }

    // Update previous data reference
    setPrevHierarchicalData(hierarchicalData);
  }, [hierarchicalData, handleFocusOnLatest]);

  return {
    zoomRef,
    handleZoomIn,
    handleZoomOut,
    handleCenter,
    handleFocusOnLatest,
    updateSelectedNode
  };
};