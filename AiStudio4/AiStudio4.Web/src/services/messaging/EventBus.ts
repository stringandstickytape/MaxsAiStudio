type EventHandler = (data: any) => void;

class EventBus {
    private handlers: { [key: string]: EventHandler[] } = {};

    on(event: string, handler: EventHandler) {
        if (!this.handlers[event]) {
            this.handlers[event] = [];
        }
        this.handlers[event].push(handler);
    }

    off(event: string, handler: EventHandler) {
        if (!this.handlers[event]) return;
        this.handlers[event] = this.handlers[event].filter(h => h !== handler);
    }

    emit(event: string, data: any) {
        if (!this.handlers[event]) return;
        this.handlers[event].forEach(handler => {
            try {
                handler(data);
            } catch (e) {
                console.error(`Error in handler for event "${event}":`, e);
            }
        });
    }
}

export const eventBus = new EventBus();
