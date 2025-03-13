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
  // Use Zustand store
  const { clientId } = useWebSocketStore();
  const [expandedNodes, setExpandedNodes] = React.useState<Record<string, boolean>>({});

  const handleNodeClick = async (node: TreeNode) => {
    // Add messageId to URL when loading conv
    window.history.pushState({}, '', `?messageId=${node.id}`);

    // Call the parent's onNodeClick handler
    onNodeClick(node.id);

    // Check if this is a user message - if so, load it into input area
    if (node.text.startsWith('User:')) {
      // Extract the content without the 'User:' prefix
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

  // Initialize all nodes as expanded when the tree data changes
  React.useEffect(() => {
    if (treeData) {
      const expandAllNodes = (node: TreeNode, expandedState: Record<string, boolean>) => {
        expandedState[node.id] = true;
        
        // Process children if they exist
        const children = node.children ? (Array.isArray(node.children) ? node.children : [node.children]) : [];
        children.forEach(child => expandAllNodes(child, expandedState));
        
        return expandedState;
      };
      
      setExpandedNodes(expandAllNodes(treeData, {}));
    }
  }, [treeData]);

  const renderTree = (node: TreeNode): JSX.Element => {
    // Ensure children is always an array
    const children = node.children ? (Array.isArray(node.children) ? node.children : [node.children]) : [];
    const hasChildren = children.length > 0;
    const isExpanded = expandedNodes[node.id] ?? true; // Default to expanded if not in state

    return (
      <div key={node.id} className="my-1 transition-all duration-200 ease-in-out ">
        <div 
          className="flex items-center py-1 px-1.5 rounded-md hover:bg-gray-700/70 group transition-all duration-150"
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

  return <div className="px-0.5 py-1">{renderTree(treeData)}</div>;
};