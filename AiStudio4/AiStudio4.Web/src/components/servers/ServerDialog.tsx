// AiStudio4.Web/src/components/servers/ServerDialog.tsx
import { useState, useEffect } from 'react';
// import { Dialog, DialogContent } from '@/components/ui/dialog'; // Removed old import
import {
  UnifiedModalDialog,
  UnifiedModalHeader,
  UnifiedModalContent,
  UnifiedModalFooter,
} from '@/components/ui/unified-modal-dialog'; // Added new import
import { useMcpServerStore } from '@/stores/useMcpServerStore';
import { Switch } from '@/components/ui/switch';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Check, X } from 'lucide-react';

export function ServerDialog() {
  const [isOpen, setIsOpen] = useState(false);
  const {
    servers,
    enabledCount,
    fetchServers,
    setServerEnabled,
  } = useMcpServerStore();

  // Listen for global event to open dialog
  useEffect(() => {
    const handleOpen = () => setIsOpen(true);
    window.addEventListener('open-server-list', handleOpen);
    return () => window.removeEventListener('open-server-list', handleOpen);
  }, []);

  // Fetch servers whenever dialog is opened
  useEffect(() => {
    if (isOpen) {
      fetchServers();
    }
  }, [isOpen, fetchServers]);

  const handleToggle = (id: string, current: boolean) => {
    setServerEnabled(id, !current);
  };

  return (
    <UnifiedModalDialog
      open={isOpen}
      onOpenChange={setIsOpen}
      variant="library" // Use library variant as a base
      size="3xl" // Corresponds to max-w-3xl
      height="lg" // Corresponds roughly to 70vh
    >
      <UnifiedModalHeader>
        <div className="flex justify-between items-center w-full">
          <h2 className="text-xl font-semibold">MCP Servers</h2>
          <span className="text-sm text-muted-foreground">Enabled: {enabledCount} / {servers.length}</span>
        </div>
      </UnifiedModalHeader>
      <UnifiedModalContent>
        <div className="space-y-3 pr-1"> {/* Removed fixed height, relies on flex-1 + overflow */}
          {servers.length === 0 ? (
            <div className="text-center text-muted-foreground pt-4">No MCP servers configured.</div>
          ) : (
            servers.map((srv) => (
              <Card key={srv.id} className="card-base">
                <CardHeader className="px-4 py-2 flex flex-row items-center justify-between">
                  <div>
                    <CardTitle className="text-lg">{srv.name}</CardTitle>
                    <p className="text-xs text-muted-foreground">{srv.description}</p>
                  </div>
                  <Switch
                    checked={srv.isEnabled}
                    onCheckedChange={() => handleToggle(srv.id, srv.isEnabled)}
                  />
                </CardHeader>
                {srv.isEnabled && (
                  <CardContent className="px-4 py-2 text-green-400 flex items-center gap-1"><Check className="h-4 w-4" /> Enabled</CardContent>
                )}
                {!srv.isEnabled && (
                  <CardContent className="px-4 py-2 text-red-400 flex items-center gap-1"><X className="h-4 w-4" /> Disabled</CardContent>
                )}
              </Card>
            ))
          )}
        </div>
      </UnifiedModalContent>
      <UnifiedModalFooter>
        <Button variant="outline" size="sm" onClick={() => setIsOpen(false)}>
          Close
        </Button>
      </UnifiedModalFooter>
    </UnifiedModalDialog>
  );
}