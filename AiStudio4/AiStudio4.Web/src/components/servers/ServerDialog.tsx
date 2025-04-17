// AiStudio4.Web/src/components/servers/ServerDialog.tsx
import { useState, useEffect } from 'react';
import { Dialog, DialogContent } from '@/components/ui/dialog';
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
    <Dialog open={isOpen} onOpenChange={setIsOpen}>
      <DialogContent className="bg-gray-900 border-gray-700 text-gray-100 max-w-3xl h-[70vh] p-4 overflow-hidden">
        <div className="flex justify-between items-center mb-4">
          <h2 className="text-2xl font-semibold">MCP Servers</h2>
          <span className="text-sm text-gray-400">Enabled: {enabledCount} / {servers.length}</span>
        </div>
        <div className="space-y-3 overflow-y-auto h-[calc(100%-3rem)] pr-1">
          {servers.length === 0 ? (
            <div className="text-center text-gray-400">No MCP servers configured.</div>
          ) : (
            servers.map((srv) => (
              <Card key={srv.id} className="card-base">
                <CardHeader className="px-4 py-2 flex flex-row items-center justify-between">
                  <div>
                    <CardTitle className="text-lg">{srv.name}</CardTitle>
                    <p className="text-xs text-gray-400">{srv.description}</p>
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
        <div className="pt-4 flex justify-end">
          <Button variant="outline" size="sm" onClick={() => setIsOpen(false)}>
            Close
          </Button>
        </div>
      </DialogContent>
    </Dialog>
  );
}