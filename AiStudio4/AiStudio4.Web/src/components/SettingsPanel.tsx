import React from 'react';
import { Button } from '@/components/ui/button';
import { ModelManagement } from './settings/ModelManagement';
import { ServiceProviderManagement } from './settings/ServiceProviderManagement';
import { Tabs, TabsList, TabsTrigger, TabsContent } from '@/components/ui/tabs';
import { Card, CardContent } from '@/components/ui/card';
import { Model, ServiceProvider } from '@/types/settings';
import {
    useGetModelsQuery,
    useGetServiceProvidersQuery,
    useAddModelMutation,
    useUpdateModelMutation,
    useDeleteModelMutation,
    useAddServiceProviderMutation,
    useUpdateServiceProviderMutation,
    useDeleteServiceProviderMutation
} from '@/services/api/settingsApi';

interface SettingsPanelProps {
    isOpen: boolean;
}

export const SettingsPanel: React.FC<SettingsPanelProps> = ({ isOpen }) => {
    // Use RTK Query hooks to fetch data and handle mutations
    const {
        data: models = [],
        isLoading: isModelsLoading,
        error: modelsError,
        refetch: refetchModels
    } = useGetModelsQuery(undefined, { skip: !isOpen });

    const {
        data: providers = [],
        isLoading: isProvidersLoading,
        error: providersError,
        refetch: refetchProviders
    } = useGetServiceProvidersQuery(undefined, { skip: !isOpen });

    // Model mutations
    const [addModel, { isLoading: isAddingModel }] = useAddModelMutation();
    const [updateModel, { isLoading: isUpdatingModel }] = useUpdateModelMutation();
    const [deleteModel, { isLoading: isDeletingModel }] = useDeleteModelMutation();

    // Provider mutations
    const [addServiceProvider, { isLoading: isAddingProvider }] = useAddServiceProviderMutation();
    const [updateServiceProvider, { isLoading: isUpdatingProvider }] = useUpdateServiceProviderMutation();
    const [deleteServiceProvider, { isLoading: isDeletingProvider }] = useDeleteServiceProviderMutation();

    // Determine loading and error states
    const isLoading = isModelsLoading || isProvidersLoading;
    const error = modelsError || providersError;

    // Retry loading function
    const handleRetry = () => {
        refetchModels();
        refetchProviders();
    };

    // Handlers for models
    const handleAddModel = async (model: Omit<Model, 'guid'>) => {
        try {
            await addModel(model).unwrap();
        } catch (err) {
            console.error('Failed to add model:', err);
            throw err; // Re-throw to be caught by the form component
        }
    };

    const handleUpdateModel = async (model: Model) => {
        try {
            await updateModel(model).unwrap();
        } catch (err) {
            console.error('Failed to update model:', err);
            throw err; // Re-throw to be caught by the form component
        }
    };

    const handleDeleteModel = async (modelGuid: string) => {
        try {
            await deleteModel(modelGuid).unwrap();
        } catch (err) {
            console.error('Failed to delete model:', err);
            throw err; // Re-throw to be caught by the form component
        }
    };

    // Handlers for service providers
    const handleAddProvider = async (provider: Omit<ServiceProvider, 'guid'>) => {
        try {
            await addServiceProvider(provider).unwrap();
        } catch (err) {
            console.error('Failed to add service provider:', err);
            throw err; // Re-throw to be caught by the form component
        }
    };

    const handleUpdateProvider = async (provider: ServiceProvider) => {
        try {
            await updateServiceProvider(provider).unwrap();
        } catch (err) {
            console.error('Failed to update service provider:', err);
            throw err; // Re-throw to be caught by the form component
        }
    };

    const handleDeleteProvider = async (providerGuid: string) => {
        try {
            await deleteServiceProvider(providerGuid).unwrap();
        } catch (err) {
            console.error('Failed to delete service provider:', err);
            throw err; // Re-throw to be caught by the form component
        }
    };

    if (!isOpen) return null;

    return (
        <div className="p-4 overflow-y-auto h-full bg-gray-900 text-gray-100">
            {isLoading ? (
                <div className="flex items-center justify-center h-32">
                    <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-500"></div>
                </div>
            ) : error ? (
                <div className="text-red-400 p-4 bg-red-950/30 border border-red-800/50 rounded-md mb-4">
                    {error instanceof Error ? error.message : 'An error occurred while loading settings'}
                    <Button
                        className="mt-2 w-full bg-gray-800 hover:bg-gray-700 text-gray-100"
                        variant="outline"
                        onClick={handleRetry}
                    >
                        Retry
                    </Button>
                </div>
            ) : (
                <Tabs defaultValue="models" className="w-full">
                    <TabsList className="grid grid-cols-4 mb-4 bg-gray-800 p-1">
                        <TabsTrigger
                            value="models"
                            className="data-[state=active]:bg-gray-700 data-[state=active]:text-gray-100 text-gray-400"
                        >
                            Models
                        </TabsTrigger>
                        <TabsTrigger
                            value="providers"
                            className="data-[state=active]:bg-gray-700 data-[state=active]:text-gray-100 text-gray-400"
                        >
                            Providers
                        </TabsTrigger>
                        <TabsTrigger
                            value="appearance"
                            className="data-[state=active]:bg-gray-700 data-[state=active]:text-gray-100 text-gray-400"
                        >
                            Appearance
                        </TabsTrigger>
                    </TabsList>

                    <TabsContent value="models" className="space-y-4">
                        <ModelManagement
                            models={models}
                            providers={providers}
                            onAddModel={handleAddModel}
                            onUpdateModel={handleUpdateModel}
                            onDeleteModel={handleDeleteModel}
                        />
                    </TabsContent>

                    <TabsContent value="providers" className="space-y-4">
                        <ServiceProviderManagement
                            providers={providers}
                            onAddProvider={handleAddProvider}
                            onUpdateProvider={handleUpdateProvider}
                            onDeleteProvider={handleDeleteProvider}
                        />
                    </TabsContent>

                    <TabsContent value="appearance" className="space-y-4">
                        <Card className="bg-gray-800 border-gray-700">
                            <CardContent className="pt-6">
                                <h3 className="text-md font-medium mb-2 text-gray-200">Theme Settings</h3>
                                <div className="p-3 bg-gray-700/50 rounded-md border border-gray-600/50">
                                    Theme settings will go here
                                </div>
                            </CardContent>
                        </Card>
                    </TabsContent>
                </Tabs>
            )}

            <div className="mt-8">
                <h3 className="text-md font-medium mb-2 text-gray-200">About</h3>
                <div className="p-3 bg-gray-800/50 rounded-md border border-gray-700/50">
                    <p className="text-sm text-gray-300">Version: 1.0.0</p>
                    <p className="text-sm text-gray-300">Build Date: 2023-05-28</p>
                </div>
            </div>
        </div>
    );
};