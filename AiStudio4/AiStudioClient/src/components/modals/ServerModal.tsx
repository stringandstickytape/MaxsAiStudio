// AiStudioClient/src/components/modals/ServerModal.tsx
import React, { useEffect } from 'react';
import {
  UnifiedModalDialog,
  UnifiedModalHeader,
  UnifiedModalContent,
  UnifiedModalFooter,
} from '@/components/ui/unified-modal-dialog';
import { useModalStore, ModalRegistry } from '@/stores/useModalStore';
import { useMcpServerStore } from '@/stores/useMcpServerStore';
import { Switch } from '@/components/ui/switch';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Check, X } from 'lucide-react';

type ServerModalProps = ModalRegistry['server'];

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

  // Fetch servers whenever modal is opened
  useEffect(() => {
    if (isOpen) {
      fetchServers();
    }
  }, [isOpen, fetchServers]);

  if (!isOpen) return null;

  const handleToggle = (id: string, current: boolean) => {
    setServerEnabled(id, !current);
  };

  return (
    <UnifiedModalDialog
      open={isOpen}
      onOpenChange={(open) => !open && closeModal()}
      variant="library"
      size="3xl"
      height="lg"
    >
      <UnifiedModalHeader>
        <div className="flex justify-between items-center w-full">
          <h2 className="text-xl font-semibold">MCP Servers</h2>
          <span className="text-sm text-muted-foreground">Enabled: {enabledCount} / {servers.length}</span>
        </div>
      </UnifiedModalHeader>
      <UnifiedModalContent>
        <div className="space-y-3 pr-1">
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
        <Button variant="outline" size="sm" onClick={() => closeModal()}>
          Close
        </Button>
      </UnifiedModalFooter>
    </UnifiedModalDialog>
  );
}