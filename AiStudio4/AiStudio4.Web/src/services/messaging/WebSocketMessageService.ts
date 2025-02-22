import { IMessageService } from "./IMessageService";
import { wsManager } from "../../services/websocket/WebSocketManager";
import { eventBus } from "./EventBus";

export class WebSocketMessageService implements IMessageService {
    private _connected: boolean = false;

    async connect(): Promise<void> {
        wsManager.connect();
        // _connected will be updated via connectionStatus events emitted by wsManager
        // Optionally, you could poll or wait until a specific event is received.
        this._connected = wsManager.isConnected();
    }

    async disconnect(): Promise<void> {
        wsManager.disconnect();
        this._connected = false;
    }

    async sendMessage(message: string): Promise<void> {
        // Wrap message inside an object; the messageType here is set to 'custom' by default.
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
}

export const messageService = new WebSocketMessageService();