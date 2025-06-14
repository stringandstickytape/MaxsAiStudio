import React, { useState, useEffect } from 'react';
import { ServiceProvider } from '@/types/settings';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { ServiceProviderForm } from './ServiceProviderForm';
import { Pencil, Trash2, PlusCircle, AlertCircle } from 'lucide-react';
import { useModelManagement } from '@/hooks/useResourceManagement';
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from '@/components/ui/tooltip';
import * as LobehubIcons from '@lobehub/icons';
import { useModalStore } from '@/stores/useModalStore';

interface ServiceProviderManagementProps {
  providers: ServiceProvider[];
  
  providerToEdit?: ServiceProvider | null;
  setProviderToEdit?: React.Dispatch<React.SetStateAction<ServiceProvider | null>>;
  editDialogOpen?: boolean;
  setEditDialogOpen?: React.Dispatch<React.SetStateAction<boolean>>;
}

export const ServiceProviderManagement: React.FC<ServiceProviderManagementProps> = ({
  providers,
  
  providerToEdit: externalProviderToEdit,
  setProviderToEdit: externalSetProviderToEdit,
  editDialogOpen: externalEditOpen,
  setEditDialogOpen: externalSetEditOpen,
}) => {
  
  const { addProvider, updateProvider, deleteProvider, error: storeError, clearError } = useModelManagement();
  const { openModal, closeModal } = useModalStore();

  const [isProcessing, setIsProcessing] = useState(false);
  const [error, setLocalError] = useState<string | null>(null);

  const displayError = error || storeError;

  useEffect(() => {
    setLocalError(null);
    clearError();
  }, [clearError]);

  const handleAddProvider = () => {
    openModal('providerForm', {
      mode: 'add',
      onSubmit: handleAddProviderSubmit,
    });
  };

  const handleAddProviderSubmit = async (providerData: Omit<ServiceProvider, 'guid'>) => {
    setIsProcessing(true);
    setLocalError(null);
    try {
      await addProvider(providerData);
      closeModal();
    } catch (err) {
      setLocalError(err instanceof Error ? err.message : 'Failed to add provider');
    } finally {
      setIsProcessing(false);
    }
  };

  const handleEditProvider = (provider: ServiceProvider) => {
    openModal('providerForm', {
      mode: 'edit',
      provider: provider,
      onSubmit: handleUpdateProviderSubmit,
    });
  };

  const handleUpdateProviderSubmit = async (providerData: ServiceProvider) => {
    setIsProcessing(true);
    setLocalError(null);
    try {
      await updateProvider(providerData);
      closeModal();
    } catch (err) {
      setLocalError(err instanceof Error ? err.message : 'Failed to update provider');
    } finally {
      setIsProcessing(false);
    }
  };

  const handleDeleteProvider = (provider: ServiceProvider) => {
    openModal('confirmation', {
      title: 'Confirm Deletion',
      description: `Are you sure you want to delete the provider "${provider.friendlyName}"? This action cannot be undone. Models associated with this provider may stop working.`,
      danger: true,
      onConfirm: async () => {
        await handleDeleteProviderSubmit(provider.guid);
      }
    });
  };

  const handleDeleteProviderSubmit = async (providerGuid: string) => {
    setIsProcessing(true);
    setLocalError(null);
    try {
      await deleteProvider(providerGuid);
      closeModal();
    } catch (err) {
      setLocalError(err instanceof Error ? err.message : 'Failed to delete provider');
    } finally {
      setIsProcessing(false);
    }
  };

  return (
    <>
      <div className="flex-between mb-4">
        <h2 className="text-title">Service Providers</h2>
        <Button
          onClick={handleAddProvider}
          className="flex items-center gap-2 bg-blue-600 hover:bg-blue-700 text-white"
        >
          <PlusCircle className="h-4 w-4" /> Add Provider
        </Button>
      </div>

      {providers.length === 0 ? (
        <Card className="card-base">
          <CardContent className="pt-6 text-center text-gray-400">
            <div className="flex flex-col flex-center py-8">
              <AlertCircle className="h-12 w-12 mb-4 opacity-50" />
              <p>No service providers configured yet.</p>
              <p className="mb-4">Add your first provider to get started.</p>
              <Button
                onClick={handleAddProvider}
                variant="outline"
                className="bg-gray-700 hover:bg-gray-600 text-gray-200 border-gray-600"
              >
                Add Provider
              </Button>
            </div>
          </CardContent>
        </Card>
      ) : (
                  <div className="grid server-modal-grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-4">
          {[...providers]
            .sort((a, b) => a.friendlyName.localeCompare(b.friendlyName))
            .map((provider) => (
            <Card key={provider.guid} className="card-base card-hover backdrop-blur-sm group flex flex-col relative">
              <div
                className="h-2 bg-gradient-to-r from-opacity-80 to-opacity-100 transition-all duration-300 group-hover:h-3"
                style={{
                  backgroundColor: '#4f46e5',
                  from: '#4f46e580',
                  to: '#4f46e5',
                }}
              />
              <div className="p-3 flex flex-col flex-1">
                <div className="flex items-start pr-6">
                  <CardTitle className="text-gray-100 text-lg flex items-center gap-2 truncate">
                    {provider.iconName && LobehubIcons[provider.iconName] ? (
                      <div className="flex items-center justify-center w-6 h-6">
                        {React.createElement(LobehubIcons[provider.iconName].Avatar, { size: 24 })}
                      </div>
                    ) : null}
                    {provider.friendlyName}
                  </CardTitle>
                </div>
                
                <div className="text-mono text-sm text-gray-400 truncate mb-1">Protocol: {provider.serviceName}</div>
                
                <div className="mt-auto space-y-1 text-xs">
                  <div className="text-gray-200 overflow-hidden text-ellipsis">
                    {provider.url}
                  </div>
                  {/* Charging Strategy Badge */}
                  <div className="pt-1">
                    <span className="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-gray-700 text-gray-200">
                      Cache Pricing: {provider.chargingStrategy || 'Claude'}
                    </span>
                  </div>
                </div>

                {/* Vertical stacked buttons in the bottom-right corner */}
                <div className="absolute bottom-1 right-1 flex flex-col space-y-1">
                  <TooltipProvider>
                    <Tooltip>
                      <TooltipTrigger asChild>
                        <Button
                          variant="ghost"
                          size="icon"
                          className="text-gray-400 hover:text-gray-100 hover:bg-gray-700 animate-hover h-6 w-6 p-0"
                          onClick={() => handleEditProvider(provider)}
                          disabled={isProcessing}
                        >
                          <Pencil className="h-3.5 w-3.5" />
                        </Button>
                      </TooltipTrigger>
                      <TooltipContent side="left" className="bg-gray-900 text-gray-100 text-xs border-gray-700">
                        Edit provider
                      </TooltipContent>
                    </Tooltip>
                  </TooltipProvider>
                  
                  <TooltipProvider>
                    <Tooltip>
                      <TooltipTrigger asChild>
                        <Button
                          variant="ghost"
                          size="icon"
                          className="text-red-400 hover:text-red-300 hover:bg-red-900/20 animate-hover h-6 w-6 p-0 service-provider-delete-button"
                          onClick={() => handleDeleteProvider(provider)}
                          disabled={isProcessing}
                        >
                          <Trash2 className="h-3.5 w-3.5" />
                        </Button>
                      </TooltipTrigger>
                      <TooltipContent side="left" className="bg-gray-900 text-gray-100 text-xs border-gray-700">
                        Delete provider
                      </TooltipContent>
                    </Tooltip>
                  </TooltipProvider>
                </div>
              </div>
            </Card>
          ))}
        </div>
      )}

      {displayError && (
        <div className="bg-red-950/30 text-red-400 p-3 rounded-md mb-4 border border-red-800/50">
          {displayError}
        </div>
      )}
    </>
  );
};