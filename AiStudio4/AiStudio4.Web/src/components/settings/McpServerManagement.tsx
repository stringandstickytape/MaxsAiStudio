import React, { useEffect } from 'react';
import { useMcpServerStore } from '@/stores/useMcpServerStore';
import { Button } from '@/components/ui/button';
import { Plus, AlertCircle } from 'lucide-react';
import McpServerList from './McpServerList';
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import McpServerForm from './McpServerForm';
import { Card, CardContent } from '@/components/ui/card';

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
      <div className="flex-between mb-4">
        <h2 className="text-title">MCP Servers</h2>
        <Button onClick={handleAddServer} className="flex items-center gap-2 btn-primary">
          <Plus className="w-4 h-4" />
          Add Server
        </Button>
      </div>
      
      <div className="text-sm text-gray-400 mb-4">
        Model Context Protocol (MCP) servers provide external tools that can be used by AI models.
      </div>
      
      {servers.length === 0 ? (
        <Card className="card-base">
          <CardContent className="pt-6 text-center text-gray-400">
            <div className="flex flex-col flex-center py-8">
              <AlertCircle className="h-12 w-12 mb-4 opacity-50" />
              <p>No MCP servers configured yet.</p>
              <p className="mb-4">Add your first MCP server to get started.</p>
              <Button
                onClick={handleAddServer}
                variant="outline"
                className="bg-gray-700 hover:bg-gray-600 text-gray-200 border-gray-600"
              >
                Add Server
              </Button>
            </div>
          </CardContent>
        </Card>
      ) : (
        <McpServerList servers={servers} />
      )}
      
      {/* Add Server Dialog */}
      <Dialog open={addDialogOpen} onOpenChange={setAddDialogOpen}>
        <DialogContent className="max-w-xl bg-gray-800 border-gray-700 text-gray-100" description="Add a new MCP server">
          <DialogHeader>
            <DialogTitle className="text-gray-100">Add MCP Server</DialogTitle>
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