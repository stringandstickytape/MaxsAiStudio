import React, { useState } from 'react';
import { ServiceProvider } from '@/types/settings';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { ServiceProviderForm } from './ServiceProviderForm';
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { Pencil, Trash2, PlusCircle, AlertCircle } from 'lucide-react';

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
                <h2 className="text-xl font-semibold text-gray-100">Service Providers</h2>
                <Button
                    onClick={() => setIsAddDialogOpen(true)}
                    className="flex items-center gap-2 bg-blue-600 hover:bg-blue-700 text-white"
                >
                    <PlusCircle className="h-4 w-4" /> Add Provider
                </Button>
            </div>

            {providers.length === 0 ? (
                <Card className="bg-gray-800 border-gray-700">
                    <CardContent className="pt-6 text-center text-gray-400">
                        <div className="flex flex-col items-center justify-center py-8">
                            <AlertCircle className="h-12 w-12 mb-4 opacity-50" />
                            <p>No service providers configured yet.</p>
                            <p className="mb-4">Add your first provider to get started.</p>
                            <Button
                                onClick={() => setIsAddDialogOpen(true)}
                                variant="outline"
                                className="bg-gray-700 hover:bg-gray-600 text-gray-200 border-gray-600"
                            >
                                Add Provider
                            </Button>
                        </div>
                    </CardContent>
                </Card>
            ) : (
                <div className="grid grid-cols-1 gap-4">
                    {providers.map(provider => (
                        <Card 
                            key={provider.guid} 
                            className="overflow-hidden bg-gray-800/80 border-gray-700/50 shadow-lg hover:shadow-xl transition-all duration-300 hover:-translate-y-1 backdrop-blur-sm group"
                        >
                            <div
                                className="h-2 bg-gradient-to-r from-opacity-80 to-opacity-100 transition-all duration-300 group-hover:h-3"
                                style={{
                                    backgroundColor: '#4f46e5',
                                    from: '#4f46e580',
                                    to: '#4f46e5'
                                }}
                            />
                            <CardHeader className="pb-2 pt-4 px-4">
                                <div className="flex justify-between items-start">
                                    <div className="flex items-center gap-3">
                                        <div>
                                            <CardTitle className="flex items-center gap-2 text-gray-100 text-lg">
                                                {provider.friendlyName}
                                            </CardTitle>
                                            <CardDescription className="text-gray-400 font-mono text-xs">Protocol: {provider.serviceName}</CardDescription>
                                        </div>
                                    </div>
                                    <div className="flex flex-col space-y-2">
                                        <Button
                                            variant="ghost"
                                            size="icon"
                                            className="text-gray-400 hover:text-gray-100 hover:bg-gray-700 transition-colors"
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
                                            className="text-red-400 hover:text-red-300 hover:bg-red-900/20 transition-colors"
                                            onClick={() => {
                                                setDeleteConfirmProvider(provider);
                                                setIsDeleteDialogOpen(true);
                                            }}
                                        >
                                            <Trash2 className="h-4 w-4" />
                                        </Button>
                                    </div>
                                </div>
                                <span className="text-gray-200 font-medium overflow-hidden text-ellipsis max-w-[250px]">{provider.url}</span>
                            </CardHeader>
                        </Card>
                    ))}
                </div>
            )}

            {/* Add Provider Dialog */}
            <Dialog open={isAddDialogOpen} onOpenChange={setIsAddDialogOpen}>
                <DialogContent className="max-w-xl bg-gray-800 border-gray-700 text-gray-100" description="Add a new service provider">
                    <DialogHeader>
                        <DialogTitle className="text-gray-100">Add New Provider</DialogTitle>
                    </DialogHeader>
                    {error && (
                        <div className="bg-red-950/30 text-red-400 p-3 rounded-md mb-4 border border-red-800/50">
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
                <DialogContent className="max-w-xl bg-gray-800 border-gray-700 text-gray-100" description="Edit an existing service provider">
                    <DialogHeader>
                        <DialogTitle className="text-gray-100">Edit Provider</DialogTitle>
                    </DialogHeader>
                    {error && (
                        <div className="bg-red-950/30 text-red-400 p-3 rounded-md mb-4 border border-red-800/50">
                            {error}
                        </div>
                    )}
                    {editingProvider && (
                        <ServiceProviderForm
                            key={`edit-provider-form-${editingProvider.guid}`}
                            onSubmit={handleUpdateProvider}
                            isProcessing={isProcessing}
                            initialValues={editingProvider}
                        />
                    )}
                </DialogContent>
            </Dialog>

            {/* Delete Confirmation Dialog */}
            <Dialog open={isDeleteDialogOpen} onOpenChange={setIsDeleteDialogOpen}>
                <DialogContent className="bg-gray-800 border-gray-700 text-gray-100" description="Confirm deletion of a service provider">
                    <DialogHeader>
                        <DialogTitle className="text-gray-100">Confirm Deletion</DialogTitle>
                    </DialogHeader>
                    <div className="py-4 text-gray-200">
                        Are you sure you want to delete the provider <strong>{deleteConfirmProvider?.friendlyName}</strong>?
                        This action cannot be undone. Models associated with this provider may stop working.
                    </div>
                    {error && (
                        <div className="bg-red-950/30 text-red-400 p-3 rounded-md mb-4 border border-red-800/50">
                            {error}
                        </div>
                    )}
                    <div className="flex justify-end space-x-2">
                        <Button
                            variant="outline"
                            onClick={() => setIsDeleteDialogOpen(false)}
                            disabled={isProcessing}
                            className="bg-gray-700 hover:bg-gray-600 text-gray-200 border-gray-600"
                        >
                            Cancel
                        </Button>
                        <Button
                            variant="destructive"
                            onClick={handleDeleteProviderConfirm}
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