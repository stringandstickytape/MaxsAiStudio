import React, { useEffect, useState } from 'react';
import { Button } from '@/components/ui/button';
import { ModelManagement } from './settings/ModelManagement';
import { ServiceProviderManagement } from './settings/ServiceProviderManagement';
import { Tabs, TabsList, TabsTrigger, TabsContent } from '@/components/ui/tabs';
import { Card, CardContent } from '@/components/ui/card';
import { useModelManagement } from '@/hooks/useModelManagement';
import { commandEvents } from '@/commands/settingsCommands';

interface SettingsPanelProps {
    isOpen: boolean;
}

export const SettingsPanel: React.FC<SettingsPanelProps> = ({ isOpen }) => {
    // Use model management hook for all model/provider operations
    const {
        isLoading,
        error,
        clearError,
        fetchProviders
    } = useModelManagement();

    // State to track which tab is active
    const [activeTab, setActiveTab] = useState('models');

    // Handle dialog states for editing models and providers
    const [editModelDialogOpen, setEditModelDialogOpen] = useState(false);
    const [editProviderDialogOpen, setEditProviderDialogOpen] = useState(false);
    const [modelToEdit, setModelToEdit] = useState<any | null>(null);
    const [providerToEdit, setProviderToEdit] = useState<any | null>(null);

    // Listen for command events
    useEffect(() => {
        // Only set up listeners when the panel is open
        if (!isOpen) return;

        // Listen for tab changes
        const unsubscribeTab = commandEvents.on('settings-tab', (tabName) => {
            console.log(`Changing tab to: ${tabName}`);
            if (tabName === 'models' || tabName === 'providers' || tabName === 'appearance') {
                setActiveTab(tabName);
            }
        });

        // Listen for model edit requests
        const unsubscribeModel = commandEvents.on('edit-model', (modelGuid) => {
            console.log(`Edit model requested: ${modelGuid}`);
            setActiveTab('models');
            setModelToEdit({ guid: modelGuid });
            setEditModelDialogOpen(true);
        });

        // Listen for provider edit requests
        const unsubscribeProvider = commandEvents.on('edit-provider', (providerGuid) => {
            console.log(`Edit provider requested: ${providerGuid}`);
            setActiveTab('providers');
            setProviderToEdit({ guid: providerGuid });
            setEditProviderDialogOpen(true);
        });

        return () => {
            unsubscribeTab();
            unsubscribeModel();
            unsubscribeProvider();
        };
    }, [isOpen]);

    // Fetch data when the panel opens
    useEffect(() => {
        if (isOpen) {
            // Refresh providers list when panel opens
            fetchProviders();
        }
    }, [isOpen, fetchProviders]);

    // Retry loading function
    const handleRetry = () => {
        clearError();
        fetchProviders();
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
                        onClick={handleRetry}
                    >
                        Retry
                    </Button>
                </div>
            ) : (
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
                            providers={[]} // This is no longer used
                            modelToEdit={modelToEdit}
                            setModelToEdit={setModelToEdit}
                            editDialogOpen={editModelDialogOpen}
                            setEditDialogOpen={setEditModelDialogOpen}
                        />
                    </TabsContent>

                    <TabsContent value="providers" className="space-y-4">
                        <ServiceProviderManagement
                            providerToEdit={providerToEdit}
                            setProviderToEdit={setProviderToEdit}
                            editDialogOpen={editProviderDialogOpen}
                            setEditDialogOpen={setEditProviderDialogOpen}
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