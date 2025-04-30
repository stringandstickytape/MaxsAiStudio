// AiStudio4.Web\src\components\ConvTreeView\MessageNode.tsx
import React from 'react';
import { Handle, Position } from 'reactflow';
import { TreeNode } from './types';

interface MessageNodeProps {
  data: TreeNode & {
    colors: {
      background: string;
      border: string;
      borderWidth: string;
      borderStyle: string;
    };
    timeInfo: string;
    modelInfo: string;
    onNodeClick: (nodeId: string, nodeSource: string, nodeContent: string) => void;
    onNodeMiddleClick: (event: any, nodeId: string) => void;
  };
}

export const MessageNode: React.FC<MessageNodeProps> = ({ data }) => {
  const { id, content, source, colors, timeInfo, modelInfo } = data;
  
  // Convert border style to CSS
  const getBorderStyle = () => {
    const { borderStyle, borderWidth, border } = colors;
    if (borderWidth === '0px') return 'none';
    return `${borderWidth} ${borderStyle} ${border}`;
  };
  
  // Handle node click
  const handleClick = () => {
    data.onNodeClick(id, source, content);
  };
  
  // Handle node middle click
  const handleMouseDown = (e: React.MouseEvent) => {
    data.onNodeMiddleClick(e, id);
  };
  
  // Get source label text
  const getSourceLabel = () => {
    if (source === 'user') return 'You';
    if (source === 'system') return 'System';
    return 'AI';
  };
  
  // Format model info with badge if available
  const getFormattedCaption = () => {
    if (modelInfo && timeInfo) {
      return (
        <>
          <span className="bg-indigo-900/20 rounded px-1 py-0.5 text-[7px] md:text-[8px]">
            {modelInfo}
          </span>
          <span className="mx-1">·</span>
          <span>{timeInfo}</span>
        </>
      );
    }
    
    if (modelInfo) {
      return (
        <span className="bg-indigo-900/20 rounded px-1 py-0.5 text-[7px] md:text-[8px]">
          {modelInfo}
        </span>
      );
    }
    
    if (timeInfo) {
      return <span>{timeInfo}</span>;
    }
    
    return null;
  };
  
  return (
    <div 
      className="ConvTreeView relative rounded-lg p-3 shadow-md w-[220px] md:w-[240px]"
      style={{
        backgroundColor: colors.background,
        border: getBorderStyle(),
        color: 'white',
      }}
      onClick={handleClick}
      onMouseDown={handleMouseDown}
    >
      {/* Connection handles */}
      <Handle 
        type="target" 
        position={Position.Top} 
        style={{ background: '#6b7280', width: '8px', height: '8px' }} 
      />
      <Handle 
        type="source" 
        position={Position.Bottom} 
        style={{ background: '#6b7280', width: '8px', height: '8px' }} 
      />
      
      {/* Source label */}
      <div className="text-[10px] font-bold mb-1">
        {getSourceLabel()}
      </div>
      
      {/* Message content */}
      <div 
        className="text-[10px] overflow-hidden text-ellipsis line-clamp-4 mb-2"
        style={{ wordWrap: 'break-word' }}
      >
        {content}
      </div>
      
      {/* Caption with model info and timestamp */}
      <div className="text-[7px] md:text-[8px] text-indigo-200 text-right">
        {getFormattedCaption()}
      </div>
    </div>
  );
};