import { webSocketService } from './websocket/WebSocketService';
import { ServiceProvider, Model } from '@/types/settings';

export class SettingsService {
    // Removed normalizeModel as JsonConvert handles serialization/deserialization

    // Removed normalizeProvider as JsonConvert handles serialization/deserialization

    private static async fetchData<T>(endpoint: string, clientId: string, body?: any): Promise<T> {
        const response = await fetch(endpoint, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'X-Client-Id': clientId,
            },
            body: JSON.stringify(body ? { ...body, clientId } : { clientId })
        });

        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }

        const data = await response.json();
        if (!data.success) {
            throw new Error(data.error || `Failed to fetch data from ${endpoint}`);
        }

        return data;
    }

    private static async executeAction(endpoint: string, data: any): Promise<void> {
        const clientId = webSocketService.getClientId();
        if (!clientId) {
            throw new Error('Client ID not found');
        }

        const responseData = await this.fetchData(endpoint, clientId, data);

        if (!responseData) {
            throw new Error(`Failed to execute action on ${endpoint}`);
        }
    }

    static async getModels(): Promise<Model[]> {
        const clientId = webSocketService.getClientId();
        if (!clientId) {
            throw new Error('Client ID not found');
        }

        const data = await this.fetchData<{ models: any[] }>('/api/getModels', clientId);
        return data.models;
    }

    static async getServiceProviders(): Promise<ServiceProvider[]> {
        const clientId = webSocketService.getClientId();
        if (!clientId) {
            throw new Error('Client ID not found');
        }

        const data = await this.fetchData<{ providers: any[] }>('/api/getServiceProviders', clientId);
        return data.providers;
    }

    static async addModel(model: Omit<Model, 'guid'>): Promise<void> {
        await this.executeAction('/api/addModel', model);
    }

    static async updateModel(model: Model): Promise<void> {
        await this.executeAction('/api/updateModel', model);
    }

    static async deleteModel(modelGuid: string): Promise<void> {
        await this.executeAction('/api/deleteModel', { modelGuid });
    }

    static async addServiceProvider(provider: Omit<ServiceProvider, 'guid'>): Promise<void> {
        await this.executeAction('/api/addServiceProvider', provider);
    }

    static async updateServiceProvider(provider: ServiceProvider): Promise<void> {
        await this.executeAction('/api/updateServiceProvider', provider);
    }

    static async deleteServiceProvider(providerGuid: string): Promise<void> {
        await this.executeAction('/api/deleteServiceProvider', { providerGuid });
    }
}