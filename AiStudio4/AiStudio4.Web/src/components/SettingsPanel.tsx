// src/components/SettingsPanel.tsx
import React, { useEffect, useState } from 'react';
import { Button } from '@/components/ui/button';
import { ModelManagement } from './settings/ModelManagement';
import { ServiceProviderManagement } from './settings/ServiceProviderManagement';
import { AppearanceTab } from './settings/AppearanceTab';
import { Tabs, TabsList, TabsTrigger, TabsContent } from '@/components/ui/tabs';
import { useModelManagement } from '@/hooks/useModelManagement';
import { Model, ServiceProvider } from '@/types/settings';
import { commandEvents } from '@/commands/settingsCommands';

export const SettingsPanel: React.FC = () => {
    // Use ModelManagement hook instead of direct store access
    const {
        models,
        providers,
        isLoading: loading,
        error,
        fetchModels,
        fetchProviders,
        clearError: setError
    } = useModelManagement();

    // State to track which tab is active
    const [activeTab, setActiveTab] = useState('models');

    // Handle dialog states for editing models and providers
    const [editModelDialogOpen, setEditModelDialogOpen] = useState(false);
    const [editProviderDialogOpen, setEditProviderDialogOpen] = useState(false);
    const [modelToEdit, setModelToEdit] = useState<Model | null>(null);
    const [providerToEdit, setProviderToEdit] = useState<ServiceProvider | null>(null);

    // Listen for command events
    useEffect(() => {
        // Listen for tab changes
        const unsubscribeTab = commandEvents.on('settings-tab', (tabName) => {
            if (tabName === 'models' || tabName === 'providers' || tabName === 'appearance') {
                setActiveTab(tabName);
            }
        });

        // Listen for model edit requests
        const unsubscribeModel = commandEvents.on('edit-model', (modelGuid) => {
            const model = models.find(m => m.guid === modelGuid);
            if (model) {
                setActiveTab('models');
                setModelToEdit(model);
                setEditModelDialogOpen(true);
            }
        });

        // Listen for provider edit requests
        const unsubscribeProvider = commandEvents.on('edit-provider', (providerGuid) => {
            const provider = providers.find(p => p.guid === providerGuid);
            if (provider) {
                setActiveTab('providers');
                setProviderToEdit(provider);
                setEditProviderDialogOpen(true);
            }
        });

        return () => {
            unsubscribeTab();
            unsubscribeModel();
            unsubscribeProvider();
        };
    }, [models, providers]);

    // Fetch data on component mount
    useEffect(() => {
        fetchData();
    }, []);

    // Function to fetch models and providers using hooks
    const fetchData = async () => {
        try {
            await Promise.all([
                fetchModels(),
                fetchProviders()
            ]);
        } catch (error) {
            console.error('Error fetching settings data:', error);
            setError(error instanceof Error ? error.message : 'Failed to load settings');
        }
    };

    // Retry loading function
    const handleRetry = () => {
        setError(null);
        fetchData();
    };

    if (loading) {
        return (
            <div className="flex items-center justify-center h-32 p-4">
                <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-500"></div>
            </div>
        );
    }

    if (error) {
        return (
            <div className="text-red-400 p-4 bg-red-950/30 border border-red-800/50 rounded-md m-4">
                {error}
                <Button
                    className="mt-2 w-full bg-gray-800 hover:bg-gray-700 text-gray-100"
                    variant="outline"
                    onClick={handleRetry}
                >
                    Retry
                </Button>
            </div>
        );
    }

    return (
        <div className="p-4 overflow-y-auto h-full bg-gray-900 text-gray-100">
            <Tabs value={activeTab} onValueChange={setActiveTab} className="w-full">
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
                        providers={providers}
                        modelToEdit={modelToEdit}
                        setModelToEdit={setModelToEdit}
                        editDialogOpen={editModelDialogOpen}
                        setEditDialogOpen={setEditModelDialogOpen}
                    />
                </TabsContent>

                <TabsContent value="providers" className="space-y-4">
                    <ServiceProviderManagement
                        providers={providers}
                        providerToEdit={providerToEdit}
                        setProviderToEdit={setProviderToEdit}
                        editDialogOpen={editProviderDialogOpen}
                        setEditDialogOpen={setEditProviderDialogOpen}
                    />
                </TabsContent>

                <TabsContent value="appearance" className="space-y-4">
                    <AppearanceTab />
                </TabsContent>
            </Tabs>

            <div className="mt-8">
                <h3 className="text-md font-medium mb-2 text-gray-200">About</h3>
                <div className="p-3 bg-gray-800/50 rounded-md border border-gray-700/50">
                    <p className="text-sm text-gray-300">Version: 1.0.0</p>
                    <p className="text-sm text-gray-300">Build Date: 2023-05-28</p>
                </div>
            </div>
        </div>
    );
}