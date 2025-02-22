export interface IMessageService {
    connect(): Promise<void>;
    disconnect(): Promise<void>;
    sendMessage(message: string): Promise<void>;
    subscribe(messageType: string, handler: (data: any) => void): void;
    unsubscribe(messageType: string, handler: (data: any) => void): void;
    isConnected(): boolean;
}
