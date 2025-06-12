import React, { useState, useEffect } from 'react';
import { Input } from '@/components/ui/input';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { ModelForm } from './ModelForm';
import {
  UnifiedModalDialog,
  UnifiedModalHeader,
  UnifiedModalContent,
  UnifiedModalFooter
} from '@/components/ui/unified-modal-dialog';
import { Pencil, Trash2, Star, PlusCircle, AlertCircle } from 'lucide-react';
import { Model } from '@/types/settings';
import { useModelManagement } from '@/hooks/useResourceManagement';
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from '@/components/ui/tooltip';

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

  
  const [internalEditingModel, setInternalEditingModel] = useState<Model | null>(null);  const [internalEditOpen, setInternalEditOpen] = useState(false);

  // Search query for filtering models
  const [searchQuery, setSearchQuery] = useState<string>('');

  
  const editingModel = externalModelToEdit !== undefined ? externalModelToEdit : internalEditingModel;
  const setEditingModel = externalSetModelToEdit || setInternalEditingModel;
  const editOpen = externalEditOpen !== undefined ? externalEditOpen : internalEditOpen;
  const setEditOpen = externalSetEditOpen || setInternalEditOpen;

  const [addOpen, setAddOpen] = useState(false);
  const [modelToDelete, setModelToDelete] = useState<Model | null>(null);
  const [deleteOpen, setDeleteOpen] = useState(false);
  const [error, setLocalError] = useState<string | null>(null);
  const [isProcessing, setIsProcessing] = useState(false);  const displayError = error || storeError;

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
    if (!addOpen && !editOpen && !deleteOpen) {
      setLocalError(null);
      clearError();
    }
  }, [addOpen, editOpen, deleteOpen, clearError]);

  const handleAddModel = async (modelData: Omit<Model, 'guid'>) => {
    setIsProcessing(true);
    setLocalError(null);
    try {
      await addModel(modelData);
      setAddOpen(false);
    } catch (err: any) {
      setLocalError(err?.message || 'Failed to add model');
    } finally {
      setIsProcessing(false);
    }
  };

  const handleUpdateModel = async (modelData: Model) => {
    setIsProcessing(true);
    setLocalError(null);
    try {
      await updateModel(modelData);
      setEditOpen(false);
    } catch (err: any) {
      setLocalError(err?.message || 'Failed to update model');
    } finally {
      setIsProcessing(false);
    }
  };

  const handleDeleteModelConfirm = async () => {
    if (!modelToDelete) return;

    setIsProcessing(true);
    setLocalError(null);
    try {
      await deleteModel(modelToDelete.guid);
      setDeleteOpen(false);
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
        <Button onClick={() => setAddOpen(true)} className="flex items-center gap-2 btn-primary self-start md:self-auto">
          <PlusCircle className="h-4 w-4" /> Add Model
        </Button>
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
                onClick={() => setAddOpen(true)}
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
        <div className="grid model-modal-grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-4">          {[...filteredModels]
            // Sort models alphabetically by friendly name (A → Z)
            .sort((a, b) => a.friendlyName.localeCompare(b.friendlyName))
            .map((model) => (
            <Card key={model.guid} className="card-base card-hover backdrop-blur-sm group flex flex-col relative">
              <div
                className="h-2 bg-gradient-to-r from-opacity-80 to-opacity-100 animate-hover group-hover:h-3"
                style={{
                  backgroundColor: model.color || '#4f46e5',
                  from: `${model.color}80` || '#4f46e580',
                  to: model.color || '#4f46e5',
                }}
              />
              <div className="p-3 flex flex-col flex-1">
                <div className="flex items-start pr-6"> {/* Add padding-right to make room for buttons */}
                  <CardTitle className="text-gray-100 text-lg flex items-center gap-1 truncate">
                    {model.friendlyName}
                    {model.starred && <Star className="h-4 w-4 flex-shrink-0 fill-yellow-400 text-yellow-400" />}
                  </CardTitle>
                </div>

                <div className="text-mono text-sm text-gray-400 truncate mb-1">{model.modelName}</div>

                <div className="mt-auto space-y-1 text-xs">
                  <div className="text-gray-400">{getProviderName(model.providerGuid)}</div>
                  
                  <div className="text-gray-400">In: ${model.input1MTokenPrice} / Out: ${model.output1MTokenPrice}</div>
                  
                  {model.supportsPrefill && (
                    <div className="text-blue-400 flex items-center gap-1">
                      <span className="inline-block w-2 h-2 rounded-full bg-blue-400"></span>
                      Prefill
                    </div>
                  )}

                  {model.userNotes && (
                    <div className="text-gray-200 italic mt-1 line-clamp-2">{model.userNotes}</div>
                  )}
                </div>

                {/* Vertical stacked buttons in the bottom-right corner */}
                <div className="absolute bottom-1 right-1 flex flex-col space-y-1">
                  <TooltipProvider>
                    <Tooltip>
                      <TooltipTrigger asChild>
                        <Button
                          variant="ghost"
                          size="icon"
                          className={`${model.starred ? 'text-yellow-400 hover:text-yellow-300' : 'text-gray-400 hover:text-yellow-400'} hover:bg-gray-700 animate-hover h-6 w-6 p-0`}
                          onClick={() => handleToggleStarred(model)}
                          disabled={isProcessing}
                        >
                          <Star className={`h-3.5 w-3.5 ${model.starred ? 'fill-yellow-400' : ''}`} />
                        </Button>
                      </TooltipTrigger>
                      <TooltipContent side="left" className="bg-gray-900 text-gray-100 text-xs border-gray-700">
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
                          className="btn-ghost icon-btn h-6 w-6 p-0"
                          onClick={() => {
                            setEditingModel(model);
                            setEditOpen(true);
                          }}
                          disabled={isProcessing}
                        >
                          <Pencil className="h-3.5 w-3.5" />
                        </Button>
                      </TooltipTrigger>
                      <TooltipContent side="left" className="bg-gray-900 text-gray-100 text-xs border-gray-700">
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
                          className="btn-danger icon-btn h-6 w-6 p-0"
                          onClick={() => {
                            setModelToDelete(model);
                            setDeleteOpen(true);
                          }}
                          disabled={isProcessing}
                        >
                          <Trash2 className="h-3.5 w-3.5" />
                        </Button>
                      </TooltipTrigger>
                      <TooltipContent side="left" className="bg-gray-900 text-gray-100 text-xs border-gray-700">
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

      
      <UnifiedModalDialog open={addOpen} onOpenChange={setAddOpen} size="xl" variant="settings">
        <UnifiedModalHeader>
          <h2 className="text-gray-100 text-xl font-semibold">Add New Model</h2>
        </UnifiedModalHeader>
        <UnifiedModalContent>
          {displayError && (
            <div className="bg-red-950/30 text-red-400 p-3 rounded-md mb-4 border border-red-800/50">
              {displayError}
            </div>
          )}
          <ModelForm providers={providers} onSubmit={handleAddModel} isProcessing={isProcessing} />
        </UnifiedModalContent>
      </UnifiedModalDialog>

      
      <UnifiedModalDialog open={editOpen} onOpenChange={setEditOpen} size="xl" variant="settings">
        <UnifiedModalHeader>
          <h2 className="text-gray-100 text-xl font-semibold">Edit Model</h2>
        </UnifiedModalHeader>
        <UnifiedModalContent>
          {displayError && (
            <div className="bg-red-950/30 text-red-400 p-3 rounded-md mb-4 border border-red-800/50">
              {displayError}
            </div>
          )}
          {editingModel && (
            <ModelForm
              key={`edit-model-form-${editingModel.guid}`}
              providers={providers}
              onSubmit={handleUpdateModel}
              isProcessing={isProcessing}
              initialValues={editingModel}
            />
          )}
        </UnifiedModalContent>
      </UnifiedModalDialog>

      
      <UnifiedModalDialog open={deleteOpen} onOpenChange={setDeleteOpen} size="xl" variant="settings">
        <UnifiedModalHeader>
          <h2 className="text-gray-100 text-xl font-semibold">Confirm Deletion</h2>
        </UnifiedModalHeader>
        <UnifiedModalContent>
          <div className="py-4 text-gray-200">
            Are you sure you want to delete the model <strong>{modelToDelete?.friendlyName}</strong>? This action cannot
            be undone.
          </div>
          {displayError && (
            <div className="bg-red-950/30 text-red-400 p-3 rounded-md mb-4 border border-red-800/50">
              {displayError}
            </div>
          )}
        </UnifiedModalContent>
        <UnifiedModalFooter className="flex justify-end space-x-2">
          <Button
            variant="outline"
            onClick={() => setDeleteOpen(false)}
            disabled={isProcessing}
            className="bg-gray-700 hover:bg-gray-600 text-gray-200 border-gray-600"
          >
            Cancel
          </Button>
          <Button
            variant="destructive"
            onClick={handleDeleteModelConfirm}
            disabled={isProcessing}
            className="bg-red-700 hover:bg-red-800 text-white border-red-900"
          >
            {isProcessing ? 'Deleting...' : 'Delete'}
          </Button>
        </UnifiedModalFooter>
      </UnifiedModalDialog>
    </>
  );
};