import { wsManager } from './websocket/WebSocketManager';
import { ServiceProvider, Model } from '@/types/settings';

export class SettingsService {
    private static normalizeModel(model: any): Model {
        return {
            guid: model.Guid,
            modelName: model.ModelName,
            friendlyName: model.FriendlyName,
            providerGuid: model.ProviderGuid,
            userNotes: model.UserNotes,
            additionalParams: model.AdditionalParams,
            input1MTokenPrice: model.Input1MTokenPrice,  // Fix casing
            output1MTokenPrice: model.Output1MTokenPrice,  // Fix casing
            color: model.Color,
            starred: model.Starred,
            supportsPrefill: model.SupportsPrefill
        };
    }

    private static normalizeProvider(provider: any): ServiceProvider {
        return {
            guid: provider.Guid,
            serviceName: provider.ServiceName,
            friendlyName: provider.FriendlyName,
            url: provider.Url,
            apiKey: provider.ApiKey
        };
    }

    static async getModels(): Promise<Model[]> {
        const clientId = wsManager.getClientId();
        if (!clientId) {
            throw new Error('Client ID not found');
        }

        const response = await fetch('/api/getModels', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'X-Client-Id': clientId,
            },
            body: JSON.stringify({ clientId })
        });

        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }

        const data = await response.json();
        if (!data.success) {
            throw new Error(data.error || 'Failed to fetch models');
        }

        // Apply normalization to each model
        return data.models.map(this.normalizeModel);
    }

    static async getServiceProviders(): Promise<ServiceProvider[]> {
        const clientId = wsManager.getClientId();
        if (!clientId) {
            throw new Error('Client ID not found');
        }

        const response = await fetch('/api/getServiceProviders', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'X-Client-Id': clientId,
            },
            body: JSON.stringify({ clientId })
        });

        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }

        const data = await response.json();
        if (!data.success) {
            throw new Error(data.error || 'Failed to fetch service providers');
        }

        // Apply normalization to each provider
        return data.providers.map(this.normalizeProvider);
    }

    static async addModel(model: Omit<Model, 'guid'>): Promise<void> {
        const clientId = wsManager.getClientId();
        if (!clientId) {
            throw new Error('Client ID not found');
        }

        const response = await fetch('/api/addModel', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'X-Client-Id': clientId,
            },
            body: JSON.stringify({
                ...model,
                clientId
            })
        });

        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }

        const data = await response.json();
        if (!data.success) {
            throw new Error(data.error || 'Failed to add model');
        }
    }

    static async updateModel(model: Model): Promise<void> {
        const clientId = wsManager.getClientId();
        if (!clientId) {
            throw new Error('Client ID not found');
        }

        const response = await fetch('/api/updateModel', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'X-Client-Id': clientId,
            },
            body: JSON.stringify({
                ...model,
                clientId
            })
        });

        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }

        const data = await response.json();
        if (!data.success) {
            throw new Error(data.error || 'Failed to update model');
        }
    }

    static async deleteModel(modelGuid: string): Promise<void> {
        const clientId = wsManager.getClientId();
        if (!clientId) {
            throw new Error('Client ID not found');
        }

        const response = await fetch('/api/deleteModel', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'X-Client-Id': clientId,
            },
            body: JSON.stringify({
                modelGuid,
                clientId
            })
        });

        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }

        const data = await response.json();
        if (!data.success) {
            throw new Error(data.error || 'Failed to delete model');
        }
    }

    static async addServiceProvider(provider: Omit<ServiceProvider, 'guid'>): Promise<void> {
        const clientId = wsManager.getClientId();
        if (!clientId) {
            throw new Error('Client ID not found');
        }

        const response = await fetch('/api/addServiceProvider', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'X-Client-Id': clientId,
            },
            body: JSON.stringify({
                ...provider,
                clientId
            })
        });

        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }

        const data = await response.json();
        if (!data.success) {
            throw new Error(data.error || 'Failed to add service provider');
        }
    }

    static async updateServiceProvider(provider: ServiceProvider): Promise<void> {
        const clientId = wsManager.getClientId();
        if (!clientId) {
            throw new Error('Client ID not found');
        }

        const response = await fetch('/api/updateServiceProvider', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'X-Client-Id': clientId,
            },
            body: JSON.stringify({
                ...provider,
                clientId
            })
        });

        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }

        const data = await response.json();
        if (!data.success) {
            throw new Error(data.error || 'Failed to update service provider');
        }
    }

    static async deleteServiceProvider(providerGuid: string): Promise<void> {
        const clientId = wsManager.getClientId();
        if (!clientId) {
            throw new Error('Client ID not found');
        }

        const response = await fetch('/api/deleteServiceProvider', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'X-Client-Id': clientId,
            },
            body: JSON.stringify({
                providerGuid,
                clientId
            })
        });

        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }

        const data = await response.json();
        if (!data.success) {
            throw new Error(data.error || 'Failed to delete service provider');
        }
    }
}