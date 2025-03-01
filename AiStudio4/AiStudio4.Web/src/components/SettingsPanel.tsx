import React, { useState, useEffect } from 'react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { ModelManagement } from './settings/ModelManagement';
import { ServiceProviderManagement } from './settings/ServiceProviderManagement';
import { Tabs, TabsList, TabsTrigger, TabsContent } from '@/components/ui/tabs';
import { Card, CardContent } from '@/components/ui/card';
import { SettingsService } from '@/services/SettingsService';
import { Model, ServiceProvider } from '@/types/settings';

interface SettingsPanelProps {
    isOpen: boolean;
    onClose?: () => void;
    isPinned?: boolean;
}

export const SettingsPanel: React.FC<SettingsPanelProps> = ({ isOpen, onClose, isPinned }) => {
    const [models, setModels] = useState<Model[]>([]);
    const [providers, setProviders] = useState<ServiceProvider[]>([]);
    const [isLoading, setIsLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);

    useEffect(() => {
        if (isOpen) {
            loadData();
        }
    }, [isOpen]);

    const loadData = async () => {
        setIsLoading(true);
        setError(null);
        try {
            const [modelsData, providersData] = await Promise.all([
                SettingsService.getModels(),
                SettingsService.getServiceProviders()
            ]);
            setModels(modelsData);
            setProviders(providersData);
        } catch (err) {
            console.error('Failed to load settings data:', err);
            setError(err instanceof Error ? err.message : 'Unknown error loading data');
        } finally {
            setIsLoading(false);
        }
    };

    // Handlers for models
    const handleAddModel = async (model: Omit<Model, 'guid'>) => {
        try {
            await SettingsService.addModel(model);
            await loadData(); // Reload data after successful add
        } catch (err) {
            console.error('Failed to add model:', err);
            throw err; // Re-throw to be caught by the form component
        }
    };

    const handleUpdateModel = async (model: Model) => {
        try {
            await SettingsService.updateModel(model);
            await loadData(); // Reload data after successful update
        } catch (err) {
            console.error('Failed to update model:', err);
            throw err; // Re-throw to be caught by the form component
        }
    };

    const handleDeleteModel = async (modelGuid: string) => {
        try {
            await SettingsService.deleteModel(modelGuid);
            await loadData(); // Reload data after successful delete
        } catch (err) {
            console.error('Failed to delete model:', err);
            throw err; // Re-throw to be caught by the form component
        }
    };

    // Handlers for service providers
    const handleAddProvider = async (provider: Omit<ServiceProvider, 'guid'>) => {
        try {
            await SettingsService.addServiceProvider(provider);
            await loadData(); // Reload data after successful add
        } catch (err) {
            console.error('Failed to add service provider:', err);
            throw err; // Re-throw to be caught by the form component
        }
    };

    const handleUpdateProvider = async (provider: ServiceProvider) => {
        try {
            await SettingsService.updateServiceProvider(provider);
            await loadData(); // Reload data after successful update
        } catch (err) {
            console.error('Failed to update service provider:', err);
            throw err; // Re-throw to be caught by the form component
        }
    };

    const handleDeleteProvider = async (providerGuid: string) => {
        try {
            await SettingsService.deleteServiceProvider(providerGuid);
            await loadData(); // Reload data after successful delete
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
                    {error}
                    <Button
                        className="mt-2 w-full bg-gray-800 hover:bg-gray-700 text-gray-100"
                        variant="outline"
                        onClick={() => loadData()}
                    >
                        Retry
                    </Button>
                </div>
            ) : (
                <Tabs defaultValue="models" className="w-full">
                    <TabsList className="grid grid-cols-3 mb-4 bg-gray-800 p-1">
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