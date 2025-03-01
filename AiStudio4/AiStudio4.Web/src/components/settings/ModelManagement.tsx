import React, { useState } from 'react';
import { Model, ServiceProvider } from '@/types/settings';
import { Card, CardContent, CardHeader, CardTitle, CardDescription, CardFooter } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { ModelForm } from './ModelForm';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogTrigger } from '@/components/ui/dialog';
import { Pencil, Trash2, Star, PlusCircle, AlertCircle } from 'lucide-react';

interface ModelManagementProps {
    models: Model[];
    providers: ServiceProvider[];
    onAddModel: (model: Omit<Model, 'guid'>) => Promise<void>;
    onUpdateModel: (model: Model) => Promise<void>;
    onDeleteModel: (modelGuid: string) => Promise<void>;
}

export const ModelManagement: React.FC<ModelManagementProps> = ({
    models,
    providers,
    onAddModel,
    onUpdateModel,
    onDeleteModel
}) => {
    const [editingModel, setEditingModel] = useState<Model | null>(null);
    const [isAddDialogOpen, setIsAddDialogOpen] = useState(false);
    const [isEditDialogOpen, setIsEditDialogOpen] = useState(false);
    const [deleteConfirmModel, setDeleteConfirmModel] = useState<Model | null>(null);
    const [isDeleteDialogOpen, setIsDeleteDialogOpen] = useState(false);
    const [isProcessing, setIsProcessing] = useState(false);
    const [error, setError] = useState<string | null>(null);

    const handleAddModel = async (modelData: Omit<Model, 'guid'>) => {
        setIsProcessing(true);
        setError(null);
        try {
            await onAddModel(modelData);
            setIsAddDialogOpen(false);
        } catch (err) {
            setError(err instanceof Error ? err.message : 'Failed to add model');
        } finally {
            setIsProcessing(false);
        }
    };

    const handleUpdateModel = async (modelData: Model) => {
        setIsProcessing(true);
        setError(null);
        try {
            await onUpdateModel(modelData);
            setIsEditDialogOpen(false);
        } catch (err) {
            setError(err instanceof Error ? err.message : 'Failed to update model');
        } finally {
            setIsProcessing(false);
        }
    };

    const handleDeleteModelConfirm = async () => {
        if (!deleteConfirmModel) return;
        
        setIsProcessing(true);
        setError(null);
        try {
            await onDeleteModel(deleteConfirmModel.guid);
            setIsDeleteDialogOpen(false);
            setDeleteConfirmModel(null);
        } catch (err) {
            setError(err instanceof Error ? err.message : 'Failed to delete model');
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
                <h2 className="text-xl font-semibold">AI Models</h2>
                <Button 
                    onClick={() => setIsAddDialogOpen(true)}
                    className="flex items-center gap-2"
                >
                    <PlusCircle className="h-4 w-4" /> Add Model
                </Button>
            </div>

            {models.length === 0 ? (
                <Card>
                    <CardContent className="pt-6 text-center text-gray-400">
                        <div className="flex flex-col items-center justify-center py-8">
                            <AlertCircle className="h-12 w-12 mb-4 opacity-50" />
                            <p>No models configured yet.</p>
                            <p className="mb-4">Add your first model to get started.</p>
                            <Button 
                                onClick={() => setIsAddDialogOpen(true)}
                                variant="outline"
                            >
                                Add Model
                            </Button>
                        </div>
                    </CardContent>
                </Card>
            ) : (
                <div className="grid grid-cols-1 gap-4">
                    {models.map(model => (
                        <Card key={model.guid} className="overflow-hidden">
                            <div 
                                className="h-1" 
                                style={{ backgroundColor: model.color || '#4f46e5' }} 
                            />
                            <CardHeader className="pb-2">
                                <div className="flex justify-between items-start">
                                    <div>
                                        <CardTitle className="flex items-center gap-2">
                                            {model.friendlyName}
                                            {model.starred && <Star className="h-4 w-4 fill-yellow-400 text-yellow-400" />}
                                        </CardTitle>
                                        <CardDescription>{model.modelName}</CardDescription>
                                    </div>
                                    <div className="flex space-x-2">
                                        <Button 
                                            variant="ghost" 
                                            size="icon" 
                                            onClick={() => {
                                                setEditingModel(model);
                                                setIsEditDialogOpen(true);
                                            }}
                                        >
                                            <Pencil className="h-4 w-4" />
                                        </Button>
                                        <Button 
                                            variant="ghost" 
                                            size="icon"
                                            className="text-red-500 hover:text-red-700 hover:bg-red-100/10"
                                            onClick={() => {
                                                setDeleteConfirmModel(model);
                                                setIsDeleteDialogOpen(true);
                                            }}
                                        >
                                            <Trash2 className="h-4 w-4" />
                                        </Button>
                                    </div>
                                </div>
                            </CardHeader>
                            <CardContent>
                                <div className="grid grid-cols-2 gap-2 text-sm">
                                    <div>
                                        <span className="text-gray-400">Provider:</span> {getProviderName(model.providerGuid)}
                                    </div>
                                    <div>
                                        <span className="text-gray-400">Token Prices:</span> {model.input1MTokenPrice} / {model.output1MTokenPrice}
                                    </div>
                                </div>
                                {model.userNotes && (
                                    <div className="mt-2 text-sm">
                                        <span className="text-gray-400">Notes:</span> {model.userNotes}
                                    </div>
                                )}
                            </CardContent>
                        </Card>
                    ))}
                </div>
            )}

            {/* Add Model Dialog */}
            <Dialog open={isAddDialogOpen} onOpenChange={setIsAddDialogOpen}>
                <DialogContent className="max-w-xl" description="Add a new AI model">
                    <DialogHeader>
                        <DialogTitle>Add New Model</DialogTitle>
                    </DialogHeader>
                    {error && (
                        <div className="bg-red-500/10 text-red-500 p-3 rounded-md mb-4">
                            {error}
                        </div>
                    )}
                    <ModelForm 
                        providers={providers}
                        onSubmit={handleAddModel}
                        isProcessing={isProcessing}
                    />
                </DialogContent>
            </Dialog>

// In ModelManagement.tsx
            // Find the Edit Model Dialog section and ensure it has a unique key

            {/* Edit Model Dialog */}
            <Dialog open={isEditDialogOpen} onOpenChange={setIsEditDialogOpen}>
                <DialogContent className="max-w-xl" description="Edit an existing AI model">
                    <DialogHeader>
                        <DialogTitle>Edit Model</DialogTitle>
                    </DialogHeader>
                    {error && (
                        <div className="bg-red-500/10 text-red-500 p-3 rounded-md mb-4">
                            {error}
                        </div>
                    )}
                    {editingModel && (
                        <ModelForm
                            key={`edit-model-form-${editingModel.guid}`} // More unique key
                            providers={providers}
                            onSubmit={handleUpdateModel}
                            isProcessing={isProcessing}
                            initialValues={editingModel}
                        />
                    )}
                </DialogContent>
            </Dialog>

            {/* Delete Confirmation Dialog */}
            <Dialog open={isDeleteDialogOpen} onOpenChange={setIsDeleteDialogOpen}>
                <DialogContent description="Confirm deletion of an AI model">
                    <DialogHeader>
                        <DialogTitle>Confirm Deletion</DialogTitle>
                    </DialogHeader>
                    <div className="py-4">
                        Are you sure you want to delete the model <strong>{deleteConfirmModel?.friendlyName}</strong>?
                        This action cannot be undone.
                    </div>
                    {error && (
                        <div className="bg-red-500/10 text-red-500 p-3 rounded-md mb-4">
                            {error}
                        </div>
                    )}
                    <div className="flex justify-end space-x-2">
                        <Button 
                            variant="outline" 
                            onClick={() => setIsDeleteDialogOpen(false)}
                            disabled={isProcessing}
                        >
                            Cancel
                        </Button>
                        <Button 
                            variant="destructive" 
                            onClick={handleDeleteModelConfirm}
                            disabled={isProcessing}
                        >
                            {isProcessing ? 'Deleting...' : 'Delete'}
                        </Button>
                    </div>
                </DialogContent>
            </Dialog>
        </>
    );
};