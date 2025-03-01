import React, { useState } from 'react';
import { ServiceProvider } from '@/types/settings';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { ServiceProviderForm } from './ServiceProviderForm';
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { Pencil, Trash2, PlusCircle, Server, AlertCircle } from 'lucide-react';

interface ServiceProviderManagementProps {
    providers: ServiceProvider[];
    onAddProvider: (provider: Omit<ServiceProvider, 'guid'>) => Promise<void>;
    onUpdateProvider: (provider: ServiceProvider) => Promise<void>;
    onDeleteProvider: (providerGuid: string) => Promise<void>;
}

export const ServiceProviderManagement: React.FC<ServiceProviderManagementProps> = ({
    providers,
    onAddProvider,
    onUpdateProvider,
    onDeleteProvider
}) => {
    const [editingProvider, setEditingProvider] = useState<ServiceProvider | null>(null);
    const [isAddDialogOpen, setIsAddDialogOpen] = useState(false);
    const [isEditDialogOpen, setIsEditDialogOpen] = useState(false);
    const [deleteConfirmProvider, setDeleteConfirmProvider] = useState<ServiceProvider | null>(null);
    const [isDeleteDialogOpen, setIsDeleteDialogOpen] = useState(false);
    const [isProcessing, setIsProcessing] = useState(false);
    const [error, setError] = useState<string | null>(null);

    const handleAddProvider = async (providerData: Omit<ServiceProvider, 'guid'>) => {
        setIsProcessing(true);
        setError(null);
        try {
            await onAddProvider(providerData);
            setIsAddDialogOpen(false);
        } catch (err) {
            setError(err instanceof Error ? err.message : 'Failed to add provider');
        } finally {
            setIsProcessing(false);
        }
    };

    const handleUpdateProvider = async (providerData: ServiceProvider) => {
        setIsProcessing(true);
        setError(null);
        try {
            await onUpdateProvider(providerData);
            setIsEditDialogOpen(false);
        } catch (err) {
            setError(err instanceof Error ? err.message : 'Failed to update provider');
        } finally {
            setIsProcessing(false);
        }
    };

    const handleDeleteProviderConfirm = async () => {
        if (!deleteConfirmProvider) return;
        
        setIsProcessing(true);
        setError(null);
        try {
            await onDeleteProvider(deleteConfirmProvider.guid);
            setIsDeleteDialogOpen(false);
            setDeleteConfirmProvider(null);
        } catch (err) {
            setError(err instanceof Error ? err.message : 'Failed to delete provider');
        } finally {
            setIsProcessing(false);
        }
    };

    return (
        <>
            <div className="flex justify-between items-center mb-4">
                <h2 className="text-xl font-semibold">Service Providers</h2>
                <Button 
                    onClick={() => setIsAddDialogOpen(true)}
                    className="flex items-center gap-2"
                >
                    <PlusCircle className="h-4 w-4" /> Add Provider
                </Button>
            </div>

            {providers.length === 0 ? (
                <Card>
                    <CardContent className="pt-6 text-center text-gray-400">
                        <div className="flex flex-col items-center justify-center py-8">
                            <AlertCircle className="h-12 w-12 mb-4 opacity-50" />
                            <p>No service providers configured yet.</p>
                            <p className="mb-4">Add your first provider to get started.</p>
                            <Button 
                                onClick={() => setIsAddDialogOpen(true)}
                                variant="outline"
                            >
                                Add Provider
                            </Button>
                        </div>
                    </CardContent>
                </Card>
            ) : (
                <div className="grid grid-cols-1 gap-4">
                    {providers.map(provider => (
                        <Card key={provider.guid} className="overflow-hidden">
                            <CardHeader className="pb-2">
                                <div className="flex justify-between items-start">
                                    <div>
                                        <CardTitle className="flex items-center gap-2">
                                            <Server className="h-4 w-4" /> {provider.friendlyName}
                                        </CardTitle>
                                        <CardDescription>{provider.serviceName}</CardDescription>
                                    </div>
                                    <div className="flex space-x-2">
                                        <Button 
                                            variant="ghost" 
                                            size="icon" 
                                            onClick={() => {
                                                setEditingProvider(provider);
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
                                                setDeleteConfirmProvider(provider);
                                                setIsDeleteDialogOpen(true);
                                            }}
                                        >
                                            <Trash2 className="h-4 w-4" />
                                        </Button>
                                    </div>
                                </div>
                            </CardHeader>
                            <CardContent>
                                <div className="grid grid-cols-1 gap-2 text-sm">
                                    <div>
                                        <span className="text-gray-400">URL:</span> {provider.url}
                                    </div>
                                    <div>
                                        <span className="text-gray-400">API Key:</span> {provider.apiKey ? '��������' : 'Not set'}
                                    </div>
                                </div>
                            </CardContent>
                        </Card>
                    ))}
                </div>
            )}

            {/* Add Provider Dialog */}
            <Dialog open={isAddDialogOpen} onOpenChange={setIsAddDialogOpen}>
                <DialogContent className="max-w-xl" description="Add a new service provider">
                    <DialogHeader>
                        <DialogTitle>Add New Service Provider</DialogTitle>
                    </DialogHeader>
                    {error && (
                        <div className="bg-red-500/10 text-red-500 p-3 rounded-md mb-4">
                            {error}
                        </div>
                    )}
                    <ServiceProviderForm 
                        onSubmit={handleAddProvider}
                        isProcessing={isProcessing}
                    />
                </DialogContent>
            </Dialog>

            {/* Edit Provider Dialog */}
            <Dialog open={isEditDialogOpen} onOpenChange={setIsEditDialogOpen}>
                <DialogContent className="max-w-xl" description="Edit an existing service provider">
                    <DialogHeader>
                        <DialogTitle>Edit Service Provider</DialogTitle>
                    </DialogHeader>
                    {error && (
                        <div className="bg-red-500/10 text-red-500 p-3 rounded-md mb-4">
                            {error}
                        </div>
                    )}
                    {editingProvider && (
                        <ServiceProviderForm
                            key={`edit-provider-form-${editingProvider.guid}`} // More unique key
                            onSubmit={handleUpdateProvider}
                            isProcessing={isProcessing}
                            initialValues={editingProvider}
                        />
                    )}
                </DialogContent>
            </Dialog>

            {/* Delete Confirmation Dialog */}
            <Dialog open={isDeleteDialogOpen} onOpenChange={setIsDeleteDialogOpen}>
                <DialogContent description="Confirm deletion of a service provider">
                    <DialogHeader>
                        <DialogTitle>Confirm Deletion</DialogTitle>
                    </DialogHeader>
                    <div className="py-4">
                        Are you sure you want to delete the service provider <strong>{deleteConfirmProvider?.friendlyName}</strong>?
                        This action cannot be undone and may affect models using this provider.
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
                            onClick={handleDeleteProviderConfirm}
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