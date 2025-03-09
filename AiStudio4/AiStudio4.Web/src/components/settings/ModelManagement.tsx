// src/components/settings/ModelManagement.tsx
import React, { useState, useEffect } from 'react';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { ModelForm } from './ModelForm';
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { Pencil, Trash2, Star, PlusCircle, AlertCircle } from 'lucide-react';
import { Model } from '@/types/settings';
import { useModelManagement } from '@/hooks/useModelManagement';

interface ModelManagementProps {
    providers: any[]; // We'll still get providers as props for now
    // New props for external control
    modelToEdit?: Model | null;
    setModelToEdit?: React.Dispatch<React.SetStateAction<Model | null>>;
    editDialogOpen?: boolean;
    setEditDialogOpen?: React.Dispatch<React.SetStateAction<boolean>>;
}

export const ModelManagement: React.FC<ModelManagementProps> = ({
    providers,
    // Use provided state or internal state
    modelToEdit: externalModelToEdit,
    setModelToEdit: externalSetModelToEdit,
    editDialogOpen: externalEditOpen,
    setEditDialogOpen: externalSetEditOpen
}) => {
    // Use model management hook
    const {
        models,
        isLoading,
        error: storeError,
        addModel,
        updateModel,
        deleteModel,
        clearError
    } = useModelManagement();

    // Create internal state if external state is not provided
    const [internalEditingModel, setInternalEditingModel] = useState<Model | null>(null);
    const [internalEditOpen, setInternalEditOpen] = useState(false);

    // Use either external or internal state
    const editingModel = externalModelToEdit !== undefined ? externalModelToEdit : internalEditingModel;
    const setEditingModel = externalSetModelToEdit || setInternalEditingModel;
    const editOpen = externalEditOpen !== undefined ? externalEditOpen : internalEditOpen;
    const setEditOpen = externalSetEditOpen || setInternalEditOpen;

    const [addOpen, setAddOpen] = useState(false);
    const [modelToDelete, setModelToDelete] = useState<Model | null>(null);
    const [deleteOpen, setDeleteOpen] = useState(false);
    const [error, setLocalError] = useState<string | null>(null);
    const [isProcessing, setIsProcessing] = useState(false);

    // Combine errors from store and local state
    const displayError = error || storeError;

    // Clear error when dialog closes
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

    // Handle toggling the starred status directly
    const handleToggleStarred = async (model: Model) => {
        setIsProcessing(true);
        setLocalError(null);
        try {
            // Create updated model with toggled starred value
            const updatedModel = {
                ...model,
                starred: !model.starred
            };

            await updateModel(updatedModel);
        } catch (err: any) {
            setLocalError(err?.message || 'Failed to update star status');
        } finally {
            setIsProcessing(false);
        }
    };

    const getProviderName = (providerGuid: string): string => {
        const provider = providers.find(p => p.guid === providerGuid);
        return provider ? provider.friendlyName : 'Unknown Provider';
    };

    return (
        <>
            <div className="flex justify-between items-center mb-4">
                <h2 className="text-xl font-semibold text-gray-100">AI Models</h2>
                <Button
                    onClick={() => setAddOpen(true)}
                    className="flex items-center gap-2 bg-blue-600 hover:bg-blue-700 text-white"
                >
                    <PlusCircle className="h-4 w-4" /> Add Model
                </Button>
            </div>

            {isLoading ? (
                <div className="flex justify-center py-8">
                    <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-500"></div>
                </div>
            ) : models.length === 0 ? (
                <Card className="bg-gray-800 border-gray-700">
                    <CardContent className="pt-6 text-center text-gray-400">
                        <div className="flex flex-col items-center justify-center py-8">
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
                </Card>
            ) : (
                <div className="grid grid-cols-1 gap-4">
                    {models.map(model => (
                        <Card
                            key={model.guid}
                            className="overflow-hidden bg-gray-800/80 border-gray-700/50 shadow-lg hover:shadow-xl transition-all duration-300 hover:-translate-y-1 backdrop-blur-sm group"
                        >
                            <div
                                className="h-2 bg-gradient-to-r from-opacity-80 to-opacity-100 transition-all duration-300 group-hover:h-3"
                                style={{
                                    backgroundColor: model.color || '#4f46e5',
                                    from: `${model.color}80` || '#4f46e580',
                                    to: model.color || '#4f46e5'
                                }}
                            />
                            <CardHeader className="pb-2 pt-4 px-4">
                                <div className="flex justify-between items-start">
                                    <div className="flex items-center gap-3">
                                        <div>
                                            <CardTitle className="flex items-center gap-2 text-gray-100 text-lg">
                                                {model.friendlyName}
                                                {model.starred && <Star className="h-4 w-4 fill-yellow-400 text-yellow-400" />}
                                            </CardTitle>
                                            <CardDescription className="text-gray-400 font-mono text-xs">{model.modelName}</CardDescription>
                                        </div>
                                    </div>
                                    <div className="flex flex-col space-y-2">
                                        <Button
                                            variant="ghost"
                                            size="icon"
                                            className={`${model.starred ? 'text-yellow-400 hover:text-yellow-300' : 'text-gray-400 hover:text-yellow-400'} hover:bg-gray-700 transition-colors`}
                                            onClick={() => handleToggleStarred(model)}
                                            disabled={isProcessing}
                                        >
                                            <Star className={`h-4 w-4 ${model.starred ? 'fill-yellow-400' : ''}`} />
                                        </Button>
                                        <Button
                                            variant="ghost"
                                            size="icon"
                                            className="text-gray-400 hover:text-gray-100 hover:bg-gray-700 transition-colors"
                                            onClick={() => {
                                                setEditingModel(model);
                                                setEditOpen(true);
                                            }}
                                            disabled={isProcessing}
                                        >
                                            <Pencil className="h-4 w-4" />
                                        </Button>
                                        <Button
                                            variant="ghost"
                                            size="icon"
                                            className="text-red-400 hover:text-red-300 hover:bg-red-900/20 transition-colors"
                                            onClick={() => {
                                                setModelToDelete(model);
                                                setDeleteOpen(true);
                                            }}
                                            disabled={isProcessing}
                                        >
                                            <Trash2 className="h-4 w-4" />
                                        </Button>
                                    </div>
                                </div>
                            </CardHeader>
                            <CardContent className="pt-4 px-4">
                                <div className="bg-gray-700/30 p-4 rounded-lg">
                                    <div className="flex flex-col gap-3">
                                        <div className="flex items-center justify-between">
                                            <span className="text-gray-400 text-xs">Provider</span>
                                            <span className="text-gray-200 font-medium flex items-center gap-2">
                                                {getProviderName(model.providerGuid)}
                                            </span>
                                        </div>
                                        <div className="flex items-center justify-between">
                                            <span className="text-gray-400 text-xs">Token Pricing</span>
                                            <div className="text-gray-200 font-medium">
                                                <span>In: ${model.input1MTokenPrice}</span>
                                                <span className="mx-2 text-gray-500">|</span>
                                                <span>Out: ${model.output1MTokenPrice}</span>
                                            </div>
                                        </div>
                                        {model.userNotes && (
                                            <div className="pt-2 border-t border-gray-700/50">
                                                <div className="text-gray-200 italic text-sm">{model.userNotes}</div>
                                            </div>
                                        )}
                                    </div>
                                </div>
                                {model.supportsPrefill && (
                                    <div className="mt-3 text-xs flex items-center gap-1 text-blue-400">
                                        <span className="inline-block w-2 h-2 rounded-full bg-blue-400"></span>
                                        Supports prefilling content
                                    </div>
                                )}
                            </CardContent>
                        </Card>
                    ))}
                </div>
            )}

            {/* Add Model Dialog */}
            <Dialog open={addOpen} onOpenChange={setAddOpen}>
                <DialogContent className="max-w-xl bg-gray-800 border-gray-700 text-gray-100" description="Add a new AI model">
                    <DialogHeader>
                        <DialogTitle className="text-gray-100">Add New Model</DialogTitle>
                    </DialogHeader>
                    {displayError && (
                        <div className="bg-red-950/30 text-red-400 p-3 rounded-md mb-4 border border-red-800/50">
                            {displayError}
                        </div>
                    )}
                    <ModelForm
                        providers={providers}
                        onSubmit={handleAddModel}
                        isProcessing={isProcessing}
                    />
                </DialogContent>
            </Dialog>

            {/* Edit Model Dialog */}
            <Dialog open={editOpen} onOpenChange={setEditOpen}>
                <DialogContent className="max-w-xl bg-gray-800 border-gray-700 text-gray-100" description="Edit an existing AI model">
                    <DialogHeader>
                        <DialogTitle className="text-gray-100">Edit Model</DialogTitle>
                    </DialogHeader>
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
                </DialogContent>
            </Dialog>

            {/* Delete Confirmation Dialog */}
            <Dialog open={deleteOpen} onOpenChange={setDeleteOpen}>
                <DialogContent className="bg-gray-800 border-gray-700 text-gray-100" description="Confirm deletion of an AI model">
                    <DialogHeader>
                        <DialogTitle className="text-gray-100">Confirm Deletion</DialogTitle>
                    </DialogHeader>
                    <div className="py-4 text-gray-200">
                        Are you sure you want to delete the model <strong>{modelToDelete?.friendlyName}</strong>?
                        This action cannot be undone.
                    </div>
                    {displayError && (
                        <div className="bg-red-950/30 text-red-400 p-3 rounded-md mb-4 border border-red-800/50">
                            {displayError}
                        </div>
                    )}
                    <div className="flex justify-end space-x-2">
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
                    </div>
                </DialogContent>
            </Dialog>
        </>
    );
};