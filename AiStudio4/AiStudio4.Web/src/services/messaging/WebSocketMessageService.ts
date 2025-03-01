import { IMessageService } from "./IMessageService";
import { wsManager } from "@/services/websocket/WebSocketManager";
import { eventBus } from "@/services/messaging/EventBus";

export class WebSocketMessageService implements IMessageService {
    private _connected = false;
    private connectionChangeHandlers: ((connected: boolean) => void)[] = [];

    async connect(): Promise<void> {
        wsManager.connect();
        this._connected = wsManager.isConnected();
        this.notifyConnectionChange();
    }

    async disconnect(): Promise<void> {
        wsManager.disconnect();
        this._connected = false;
        this.notifyConnectionChange();
    }

    async sendMessage(message: string): Promise<void> {
        wsManager.send({ messageType: "custom", content: message });
    }

    subscribe(messageType: string, handler: (data: any) => void): void {
        eventBus.on(messageType, handler);
    }

    unsubscribe(messageType: string, handler: (data: any) => void): void {
        eventBus.off(messageType, handler);
    }

    isConnected(): boolean {
        return this._connected;
    }

    onConnectionChange(handler: (connected: boolean) => void): void {
        this.connectionChangeHandlers.push(handler);
        handler(this._connected);
    }

    offConnectionChange(handler: (connected: boolean) => void): void {
        this.connectionChangeHandlers = this.connectionChangeHandlers.filter(h => h !== handler);
    }

    private notifyConnectionChange(): void {
        this.connectionChangeHandlers.forEach(handler => handler(this._connected));
    }
}

export const messageService = new WebSocketMessageService();