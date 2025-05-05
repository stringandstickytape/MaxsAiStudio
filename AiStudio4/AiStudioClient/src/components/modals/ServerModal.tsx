// AiStudioClient/src/components/modals/ServerModal.tsx
import React, { useEffect, useState } from 'react';
import {
  UnifiedModalDialog,
  UnifiedModalHeader,
  UnifiedModalContent,
  UnifiedModalFooter,
} from '@/components/ui/unified-modal-dialog';
import { useModalStore, ModalRegistry } from '@/stores/useModalStore';
import { McpServerDefinition, useMcpServerStore } from '@/stores/useMcpServerStore';
import { Switch } from '@/components/ui/switch';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Check, X, Search, PlusCircle, RefreshCw, CheckSquare, Square, Edit, Trash2 } from 'lucide-react';
import { Input } from '@/components/ui/input';
import { cn } from '@/lib/utils';
import { ServerFormDialog } from '@/components/servers/ServerFormDialog';
import { DeleteServerDialog } from '@/components/servers/DeleteServerDialog';

type ServerModalProps = ModalRegistry['server'];

type ServerCategory = {
  id: string;
  name: string;
};

// Define themeable properties for the component
export const themeableProps = {};

export function ServerModal() {
  const { openModalId, modalProps, closeModal } = useModalStore();
  const isOpen = openModalId === 'server';
  const props = isOpen ? (modalProps as ServerModalProps) : null;

  const {
    servers,
    enabledCount,
    fetchServers,
    setServerEnabled,
  } = useMcpServerStore();

  const [searchTerm, setSearchTerm] = useState('');
  const [selectedCategory, setSelectedCategory] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [isFormDialogOpen, setIsFormDialogOpen] = useState(false);
  const [isDeleteDialogOpen, setIsDeleteDialogOpen] = useState(false);
  const [selectedServer, setSelectedServer] = useState<McpServerDefinition | null>(null);
  const [formMode, setFormMode] = useState<'add' | 'edit'>('add');

  // Fetch servers whenever modal is opened
  useEffect(() => {
    if (isOpen) {
      handleRefreshServers();
    }
  }, [isOpen]);

  if (!isOpen) return null;

  const handleToggle = (id: string, current: boolean) => {
    setServerEnabled(id, !current);
  };

  const handleRefreshServers = async () => {
    try {
      setIsLoading(true);
      await fetchServers();
    } catch (error) {
      console.error('Error refreshing servers:', error);
    } finally {
      setIsLoading(false);
    }
  };

  const handleAddServer = () => {
    setSelectedServer(null);
    setFormMode('add');
    setIsFormDialogOpen(true);
  };

  const handleEditServer = (server: McpServerDefinition) => {
    setSelectedServer(server);
    setFormMode('edit');
    setIsFormDialogOpen(true);
  };

  const handleDeleteServer = (server: McpServerDefinition) => {
    setSelectedServer(server);
    setIsDeleteDialogOpen(true);
  };

  // Filter servers based on search term and category
  const filteredServers = servers.filter((server) => {
    const matchesSearch =
      searchTerm === '' ||
      server.name.toLowerCase().includes(searchTerm.toLowerCase()) ||
      (server.description && server.description.toLowerCase().includes(searchTerm.toLowerCase()));

    // Filter by selected category
    const matchesCategory = 
      selectedCategory === null || 
      (server.categories && server.categories.includes(selectedCategory));

    return matchesSearch && matchesCategory;
  });

  return (
    <>
      <UnifiedModalDialog
        open={isOpen}
        onOpenChange={(open) => !open && closeModal()}
        variant="library"
        size="3xl"
        height="lg"
        className="ServerModal"
        style={{
          backgroundColor: 'var(--global-background-color)',
          color: 'var(--global-text-color)',
          fontFamily: 'var(--global-font-family)',
          fontSize: 'var(--global-font-size)'
        }}
      >
        <UnifiedModalHeader style={{ backgroundColor: 'var(--global-background-color)' }}>
          <div className="flex justify-between items-center w-full" style={{ backgroundColor: 'var(--global-background-color)', color: 'var(--global-text-color)' }}>
            <h2 className="text-title" style={{ color: 'var(--global-text-color)' }}>MCP Servers</h2>
            <div className="flex space-x-2 pr-16" style={{ backgroundColor: 'var(--global-background-color)' }}>
              <Button
                variant="outline"
                size="sm"
                onClick={handleRefreshServers}
                className="btn-primary bg-blue-600/30 hover:bg-blue-500/30 border-blue-500/50 flex items-center space-x-1"
                disabled={isLoading}
                style={{
                  backgroundColor: 'var(--global-background-color)',
                  borderColor: 'var(--global-border-color)',
                  color: 'var(--global-text-color)'
                }}
              >
                <RefreshCw className="h-4 w-4 mr-1" />
                {isLoading ? 'Refreshing...' : 'Refresh'}
              </Button>
              <Button
                variant="outline"
                size="sm"
                className="btn-primary bg-blue-600/30 hover:bg-blue-500/30 border-blue-500/50 flex items-center space-x-1"
                onClick={handleAddServer}
                style={{
                  backgroundColor: 'var(--global-primary-color)',
                  color: '#ffffff'
                }}
              >
                <PlusCircle className="h-4 w-4 mr-1" />
                <span>New Server</span>
              </Button>
            </div>
          </div>
        </UnifiedModalHeader>
        <UnifiedModalContent style={{ backgroundColor: 'var(--global-background-color)' }}>
          <div className="grid grid-cols-1 md:grid-cols-4 gap-4 mb-4" style={{ backgroundColor: 'var(--global-background-color)' }}>
            {/* Left sidebar for categories - for future use */}
            <div className="md:col-span-1" style={{ backgroundColor: 'var(--global-background-color)' }}>
              <div className="text-sm text-gray-400 mb-2" style={{ color: 'var(--global-secondary-color)' }}>
                <span className="mr-2">Enabled: {enabledCount} / {servers.length} servers</span>
              </div>
              <Card className="card-base" style={{ backgroundColor: 'var(--global-background-color)', borderColor: 'var(--global-border-color)', borderRadius: 'var(--global-border-radius)', boxShadow: 'var(--global-box-shadow)' }}>
                <CardHeader style={{ backgroundColor: 'var(--global-background-color)' }}>
                  <CardTitle className="text-sm font-medium text-gray-300" style={{ color: 'var(--global-text-color)' }}>Categories</CardTitle>
                </CardHeader>
                <CardContent className="p-2" style={{ backgroundColor: 'var(--global-background-color)' }}>
                  <div className="space-y-1" style={{ backgroundColor: 'var(--global-background-color)' }}>
                    <Button
                      variant="outline"
                      size="sm"
                      className={cn(
                        "w-full justify-start",
                        selectedCategory === null 
                          ? "bg-blue-600/30 hover:bg-blue-500/30 border-blue-500/50 text-white" 
                          : "bg-blue-900/20 hover:bg-blue-500/30 border-blue-800/30 text-gray-300 hover:text-white"
                      )}
                      onClick={() => setSelectedCategory(null)}
                      style={{
                        backgroundColor: selectedCategory === null ? 'var(--global-primary-color)' : 'var(--global-background-color)',
                        borderColor: 'var(--global-border-color)',
                        color: selectedCategory === null ? '#ffffff' : 'var(--global-text-color)'
                      }}
                    >
                      All Servers
                    </Button>
                    {/* Dynamic category buttons based on server categories */}
                    {servers
                      .flatMap(server => server.categories || [])
                      .filter((category, index, self) => category && self.indexOf(category) === index) // Get unique categories
                      .sort()
                      .map((category) => (
                        <Button
                          key={category}
                          variant="outline"
                          size="sm"
                          className={cn(
                            "w-full justify-start", 
                            selectedCategory === category 
                              ? "bg-blue-600/30 hover:bg-blue-500/30 border-blue-500/50 text-white" 
                              : "bg-blue-900/20 hover:bg-blue-500/30 border-blue-800/30 text-gray-300 hover:text-white"
                          )}
                          onClick={() => setSelectedCategory(category)}
                          style={{
                            backgroundColor: selectedCategory === category ? 'var(--global-primary-color)' : 'var(--global-background-color)',
                            borderColor: 'var(--global-border-color)',
                            color: selectedCategory === category ? '#ffffff' : 'var(--global-text-color)'
                          }}
                        >
                          {category}
                        </Button>
                      ))}
                  </div>
                </CardContent>
              </Card>
            </div>

            {/* Main content area */}
            <div className="md:col-span-3" style={{ backgroundColor: 'var(--global-background-color)' }}>
              <div className="mb-4" style={{ backgroundColor: 'var(--global-background-color)' }}>
                <div className="relative" style={{ backgroundColor: 'var(--global-background-color)' }}>
                  <Search className="absolute left-2 top-2.5 h-4 w-4 text-gray-500" />
                  <Input
                    placeholder="Search servers..."
                    value={searchTerm}
                    onChange={(e) => setSearchTerm(e.target.value)}
                    className="pl-8 input-base"
                    style={{
                      backgroundColor: 'var(--global-background-color)',
                      borderColor: 'var(--global-border-color)',
                      color: 'var(--global-text-color)'
                    }}
                  />
                </div>
              </div>

              {isLoading ? (
                <div className="flex-center h-32" style={{ backgroundColor: 'var(--global-background-color)' }}>
                  <div className="loading-spinner h-8 w-8"></div>
                </div>
              ) : filteredServers.length === 0 ? (
                <div className="text-center p-8 text-subtitle" style={{ backgroundColor: 'var(--global-background-color)', color: 'var(--global-secondary-color)' }}>
                  {searchTerm ? 'No servers match your search' : 'No servers configured'}
                </div>
              ) : (
                <div className="space-y-3" style={{ backgroundColor: 'var(--global-background-color)' }}>
                  {filteredServers.map((server) => (
                    <Card 
                      key={server.id} 
                      className={`card-base ${server.isEnabled ? 'border-blue-500 border-2' : ''}`}
                      style={{
                        backgroundColor: 'var(--global-background-color)',
                        color: 'var(--global-text-color)',
                        borderRadius: 'var(--global-border-radius)',
                        boxShadow: 'var(--global-box-shadow)',
                        border: server.isEnabled ? `2px solid var(--global-primary-color)` : `1px solid var(--global-border-color)`
                      }}
                    >
                      <CardContent className="p-4" style={{ backgroundColor: 'var(--global-background-color)' }}>
                        <div className="flex justify-between items-start" style={{ backgroundColor: 'var(--global-background-color)' }}>
                          <div style={{ backgroundColor: 'var(--global-background-color)' }}>
                            <div className="flex items-center gap-2" style={{ backgroundColor: 'var(--global-background-color)' }}>
                              <h3 className="text-title text-lg" style={{ color: 'var(--global-text-color)' }}>{server.name}</h3>
                            </div>
                            <p className="text-body" style={{ color: 'var(--global-text-color)' }}>{server.description}</p>
                            {/* Server metadata/tags */}
                            <div className="flex flex-wrap gap-1 mt-2" style={{ backgroundColor: 'var(--global-background-color)' }}>
                              <span className="text-xs px-2 py-1 bg-gray-700 rounded-full" style={{ backgroundColor: server.isEnabled ? 'var(--global-primary-color)' : 'var(--global-background-color)', borderColor: 'var(--global-border-color)', color: '#ffffff', borderRadius: 'var(--global-border-radius)' }}>
                                {server.isEnabled ? 'Enabled' : 'Disabled'}
                              </span>
                              {server.command && (
                                <span className="text-xs px-2 py-1 bg-gray-700 rounded-full" style={{ backgroundColor: 'var(--global-background-color)', borderColor: 'var(--global-border-color)', color: 'var(--global-text-color)', borderRadius: 'var(--global-border-radius)' }}>
                                  {server.command.startsWith('http') ? 'HTTP' : 'CLI'}
                                </span>
                              )}
                              {server.categories?.map((category) => (
                                <span key={category} className="text-xs px-2 py-1 bg-blue-900/50 rounded-full" style={{ backgroundColor: 'var(--global-secondary-color)', color: '#ffffff', borderRadius: 'var(--global-border-radius)' }}>
                                  {category}
                                </span>
                              ))}
                            </div>
                          </div>
                          <div className="flex flex-col gap-2" style={{ backgroundColor: 'var(--global-background-color)' }}>
                            <div className="flex space-x-1" style={{ backgroundColor: 'var(--global-background-color)' }}>
                              <Button
                                variant="ghost"
                                size="icon"
                                className="h-8 w-8 text-gray-400 hover:text-gray-100"
                                title="Edit Server"
                                onClick={() => handleEditServer(server)}
                                style={{
                                  backgroundColor: 'var(--global-background-color)',
                                  color: 'var(--global-text-color)'
                                }}
                              >
                                <Edit className="h-4 w-4" />
                              </Button>
                              <Button
                                variant="ghost"
                                size="icon"
                                className="h-8 w-8 text-gray-400 hover:text-red-400"
                                title="Delete Server"
                                onClick={() => handleDeleteServer(server)}
                                style={{
                                  backgroundColor: 'var(--global-background-color)',
                                  color: 'var(--global-text-color)'
                                }}
                              >
                                <Trash2 className="h-4 w-4" />
                              </Button>
                            </div>
                            <Button
                              variant={server.isEnabled ? "default" : "outline"}
                              size="sm"
                              onClick={() => handleToggle(server.id, server.isEnabled)}
                              className={`w-full ${server.isEnabled ? 
                                'bg-blue-600 hover:bg-blue-700 text-white' : 
                                'bg-gray-700 hover:bg-gray-600 border-gray-600'
                              }`}
                              style={{
                                backgroundColor: server.isEnabled ? 'var(--global-primary-color)' : 'var(--global-background-color)',
                                borderColor: 'var(--global-border-color)',
                                color: server.isEnabled ? '#ffffff' : 'var(--global-text-color)'
                              }}
                            >
                              {server.isEnabled ? (
                                <>
                                  <Check className="h-4 w-4 mr-1" />
                                  <span>Enabled</span>
                                </>
                              ) : (
                                <>
                                  <X className="h-4 w-4 mr-1" />
                                  <span>Disabled</span>
                                </>
                              )}
                            </Button>
                          </div>
                        </div>
                      </CardContent>
                    </Card>
                  ))}
                </div>
              )}
            </div>
          </div>
        </UnifiedModalContent>
        <UnifiedModalFooter style={{ backgroundColor: 'var(--global-background-color)' }}>
          <Button 
            variant="outline" 
            size="sm" 
            onClick={() => closeModal()}
            style={{
              backgroundColor: 'var(--global-background-color)',
              borderColor: 'var(--global-border-color)',
              color: 'var(--global-text-color)'
            }}
          >
            Close
          </Button>
        </UnifiedModalFooter>
      </UnifiedModalDialog>

      {/* Server Form Dialog */}
      <ServerFormDialog
        isOpen={isFormDialogOpen}
        onClose={() => {
          setIsFormDialogOpen(false);
          handleRefreshServers(); // Refresh the list after adding/editing
        }}
        server={selectedServer || undefined}
        mode={formMode}
      />

      {/* Delete Confirmation Dialog */}
      {selectedServer && (
        <DeleteServerDialog
          isOpen={isDeleteDialogOpen}
          onClose={() => {
            setIsDeleteDialogOpen(false);
            handleRefreshServers(); // Refresh the list after deletion
          }}
          serverId={selectedServer.id}
          serverName={selectedServer.name}
        />
      )}
    </>
  );
}