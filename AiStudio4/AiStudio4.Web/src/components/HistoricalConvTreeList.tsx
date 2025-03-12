// src/components/HistoricalConvTreeList.tsx
import { useState, useEffect } from 'react';
import { useWebSocketStore } from '@/stores/useWebSocketStore';
import { HistoricalConvTree } from './HistoricalConvTree';
import { useConvStore } from '@/stores/useConvStore';
import { useHistoricalConvsStore } from '@/stores/useHistoricalConvsStore';
import { useChatManagement } from '@/hooks/useChatManagement';
import { ChevronDown, ChevronRight, Search, X, Calendar, MessageSquare } from 'lucide-react';
import { ScrollArea } from '@/components/ui/scroll-area';

interface TreeNode {
  id: string;
  text: string;
  children: TreeNode[];
}

export const HistoricalConvTreeList = () => {
  const [expandedConv, setExpandedConv] = useState<string | null>(null);
  const [treeData, setTreeData] = useState<TreeNode | null>(null);
  const [searchTerm, setSearchTerm] = useState<string>('');
  const [isLoadingTree, setIsLoadingTree] = useState<boolean>(false);

  // Use Zustand stores
  const { clientId } = useWebSocketStore();
  const { createConv, addMessage, setActiveConv, convs: currentConvs } = useConvStore();
  const { convs, isLoading, fetchAllConvs, fetchConvTree: fetchTreeData, addOrUpdateConv } = useHistoricalConvsStore();

  // Fetch all historical convs on component mount
  useEffect(() => {
    fetchAllConvs();
  }, [fetchAllConvs]);

  // Setup WebSocket subscription for new convs
  useEffect(() => {
    // Create a handler for historical conv tree events
    const handleHistoricalConv = (content: any) => {
      if (content) {
        addOrUpdateConv({
          convGuid: content.convId || content.convGuid,
          summary: content.summary || content.content || 'Untitled Conv',
          fileName: `conv_${content.convId || content.convGuid}.json`,
          lastModified: content.lastModified || new Date().toISOString(),
          highlightColour: content.highlightColour,
        });
      }
    };

    // Add a WebSocket event listener
    const unsubscribe = useWebSocketStore.subscribe(
      (state) => state.lastMessageTime,
      async () => {
        // This will be called whenever a new WebSocket message is received
        const event = new CustomEvent('check-historical-convs');
        window.dispatchEvent(event);
      },
    );

    // Add an event listener directly to catch historical conv updates
    const handleHistoricalEvent = (e: any) => {
      if (e.detail?.type === 'historicalConvTree') {
        handleHistoricalConv(e.detail.content);
      }
    };

    window.addEventListener('historical-conv', handleHistoricalEvent);

    // Clean up by removing the listener when component unmounts
    return () => {
      unsubscribe();
      window.removeEventListener('historical-conv', handleHistoricalEvent);
    };
  }, [addOrUpdateConv]);

  // Function to fetch conv tree data when expanding a conv
  const handleFetchConvTree = async (convId: string) => {
    setIsLoadingTree(true);
    try {
      const tree = await fetchTreeData(convId);
      setTreeData(tree);
    } catch (error) {
      console.error('Error fetching conversation tree:', error);
    } finally {
      setIsLoadingTree(false);
    }
  };

  // Handle node click to load conv by ID
  // Use custom hook for loading conv by ID
  const { getConv } = useChatManagement();

  const handleNodeClick = async (nodeId: string, convId: string) => {
    if (!clientId) return;

    try {
      console.log(`Loading conv ${convId} with selected message ${nodeId}`);

      // First check if conv already exists in store
      const existingConv = currentConvs[convId];

      if (existingConv) {
        console.log('Conv already exists in store, setting as active');
        // If conv already exists, just set it as active with the selected message
        setActiveConv({
          convId,
          slctdMsgId: nodeId,
        });
        return;
      }

      // Use the hook to get the conv data
      const conv = await getConv(convId);

      if (conv && conv.messages && conv.messages.length > 0) {
        // Sort messages by parent-child relationship
        const sortedMessages = [...conv.messages];

        // Find the root message - either the first with no parent or the first message
        const rootMessage = sortedMessages.find((msg) => !msg.parentId) || sortedMessages[0];

        // Create a new conv in the store with the root message
        createConv({
          id: convId,
          rootMessage: {
            id: rootMessage.id,
            content: rootMessage.content,
            source: rootMessage.source,
            parentId: null,
            timestamp: rootMessage.timestamp,
          },
        });

        // Add all non-root messages to the conv
        const nonRootMessages = sortedMessages.filter((msg) => msg.id !== rootMessage.id);
        nonRootMessages.forEach((message) => {
          addMessage({
            convId,
            message: {
              id: message.id,
              content: message.content,
              source: message.source,
              parentId: message.parentId,
              timestamp: message.timestamp,
              costInfo: message.costInfo,
            },
          });
        });

        // Add messageId to URL
        window.history.pushState({}, '', `?messageId=${nodeId}`);

        // Set the active conv with the selected message
        setActiveConv({
          convId,
          slctdMsgId: nodeId,
        });
      } else {
        console.error('Failed to load conv data or empty conv');
      }
    } catch (error) {
      console.error('Error loading conv:', error);
    }
  };
  // Filter conversations based on search term
  const filteredConvs = convs.filter(conv => 
    searchTerm ? conv.summary.toLowerCase().includes(searchTerm.toLowerCase()) : true
  );

  // Format date for display
  const formatDate = (dateString: string) => {
    const date = new Date(dateString);
    return new Intl.DateTimeFormat('en-US', {
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    }).format(date);
  };

  return (
    <div className="flex flex-col h-full space-y-2">
      {/* Search Bar */}
      <div className="px-3 pt-2 pb-1 sticky top-0 z-10 bg-gray-900/90 backdrop-blur-sm">
        <div className="relative">
          <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
            <Search size={16} className="text-gray-400" />
          </div>
          <input
            type="text"
            placeholder="Search conversations..."
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
            className="w-full py-1.5 pl-10 pr-8 text-sm rounded-md border border-gray-700 bg-gray-800/80 
                      text-gray-200 placeholder-gray-500 focus:outline-none focus:ring-1 focus:ring-blue-500 focus:border-blue-500"
          />
          {searchTerm && (
            <button
              onClick={() => setSearchTerm('')}
              className="absolute inset-y-0 right-2 flex items-center text-gray-400 hover:text-gray-200"
            >
              <X size={14} />
            </button>
          )}
        </div>
      </div>

      {/* Conversations List */}
      <div className="flex-1 overflow-y-auto">
        {isLoading ? (
          <div className="p-4 text-center text-gray-400 flex flex-col items-center">
            <div className="animate-spin rounded-full h-6 w-6 border-t-2 border-b-2 border-blue-500 mb-2"></div>
            <span>Loading conversations...</span>
          </div>
        ) : filteredConvs.length === 0 ? (
          <div className="p-4 text-center text-gray-400 flex flex-col items-center">
            {searchTerm ? (
              <>
                <MessageSquare size={24} className="mb-2 text-gray-500" />
                <span>No conversations matching "{searchTerm}"</span>
              </>
            ) : (
              <>
                <MessageSquare size={24} className="mb-2 text-gray-500" />
                <span>No conversations found</span>
              </>
            )}
          </div>
        ) : (
          <ScrollArea className="h-full pr-1">
            <div className="space-y-1 px-1">
              {filteredConvs.map((conv) => (
                <div
                  key={conv.convGuid}
                  className={`rounded-lg overflow-hidden transition-all duration-200 ${expandedConv === conv.convGuid ? 'bg-gray-800/60' : 'hover:bg-gray-800/40'}`}
                >
                  {/* Conversation Header */}
                  <div
                    className="flex items-center p-2.5 cursor-pointer"
                    onClick={() => {
                      const newConvId = expandedConv === conv.convGuid ? null : conv.convGuid;
                      setExpandedConv(newConvId);
                      if (newConvId) handleFetchConvTree(newConvId);
                    }}
                  >
                    <div className="mr-2 text-gray-400">
                      {expandedConv === conv.convGuid ? 
                        <ChevronDown size={18} className="text-blue-400" /> : 
                        <ChevronRight size={18} />}
                    </div>
                    
                    <div className="flex-1 min-w-0">
                      <div className="font-medium text-sm text-gray-200 truncate">
                        {conv.summary}
                      </div>
                      <div className="flex items-center text-xs text-gray-400 mt-0.5">
                        <Calendar size={12} className="mr-1" />
                        {formatDate(conv.lastModified)}
                      </div>
                    </div>
                  </div>

                  {/* Tree View */}
                  {expandedConv === conv.convGuid && (
                    <div className="transition-all duration-300 overflow-hidden">
                      <div className="border-t border-gray-700/50 mx-2"></div>
                      <div className="py-1 px-1">
                        {isLoadingTree ? (
                          <div className="flex items-center justify-center py-4 text-gray-400 text-sm">
                            <div className="animate-spin rounded-full h-4 w-4 border-t-2 border-b-2 border-blue-500 mr-2"></div>
                            Loading messages...
                          </div>
                        ) : treeData ? (
                          <HistoricalConvTree
                            treeData={treeData}
                            onNodeClick={(nodeId) => handleNodeClick(nodeId, expandedConv!)}
                          />
                        ) : (
                          <div className="text-sm text-gray-400 p-2 text-center">No messages found</div>
                        )}
                      </div>
                    </div>
                  )}
                </div>
              ))}
            </div>
          </ScrollArea>
        )}
      </div>
    </div>
  );
};