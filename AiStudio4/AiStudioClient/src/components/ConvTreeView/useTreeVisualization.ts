// AiStudioClient\src\components\ConvTreeView\useTreeVisualization.ts
import { useEffect, useLayoutEffect, useRef, RefObject, useCallback, useState } from 'react';
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
  searchResults?: { conversationId: string; matchingMessageIds: string[] }[] | null;
  highlightedMessageId?: string | null;
}

export const useTreeVisualization = ({


  svgRef,
  containerRef,
  hierarchicalData,
  onNodeClick,
  onNodeMiddleClick,
  updateKey,
  selectedMessageId,
  searchResults,
  highlightedMessageId
}: UseTreeVisualizationParams) => {
  // Ref to store the zoom behavior
  const zoomRef = useRef<d3.ZoomBehavior<SVGSVGElement, unknown> | null>(null);
  
  // Ref to track previous highlighted message ID to avoid unnecessary updates
  const previousHighlightRef = useRef<string | null>(null);

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
    
    // Create a glow filter for search matches
    const defs = d3.select(svgRef.current).append('defs');
    
    // Standard glow filter
    const filter = defs.append('filter')
      .attr('id', 'glow');
    
    filter.append('feGaussianBlur')
      .attr('stdDeviation', '2')
      .attr('result', 'blur');
      
    filter.append('feComposite')
      .attr('in', 'SourceGraphic')
      .attr('in2', 'blur')
      .attr('operator', 'over');
      
    // Create a stronger glow filter for highlighted messages
    const strongGlow = defs.append('filter')
      .attr('id', 'strongGlow')
      .attr('x', '-50%')
      .attr('y', '-50%')
      .attr('width', '200%')
      .attr('height', '200%');
    
    strongGlow.append('feGaussianBlur')
      .attr('stdDeviation', '5')
      .attr('result', 'blur');
      
    strongGlow.append('feComposite')
      .attr('in', 'SourceGraphic')
      .attr('in2', 'blur')
      .attr('operator', 'over');
    
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
    
    // Map global theme values to tree nodes
    const userNodeColor = getThemeColor('--global-user-message-background', getThemeColor('--convtree-user-node-color', '#1e40af'));
    const systemNodeColor = getThemeColor('--convtree-system-node-color', '#4B5563');
    const aiNodeColor = getThemeColor('--global-ai-message-background', getThemeColor('--convtree-ai-node-color', '#4f46e5'));
    
    // Use global border colors with fallback to tree-specific values
    const userNodeBorderColor = getThemeColor('--global-user-message-border-color', getThemeColor('--convtree-user-node-border', '#1e3a8a'));
    const systemNodeBorderColor = getThemeColor('--convtree-system-node-border', '#374151');
    const aiNodeBorderColor = getThemeColor('--global-ai-message-border-color', getThemeColor('--convtree-ai-node-border', '#4338ca'));
    
    // Get border widths and styles from global theme
    const userNodeBorderWidth = getThemeColor('--global-user-message-border-width', '1px');
    const aiNodeBorderWidth = getThemeColor('--global-ai-message-border-width', '1px');
    const userNodeBorderStyle = getThemeColor('--global-user-message-border-style', 'solid');
    const aiNodeBorderStyle = getThemeColor('--global-ai-message-border-style', 'solid');

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
      
    // Add scale-invariant indicators for search matches
    nodeGroups.each(function(d) {
      const node = d3.select(this);
      const isSearchMatch = searchResults?.some(result => 
        result.matchingMessageIds.includes(d.data.id)
      );
      const isHighlighted = d.data.id === highlightedMessageId;
      
      // Add indicators only for search matches
      if (isSearchMatch || isHighlighted) {
        // Add a scale-invariant indicator that will remain visible at any zoom level
        node.append('circle')
          .attr('class', 'search-indicator')
          .attr('r', 8) // Size of the indicator
          .attr('cx', containerWidth < 400 ? -100 : -120) // Position at top-left of node
          .attr('cy', -40)
          .attr('fill', getThemeColor('--convtree-accent-color', '#4f46e5'))
          .attr('stroke', 'white')
          .attr('stroke-width', 1.5)
          .attr('opacity', isHighlighted ? 1 : 0.8);
        
        // Add a pulsing animation for highlighted messages
        if (isHighlighted) {
          const pulseCircle = node.append('circle')
            .attr('class', 'pulse-indicator')
            .attr('r', 8)
            .attr('cx', containerWidth < 400 ? -100 : -120)
            .attr('cy', -40)
            .attr('fill', 'none')
            .attr('stroke', getThemeColor('--convtree-accent-color', '#4f46e5'))
            .attr('stroke-width', 2)
            .attr('opacity', 0.6);
          
          // Add pulsing animation
          function pulse() {
            pulseCircle
              .attr('r', 8)
              .attr('opacity', 0.6)
              .transition()
              .duration(1500)
              .attr('r', 20)
              .attr('opacity', 0)
              .on('end', pulse);
          }
          
          pulse();
        }
      }
    });

    // Add tooltip for middle-click delete
    nodeGroups.append('title')
      .text('Middle-click to delete');

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
        const isSearchMatch = searchResults?.some(result => 
          result.matchingMessageIds.includes(d.data.id)
        );
        
        // Apply a slightly different color for search matches
        if (isSearchMatch) {
          if (source === 'user') return d3.color(userNodeColor)?.brighter(0.3)?.toString() || userNodeColor;
          if (source === 'system') return d3.color(systemNodeColor)?.brighter(0.3)?.toString() || systemNodeColor;
          return d3.color(aiNodeColor)?.brighter(0.3)?.toString() || aiNodeColor;
        }
        
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
      })
      .attr('stroke', (d) => {
        // Check if this node is in search results
        const isSearchMatch = searchResults?.some(result => 
          result.matchingMessageIds.includes(d.data.id)
        );
        
        // Check if this is the highlighted message
        const isHighlighted = d.data.id === highlightedMessageId;
        
        if (isHighlighted) {
          return getThemeColor('--convtree-accent-color', '#4f46e5');
        } else if (isSearchMatch) {
          return getThemeColor('--convtree-accent-color', '#4f46e5');
        } else if (d.data.id === selectedMessageId) {
          return '#f59e0b'; // Amber color for selected nodes
        }
        
        // Default stroke based on message source
        const source = d.data.source;
        if (source === 'user') return userNodeBorderColor;
        if (source === 'system') return systemNodeBorderColor;
        return aiNodeBorderColor;
      })
      .attr('stroke-width', (d) => {
        // Check if this node is in search results
        const isSearchMatch = searchResults?.some(result => 
          result.matchingMessageIds.includes(d.data.id)
        );
        
        // Check if this is the highlighted message
        const isHighlighted = d.data.id === highlightedMessageId;
        
        if (isHighlighted) {
          return '4px'; // Increased from 3px for better visibility
        } else if (isSearchMatch) {
          return '3px'; // Increased from 2px for better visibility
        } else if (d.data.id === selectedMessageId) {
          return '2px';
        }
        
        // Default stroke width
        return '1px';
      })
      .attr('filter', (d) => {
        // Apply glow effect to highlighted message with stronger glow
        if (d.data.id === highlightedMessageId) {
          return 'url(#strongGlow)';
        } else if (searchResults?.some(result => result.matchingMessageIds.includes(d.data.id))) {
          return 'url(#glow)';
        }
        return null;
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
    
    // Helper function to get theme colors - duplicated from the main effect
    // to ensure we have access to these values in this callback
    const getThemeColorForUpdate = (varName: string, fallback: string) => {
      let value = '';
      if (containerRef.current && document.body.contains(containerRef.current)) {
        value = getComputedStyle(containerRef.current).getPropertyValue(varName);
      }
      if (!value || value.trim() === '') {
        value = getComputedStyle(document.documentElement).getPropertyValue(varName);
      }
      return value && value.trim() !== '' ? value : fallback;
    };
    
    // Get theme colors for borders
    const userBorderColor = getThemeColorForUpdate('--global-user-message-border-color', getThemeColorForUpdate('--convtree-user-node-border', '#1e3a8a'));
    const systemBorderColor = getThemeColorForUpdate('--convtree-system-node-border', '#374151');
    const aiBorderColor = getThemeColorForUpdate('--global-ai-message-border-color', getThemeColorForUpdate('--convtree-ai-node-border', '#4338ca'));
    
    // Get border widths
    const userBorderWidth = getThemeColorForUpdate('--global-user-message-border-width', '1px');
    const aiBorderWidth = getThemeColorForUpdate('--global-ai-message-border-width', '1px');
    
    // Find all nodes
    const nodes = d3.select(svgRef.current).selectAll('.node');
    
    // Update all nodes to reflect current selection, search results, and highlighted message
    nodes.each(function(d: any) {
      const node = d3.select(this);
      const nodeRect = node.select('.node-rect');
      
      // Check if this node is in search results
      const isSearchMatch = searchResults?.some(result => 
        result.matchingMessageIds.includes(d.data.id)
      );
      
      // Check if this is the highlighted message
      const isHighlighted = d.data.id === highlightedMessageId;
      
      // Check if this is the selected message
      const isSelected = d.data.id === messageId;
      
      // Update class for selection
      nodeRect.classed('selected', isSelected);
      
      // Update stroke color and width based on status
      if (isHighlighted) {
        nodeRect.attr('stroke', getThemeColorForUpdate('--convtree-accent-color', '#4f46e5'));
        nodeRect.attr('stroke-width', '4px'); // Increased from 3px
        nodeRect.attr('filter', 'url(#strongGlow)');
      } else if (isSearchMatch) {
        nodeRect.attr('stroke', getThemeColorForUpdate('--convtree-accent-color', '#4f46e5'));
        nodeRect.attr('stroke-width', '3px'); // Increased from 2px
        nodeRect.attr('filter', 'url(#glow)');
      } else if (isSelected) {
        nodeRect.attr('stroke', '#f59e0b');
        nodeRect.attr('stroke-width', '2px');
        nodeRect.attr('filter', null);
      } else {
        // Reset to default
        const source = d.data.source;
        if (source === 'user') {
          nodeRect.attr('stroke', userBorderColor);
          nodeRect.attr('stroke-width', userBorderWidth);
        } else if (source === 'system') {
          nodeRect.attr('stroke', systemBorderColor);
          nodeRect.attr('stroke-width', '1px');
        } else {
          nodeRect.attr('stroke', aiBorderColor);
          nodeRect.attr('stroke-width', aiBorderWidth);
        }
        nodeRect.attr('filter', null);
      }
      
      // Update or create scale-invariant indicators
      // First remove any existing indicators
      node.selectAll('.search-indicator, .pulse-indicator').remove();
      
      // Add new indicators if needed
      if (isHighlighted || isSearchMatch) {
        const accentColor = getThemeColorForUpdate('--convtree-accent-color', '#4f46e5');
        
        // Add a scale-invariant indicator that will remain visible at any zoom level
        node.append('circle')
          .attr('class', 'search-indicator')
          .attr('r', 8) // Size of the indicator
          .attr('cx', containerRef.current ? (containerRef.current.clientWidth < 400 ? -100 : -120) : -120) // Position at top-left of node
          .attr('cy', -40)
          .attr('fill', accentColor)
          .attr('stroke', 'white')
          .attr('stroke-width', 1.5)
          .attr('opacity', isHighlighted ? 1 : 0.8);
        
        // Add a pulsing animation for highlighted messages
        if (isHighlighted) {
          const pulseCircle = node.append('circle')
            .attr('class', 'pulse-indicator')
            .attr('r', 8)
            .attr('cx', containerRef.current ? (containerRef.current.clientWidth < 400 ? -100 : -120) : -120)
            .attr('cy', -40)
            .attr('fill', 'none')
            .attr('stroke', accentColor)
            .attr('stroke-width', 2)
            .attr('opacity', 0.6);
          
          // Add pulsing animation
          function pulse() {
            pulseCircle
              .attr('r', 8)
              .attr('opacity', 0.6)
              .transition()
              .duration(1500)
              .attr('r', 20)
              .attr('opacity', 0)
              .on('end', pulse);
          }
          
          pulse();
        }
      }
    });
    
    // If this is also the highlighted message from search, scroll to it
    if (messageId === highlightedMessageId) {
      const selectedNode = nodes.filter((d: any) => d.data.id === messageId);
      if (!selectedNode.empty() && containerRef.current && zoomRef.current) {
        const nodeData = selectedNode.datum() as any;
        const containerWidth = containerRef.current.clientWidth;
        const containerHeight = containerRef.current.clientHeight;
        
        // Smoothly zoom to the highlighted node
        d3.select(svgRef.current)
          .transition()
          .duration(500)
          .call(
            zoomRef.current.transform,
            d3.zoomIdentity
              .translate(containerWidth / 2, containerHeight / 2)
              .scale(1.2)
              .translate(-nodeData.x, -nodeData.y)
          );
      }
    }
  }, [svgRef, highlightedMessageId, searchResults, containerRef, zoomRef]);
  
  // Apply initial selection if selectedMessageId is provided
  useEffect(() => {
    if (selectedMessageId && svgRef.current) {
      updateSelectedNode(selectedMessageId);
    }
  }, [selectedMessageId, updateSelectedNode, updateKey]);
  
  // Debounce function for search result updates
  const debounce = (func: Function, wait: number) => {
    let timeout: NodeJS.Timeout | null = null;
    return (...args: any[]) => {
      if (timeout) clearTimeout(timeout);
      timeout = setTimeout(() => func(...args), wait);
    };
  };
  
  // Debounced version of updateSelectedNode
  const debouncedUpdateNodes = useCallback(
    debounce((messageId: string) => {
      if (!svgRef.current) return;
      updateSelectedNode(messageId);
    }, 150), // 150ms debounce delay
    [updateSelectedNode, svgRef]
  );
  
  // Update search match highlighting when search results change
  useEffect(() => {
    if (!svgRef.current) return;
    
    // Use debounced update for search results to prevent excessive re-rendering
    debouncedUpdateNodes(selectedMessageId || '');
    
    // If we have a highlighted message, scroll to it - but only when explicitly highlighted
    // not on every search result update
    if (highlightedMessageId && highlightedMessageId !== previousHighlightRef.current) {
      previousHighlightRef.current = highlightedMessageId;
      
      // Delay the zoom operation to avoid doing it during batch updates
      setTimeout(() => {
        if (!svgRef.current) return;
        
        const nodes = d3.select(svgRef.current).selectAll('.node');
        const highlightedNode = nodes.filter((d: any) => d.data.id === highlightedMessageId);
        
        if (!highlightedNode.empty() && containerRef.current && zoomRef.current) {
          const nodeData = highlightedNode.datum() as any;
          const containerWidth = containerRef.current.clientWidth;
          const containerHeight = containerRef.current.clientHeight;
          
          // Smoothly zoom to the highlighted node
          d3.select(svgRef.current)
            .transition()
            .duration(500)
            .call(
              zoomRef.current.transform,
              d3.zoomIdentity
                .translate(containerWidth / 2, containerHeight / 2)
                .scale(1.2)
                .translate(-nodeData.x, -nodeData.y)
            );
        }
      }, 100);
    }
  }, [searchResults, highlightedMessageId, svgRef, containerRef, zoomRef, updateSelectedNode, selectedMessageId]);
  
  // Add a zoom event listener to adjust scale-invariant indicators
  useEffect(() => {
    if (!svgRef.current || !zoomRef.current) return;
    
    // Add zoom event listener
    const handleZoom = () => {
      // Get current zoom transform
      const transform = d3.zoomTransform(svgRef.current!);
      
      // Adjust the size of indicators based on zoom level
      d3.select(svgRef.current!).selectAll('.search-indicator, .pulse-indicator')
        .attr('r', (d) => 8 / transform.k) // Inverse scale based on zoom level
        .attr('stroke-width', (d) => 1.5 / transform.k); // Adjust stroke width too
    };
    
    // Attach the zoom event listener
    d3.select(svgRef.current).call(zoomRef.current.on('zoom.indicators', handleZoom));
    
    // Cleanup
    return () => {
      if (zoomRef.current) {
        zoomRef.current.on('zoom.indicators', null);
      }
    };
  }, [svgRef, zoomRef, updateKey]);

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

  // Center view when a new conversation is loaded
  useEffect(() => {
    // If hierarchicalData changes from null to a value, it means a new conversation was loaded
    if (hierarchicalData && !prevHierarchicalData) {
      // Use requestAnimationFrame to ensure the tree is fully rendered before centering
      // This will schedule the centering to happen after the next paint
      const checkAndCenter = () => {
        if (svgRef.current) {
          const nodes = d3.select(svgRef.current).selectAll('.node');
          if (nodes.size() > 0) {
            // Nodes are available, we can center
            handleCenter();
          } else {
            // Nodes not ready yet, try again in the next frame
            requestAnimationFrame(checkAndCenter);
          }
        }
      };
      
      // Start the check and center process
      requestAnimationFrame(checkAndCenter);
    }
  }, [hierarchicalData, prevHierarchicalData, handleCenter, svgRef]);

  return {
    zoomRef,
    handleZoomIn,
    handleZoomOut,
    handleCenter,
    handleFocusOnLatest,
    updateSelectedNode
  };
};