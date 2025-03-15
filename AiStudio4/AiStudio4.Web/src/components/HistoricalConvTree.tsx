import * as React from 'react';
import { useWebSocketStore } from '@/stores/useWebSocketStore';
import { ChevronRight, ChevronDown, MessageCircle } from 'lucide-react';

export interface TreeNode {
  id: string;
  text: string;
  children: TreeNode[] | TreeNode;
}

interface HistoricalConvTreeProps {
  treeData: TreeNode;
  onNodeClick: (nodeId: string) => void;
}

export const HistoricalConvTree: React.FC<HistoricalConvTreeProps> = ({ treeData, onNodeClick }) => {
  
  const { clientId } = useWebSocketStore();
  const [expandedNodes, setExpandedNodes] = React.useState<Record<string, boolean>>({});

  const handleNodeClick = async (node: TreeNode) => {
    
    window.history.pushState({}, '', `?messageId=${node.id}`);

    
    onNodeClick(node.id);

    
    if (node.text.startsWith('User:')) {
      
      const userContent = node.text.substring(5).trim();
      window.setPrompt(userContent);
    }
  };

  const toggleNode = (nodeId: string, event: React.MouseEvent) => {
    event.stopPropagation();
    setExpandedNodes(prev => ({
      ...prev,
      [nodeId]: !prev[nodeId]
    }));
  };

  
  React.useEffect(() => {
    if (treeData) {
      const expandAllNodes = (node: TreeNode, expandedState: Record<string, boolean>) => {
        expandedState[node.id] = true;
        
        
        const children = node.children ? (Array.isArray(node.children) ? node.children : [node.children]) : [];
        children.forEach(child => expandAllNodes(child, expandedState));
        
        return expandedState;
      };
      
      setExpandedNodes(expandAllNodes(treeData, {}));
    }
  }, [treeData]);

  const renderTree = (node: TreeNode): JSX.Element | JSX.Element[] => {
    // Special case for the root node: don't render it, but render its children directly
    if (node.text === 'Conv Root' || node.text.includes('Conversation Root')) {
      const children = node.children ? (Array.isArray(node.children) ? node.children : [node.children]) : [];
      return <>{children.map((child) => renderTree(child))}</>;
    }
    
    const children = node.children ? (Array.isArray(node.children) ? node.children : [node.children]) : [];
    const hasChildren = children.length > 0;
    const isExpanded = expandedNodes[node.id] ?? true; 

    return (
      <div key={node.id} className="my-1 transition-all duration-200 ease-in-out ">
        <div 
          className="flex items-center rounded-md hover:bg-gray-700/70 group transition-all duration-150"
          onClick={() => handleNodeClick(node)}
        >
          {hasChildren ? (
            <div 
              className="w-4 h-4 flex items-center justify-center text-gray-400 hover:text-white cursor-pointer mr-1"
              onClick={(e) => toggleNode(node.id, e)}
            >
              {isExpanded ? <ChevronDown size={14} /> : <ChevronRight size={14} />}
            </div>
          ) : (
            <MessageCircle size={12} className="w-4 h-4 mr-1 text-gray-500 opacity-70" />
          )}
          <div
            className="text-sm text-gray-300 group-hover:text-white cursor-pointer overflow-hidden text-ellipsis whitespace-nowrap max-w-full transition-colors duration-150"
            title={node.text}
          >
            {node.text}
          </div>
        </div>
        {hasChildren && isExpanded && (
          <div className="pl-2 mt-1 border-l border-gray-700/50 ml-2.5">
            {children.map((child) => renderTree(child))}
          </div>
        )}
      </div>
    );
  };

  return <div className="px-0.5 py-1">{treeData && renderTree(treeData)}</div>;
};

