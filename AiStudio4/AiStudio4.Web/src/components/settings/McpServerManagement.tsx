import React, { useEffect } from 'react';
import { useMcpServerStore } from '@/stores/useMcpServerStore';
import { Button } from '@/components/ui/button';
import { Plus } from 'lucide-react';
import McpServerList from './McpServerList';
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import McpServerForm from './McpServerForm';

const McpServerManagement: React.FC = () => {
  const { servers, loading, error, fetchServers } = useMcpServerStore();
  const [addDialogOpen, setAddDialogOpen] = React.useState(false);

  useEffect(() => {
    fetchServers().catch(error => {
      console.error('Error fetching MCP servers:', error);
    });
  }, [fetchServers]);

  const handleAddServer = () => {
    setAddDialogOpen(true);
  };

  if (loading && servers.length === 0) {
    return <div className="flex-center h-32 p-4">
      <div className="loading-spinner h-8 w-8"></div>
    </div>;
  }

  if (error) {
    return <div className="text-red-400 p-4 bg-red-950/30 border border-red-800/50 rounded-md m-4">
      Failed to load MCP servers: {error}
      <Button className="mt-2 w-full" variant="outline" onClick={() => fetchServers()}>
        Retry
      </Button>
    </div>;
  }

  return (
    <div className="space-y-4">
      <div className="flex justify-between items-center">
        <h2 className="text-xl font-bold text-gray-200">MCP Servers</h2>
        <Button onClick={handleAddServer} size="sm">
          <Plus className="w-4 h-4 mr-2" />
          Add Server
        </Button>
      </div>
      
      <div className="text-sm text-gray-400 mb-4">
        Model Context Protocol (MCP) servers provide external tools that can be used by AI models.
      </div>
      
      <McpServerList servers={servers} />
      
      {/* Add Server Dialog */}
      <Dialog open={addDialogOpen} onOpenChange={setAddDialogOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Add MCP Server</DialogTitle>
          </DialogHeader>
          <McpServerForm 
            onSuccess={() => setAddDialogOpen(false)}
            onCancel={() => setAddDialogOpen(false)}
          />
        </DialogContent>
      </Dialog>
    </div>
  );
};

export default McpServerManagement;