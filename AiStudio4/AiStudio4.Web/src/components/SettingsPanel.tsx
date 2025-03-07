import React, { useEffect } from 'react';
import { Button } from '@/components/ui/button';
import { ModelManagement } from './settings/ModelManagement';
import { ServiceProviderManagement } from './settings/ServiceProviderManagement';
import { Tabs, TabsList, TabsTrigger, TabsContent } from '@/components/ui/tabs';
import { Card, CardContent } from '@/components/ui/card';
import { useModelStore } from '@/stores/useModelStore';

interface SettingsPanelProps {
    isOpen: boolean;
}

export const SettingsPanel: React.FC<SettingsPanelProps> = ({ isOpen }) => {
    // Use Zustand store
    const {
        models,
        providers,
        loading,
        error,
        setModels,
        setProviders,
        setError
    } = useModelStore();

    // Fetch data when the panel opens
    useEffect(() => {
        if (isOpen) {
            fetchData();
        }
    }, [isOpen]);

    // Function to fetch models and providers
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

    // Function to fetch models
    const fetchModels = async () => {
        const response = await fetch('/api/getModels', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'X-Client-Id': localStorage.getItem('clientId') || ''
            },
            body: JSON.stringify({})
        });

        const data = await response.json();

        if (!data.success) {
            throw new Error(data.error || 'Failed to fetch models');
        }

        setModels(data.models || []);
    };

    // Function to fetch providers
    const fetchProviders = async () => {
        const response = await fetch('/api/getServiceProviders', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'X-Client-Id': localStorage.getItem('clientId') || ''
            },
            body: JSON.stringify({})
        });

        const data = await response.json();

        if (!data.success) {
            throw new Error(data.error || 'Failed to fetch service providers');
        }

        setProviders(data.providers || []);
    };

    // Retry loading function
    const handleRetry = () => {
        setError(null);
        fetchData();
    };

    if (!isOpen) return null;

    return (
        <div className="p-4 overflow-y-auto h-full bg-gray-900 text-gray-100">
            {loading ? (
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
                            providers={providers}
                        />
                    </TabsContent>

                    <TabsContent value="providers" className="space-y-4">
                        <ServiceProviderManagement
                            providers={providers}
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