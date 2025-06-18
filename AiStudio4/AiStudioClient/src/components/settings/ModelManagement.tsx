import React, { useState, useEffect } from 'react';
import { Input } from '@/components/ui/input';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { ModelForm } from './ModelForm';
import { Pencil, Trash2, Star, PlusCircle, AlertCircle, Download } from 'lucide-react';
import { Model } from '@/types/settings';
import { useModelManagement } from '@/hooks/useResourceManagement';
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from '@/components/ui/tooltip';
import { useModalStore } from '@/stores/useModalStore';
import { useModelStore } from '@/stores/useModelStore';
import { toast } from '@/hooks/use-toast';

interface ModelManagementProps {
  providers: any[]; 
  
  modelToEdit?: Model | null;
  setModelToEdit?: React.Dispatch<React.SetStateAction<Model | null>>;
  editDialogOpen?: boolean;
  setEditDialogOpen?: React.Dispatch<React.SetStateAction<boolean>>;
}

export const ModelManagement: React.FC<ModelManagementProps> = ({
  providers,
  
  modelToEdit: externalModelToEdit,
  setModelToEdit: externalSetModelToEdit,
  editDialogOpen: externalEditOpen,
  setEditDialogOpen: externalSetEditOpen,
}) => {
  
  const { models, isLoading, error: storeError, addModel, updateModel, deleteModel, clearError } = useModelManagement();
  const { openModal, openNestedModal, closeModal } = useModalStore();
  const openRouterProvider = useModelStore(state => 
    state.providers.find(p => p.url.startsWith('https://openrouter.ai'))
  );

  // Search query for filtering models
  const [searchQuery, setSearchQuery] = useState<string>('');
  const [error, setLocalError] = useState<string | null>(null);
  const [isProcessing, setIsProcessing] = useState(false);
  const displayError = error || storeError;

  // Filter models based on search query (friendly name, model name, or provider name)
  const filteredModels = models.filter((m) => {
    if (!searchQuery.trim()) return true;
    const query = searchQuery.toLowerCase();
    return (
      m.friendlyName.toLowerCase().includes(query) ||
      m.modelName.toLowerCase().includes(query) ||
      getProviderName(m.providerGuid).toLowerCase().includes(query)
    );
  });

  useEffect(() => {
    setLocalError(null);
    clearError();
  }, [clearError]);

  const handleAddModel = () => {
    openNestedModal('modelForm', {
      mode: 'add',
      providers: providers,
      onSubmit: handleAddModelSubmit,
    });
  };

  const handleOpenRouterImport = () => {
    if (!openRouterProvider) {
      toast({
        title: "Provider Not Found",
        description: "Please configure a Service Provider for OpenRouter first.",
        variant: "destructive",
      });
      return;
    }
    openModal('openRouterImporter', {});
  };

  const handleAddModelSubmit = async (modelData: Omit<Model, 'guid'>) => {
    setIsProcessing(true);
    setLocalError(null);
    try {
      await addModel(modelData);
      closeModal();
    } catch (err: any) {
      setLocalError(err?.message || 'Failed to add model');
    } finally {
      setIsProcessing(false);
    }
  };

  const handleEditModel = (model: Model) => {
    openNestedModal('modelForm', {
      mode: 'edit',
      model: model,
      providers: providers,
      onSubmit: handleUpdateModelSubmit,
    });
  };

  const handleUpdateModelSubmit = async (modelData: Model) => {
    setIsProcessing(true);
    setLocalError(null);
    try {
      await updateModel(modelData);
      closeModal();
    } catch (err: any) {
      setLocalError(err?.message || 'Failed to update model');
    } finally {
      setIsProcessing(false);
    }
  };

  const handleDeleteModel = (model: Model) => {
    openNestedModal('confirmation', {
      title: 'Confirm Deletion',
      description: `Are you sure you want to delete the model "${model.friendlyName}"? This action cannot be undone.`,
      danger: true,
      onConfirm: async () => {
        await handleDeleteModelSubmit(model.guid);
      }
    });
  };

  const handleDeleteModelSubmit = async (modelGuid: string) => {
    setIsProcessing(true);
    setLocalError(null);
    try {
      await deleteModel(modelGuid);
      closeModal();
    } catch (err: any) {
      setLocalError(err?.message || 'Failed to delete model');
    } finally {
      setIsProcessing(false);
    }
  };

  
  const handleToggleStarred = async (model: Model) => {
    setIsProcessing(true);
    setLocalError(null);
    try {
      
      const updatedModel = {
        ...model,
        starred: !model.starred,
      };

      await updateModel(updatedModel);
    } catch (err: any) {
      setLocalError(err?.message || 'Failed to update star status');
    } finally {
      setIsProcessing(false);
    }
  };  function getProviderName(providerGuid: string): string {
    const provider = providers.find((p) => p.guid === providerGuid);
    return provider ? provider.friendlyName : 'Unknown Provider';
  }

  return (
    <>
      <div className="flex flex-col md:flex-row md:items-center md:justify-between mb-4 gap-2">
        <div className="flex items-center gap-2 flex-1">
          <h2 className="text-title whitespace-nowrap">AI Models</h2>
          <Input
            type="text"
            placeholder="Search models…"
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            className="w-full md:w-72 lg:w-80 bg-gray-800 text-gray-100 border-gray-600 placeholder-gray-400"
          />
        </div>
        <div className="flex items-center gap-2 self-start md:self-auto">
          <Button onClick={handleOpenRouterImport} className="flex items-center gap-2 btn-primary">
            <Download className="h-4 w-4" /> Add OpenRouter Model
          </Button>
          <Button onClick={handleAddModel} className="flex items-center gap-2 btn-primary">
            <PlusCircle className="h-4 w-4" /> Add Model
          </Button>
        </div>
      </div>

      {isLoading ? (
        <div className="flex-center py-8">
          <div className="loading-spinner h-8 w-8"></div>
        </div>
      ) : models.length === 0 ? (
        <Card className="card-base">
          <CardContent className="pt-6 text-center text-gray-400">
            <div className="flex flex-col flex-center py-8">
              <AlertCircle className="h-12 w-12 mb-4 opacity-50" />
              <p>No models configured yet.</p>
              <p className="mb-4">Add your first model to get started.</p>
              <Button
                onClick={handleAddModel}
                variant="outline"
                className="bg-gray-700 hover:bg-gray-600 text-gray-200 border-gray-600"
              >
                Add Model
              </Button>
            </div>
          </CardContent>
        </Card>      ) : filteredModels.length === 0 ? (
        <Card className="card-base">
          <CardContent className="pt-6 text-center text-gray-400">
            <div className="flex flex-col flex-center py-8">
              <AlertCircle className="h-12 w-12 mb-4 opacity-50" />
              <p>No models match your search.</p>
            </div>
          </CardContent>
        </Card>
      ) : (
        <div className="space-y-3">          {[...filteredModels]
            // Sort models alphabetically by friendly name (A → Z)
            .sort((a, b) => a.friendlyName.localeCompare(b.friendlyName))
            .map((model) => (
            <Card key={model.guid} className="card-base card-hover backdrop-blur-sm group relative">
              <div
                className="h-1 bg-gradient-to-r from-opacity-80 to-opacity-100 animate-hover group-hover:h-1.5"
                style={{
                  backgroundColor: model.color || '#4f46e5',
                  from: `${model.color}80` || '#4f46e580',
                  to: model.color || '#4f46e5',
                }}
              />
              <div className="py-1 px-4 flex items-center justify-between">
                <div className="flex items-center gap-4 flex-1 min-w-0">
                  <div className="flex flex-col min-w-0 flex-1">
                    <div className="flex items-center gap-2">
                      <CardTitle className="text-gray-100 text-lg flex items-center gap-2 truncate">
                        {model.friendlyName}
                        {model.starred && <Star className="h-4 w-4 flex-shrink-0 fill-yellow-400 text-yellow-400" />}
                      </CardTitle>
                    </div>
                    <div className="text-mono text-sm text-gray-400 truncate">{model.modelName}</div>
                  </div>
                  
                  <div className="flex items-center gap-6 text-sm flex-shrink-0">
                    <div className="text-gray-400 text-center">
                      <div className="text-xs text-gray-500">Provider</div>
                      <div className="truncate max-w-24">{getProviderName(model.providerGuid)}</div>
                    </div>
                    
                    <div className="text-gray-400 text-center">
                      <div className="text-xs text-gray-500">Pricing</div>
                      <div className="whitespace-nowrap">In: ${model.input1MTokenPrice} / Out: ${model.output1MTokenPrice}</div>
                    </div>
                    
                    {model.supportsPrefill && (
                      <div className="text-blue-400 flex items-center gap-1 flex-shrink-0">
                        <span className="inline-block w-2 h-2 rounded-full bg-blue-400"></span>
                        <span className="text-xs">Prefill</span>
                      </div>
                    )}
                  </div>
                  
                  {model.userNotes && (
                    <div className="text-gray-300 italic text-sm max-w-48 truncate flex-shrink-0" title={model.userNotes}>
                      "{model.userNotes}"
                    </div>
                  )}
                </div>

                {/* Horizontal action buttons on the right */}
                <div className="flex items-center gap-1 ml-4 flex-shrink-0">
                  <TooltipProvider>
                    <Tooltip>
                      <TooltipTrigger asChild>
                        <Button
                          variant="ghost"
                          size="icon"
                          className={`${model.starred ? 'text-yellow-400 hover:text-yellow-300' : 'text-gray-400 hover:text-yellow-400'} hover:bg-gray-700 animate-hover h-8 w-8 p-0`}
                          onClick={() => handleToggleStarred(model)}
                          disabled={isProcessing}
                        >
                          <Star className={`h-4 w-4 ${model.starred ? 'fill-yellow-400' : ''}`} />
                        </Button>
                      </TooltipTrigger>
                      <TooltipContent side="top" className="bg-gray-900 text-gray-100 text-xs border-gray-700">
                        {model.starred ? 'Remove from favorites' : 'Add to favorites'}
                      </TooltipContent>
                    </Tooltip>
                  </TooltipProvider>
                  
                  <TooltipProvider>
                    <Tooltip>
                      <TooltipTrigger asChild>
                        <Button
                          variant="ghost"
                          size="icon"
                          className="btn-ghost icon-btn h-8 w-8 p-0"
                          onClick={() => handleEditModel(model)}
                          disabled={isProcessing}
                        >
                          <Pencil className="h-4 w-4" />
                        </Button>
                      </TooltipTrigger>
                      <TooltipContent side="top" className="bg-gray-900 text-gray-100 text-xs border-gray-700">
                        Edit model
                      </TooltipContent>
                    </Tooltip>
                  </TooltipProvider>
                  
                  <TooltipProvider>
                    <Tooltip>
                      <TooltipTrigger asChild>
                        <Button
                          variant="ghost"
                          size="icon"
                          className="btn-danger icon-btn h-8 w-8 p-0"
                          onClick={() => handleDeleteModel(model)}
                          disabled={isProcessing}
                        >
                          <Trash2 className="h-4 w-4" />
                        </Button>
                      </TooltipTrigger>
                      <TooltipContent side="top" className="bg-gray-900 text-gray-100 text-xs border-gray-700">
                        Delete model
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