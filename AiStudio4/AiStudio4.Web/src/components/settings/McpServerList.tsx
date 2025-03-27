import React from 'react';
import { McpServerDefinition } from '@/types/mcpTypes';
import { useMcpServerStore } from '@/stores/useMcpServerStore';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Switch } from '@/components/ui/switch';
import { Button } from '@/components/ui/button';
import { Edit, Trash2 } from 'lucide-react';
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import McpServerForm from './McpServerForm';

interface McpServerListProps {
  servers: McpServerDefinition[];
}

const McpServerList: React.FC<McpServerListProps> = ({ servers }) => {
  const { updateServer, deleteServer } = useMcpServerStore();
  const [serverToEdit, setServerToEdit] = React.useState<McpServerDefinition | null>(null);
  const [editDialogOpen, setEditDialogOpen] = React.useState(false);
  const [confirmDeleteOpen, setConfirmDeleteOpen] = React.useState(false);
  const [serverToDelete, setServerToDelete] = React.useState<McpServerDefinition | null>(null);

  const handleToggleEnabled = (server: McpServerDefinition) => {
    updateServer({
      ...server,
      isEnabled: !server.isEnabled,
    });
  };

  const handleEditServer = (server: McpServerDefinition) => {
    setServerToEdit(server);
    setEditDialogOpen(true);
  };

  const handleDeleteServer = (server: McpServerDefinition) => {
    setServerToDelete(server);
    setConfirmDeleteOpen(true);
  };

  const confirmDelete = async () => {
    if (serverToDelete) {
      try {
        await deleteServer(serverToDelete.id);
      } catch (error) {
        console.error('Error deleting server:', error);
      } finally {
        setConfirmDeleteOpen(false);
        setServerToDelete(null);
      }
    }
  };

  if (servers.length === 0) {
    return <div className="text-center text-gray-500 py-8">No MCP servers configured</div>;
  }

  return (
    <>
      <div className="grid gap-4 grid-cols-1">
        {Array.isArray(servers) && servers.map((server) => (
          <Card key={server.id} className="bg-gray-800 border-gray-700">
            <CardHeader className="pb-2">
              <div className="flex justify-between items-start">
                <CardTitle className="text-lg">{server.name}</CardTitle>
                <div className="flex space-x-2">
                  <Button variant="ghost" size="icon" onClick={() => handleEditServer(server)}>
                    <Edit className="w-4 h-4" />
                  </Button>
                  <Button variant="ghost" size="icon" onClick={() => handleDeleteServer(server)}>
                    <Trash2 className="w-4 h-4 text-red-500" />
                  </Button>
                </div>
              </div>
            </CardHeader>
            <CardContent>
              <div className="space-y-2">
                <div className="flex justify-between items-center">
                  <span className="text-sm font-medium">Enabled</span>
                  <Switch 
                    checked={server.isEnabled} 
                    onCheckedChange={() => handleToggleEnabled(server)} 
                  />
                </div>
                <p className="text-sm text-gray-400">{server.description}</p>
                <div className="text-xs text-gray-500">
                  <p>Command: {server.command}</p>
                  <p>Arguments: {server.arguments}</p>
                </div>
              </div>
            </CardContent>
          </Card>
        ))}
      </div>

      {/* Edit Dialog */}
      <Dialog open={editDialogOpen} onOpenChange={setEditDialogOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Edit MCP Server</DialogTitle>
          </DialogHeader>
          {serverToEdit && (
            <McpServerForm 
              initialData={serverToEdit} 
              onSuccess={() => setEditDialogOpen(false)}
              onCancel={() => setEditDialogOpen(false)}
            />
          )}
        </DialogContent>
      </Dialog>

      {/* Delete Confirmation Dialog */}
      <Dialog open={confirmDeleteOpen} onOpenChange={setConfirmDeleteOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Confirm Deletion</DialogTitle>
          </DialogHeader>
          <div className="py-4">
            Are you sure you want to delete the server "{serverToDelete?.name}"? This action cannot be undone.
          </div>
          <div className="flex justify-end space-x-2">
            <Button variant="outline" onClick={() => setConfirmDeleteOpen(false)}>
              Cancel
            </Button>
            <Button variant="destructive" onClick={confirmDelete}>
              Delete
            </Button>
          </div>
        </DialogContent>
      </Dialog>
    </>
  );
};

export default McpServerList;