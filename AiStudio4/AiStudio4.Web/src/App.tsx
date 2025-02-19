import { useState, useEffect } from "react"
import "./App.css"
import { Button } from "@/components/ui/button"
import * as ts from "typescript"
import { MarkdownPane } from "@/components/markdown-pane"
import { wsManager } from '@/services/websocket/WebSocketManager'
import { useWebSocketMessage } from '@/hooks/useWebSocketMessage'

import {
    DropdownMenu,
    DropdownMenuContent,
    DropdownMenuItem,
    DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"

interface WebSocketState {
    isConnected: boolean;
    clientId: string | null;
    messages: string[];
}

function App() {
    const buttonText: string = `TypeScript Version: ${ts.version}`;
    const [models, setModels] = useState<string[]>([])
    const [selectedModel, setSelectedModel] = useState<string>("Select Model")

    // WebSocket state
    const [wsState, setWsState] = useState<WebSocketState>({
        isConnected: false,
        clientId: null,
        messages: []
    });

    // Handle API calls
    const makeTestCall = async () => {
        try {
            const response = await fetch("/api/test", {
                method: "POST",
                headers: {
                    "Content-Type": "application/json",
                },
            })
            const data = await response.json()
            if (data.success && Array.isArray(data.models)) {
                setModels(data.models)
                setSelectedModel("Select Model")
            }
        } catch (error) {
            console.error("Error fetching test data:", error)
            setModels([])
        }
    }

    // WebSocket message handlers
    const handleClientId = (clientId: string) => {
        console.log('Received client ID message:', clientId); // Add this
        setWsState(prev => ({
            ...prev,
            isConnected: true,
            clientId
        }));
    };

    const handleGenericMessage = (message: any) => {
        setWsState(prev => ({
            ...prev,
            messages: [...prev.messages, JSON.stringify(message)]
        }));
    };

    // Use our custom hook to handle different message types
    useWebSocketMessage('clientId', handleClientId);
    useWebSocketMessage('message', handleGenericMessage);

    // Initialize WebSocket connection when model is selected
    useEffect(() => {
        if (selectedModel !== "Select Model") {
            wsManager.connect();

            return () => {
                wsManager.disconnect();
                setWsState(prev => ({
                    ...prev,
                    isConnected: false,
                    clientId: null
                }));
            };
        }
    }, [selectedModel]);

    // Handler for sending test messages
    const sendTestMessage = () => {
        wsManager.send({
            messageType: 'message',
            content: `Test message from ${wsState.clientId}`
        });
    };

    return (
        <>
            <div className="p-4">
                <div className="mt-4 space-x-4">
                    <Button className="bg-teal-500 hover:bg-teal-600 text-white">
                        {buttonText}
                    </Button>
                    <Button onClick={makeTestCall}>
                        Test Server Call
                    </Button>
                    {wsState.isConnected && (
                        <Button onClick={sendTestMessage}>
                            Send Test Message
                        </Button>
                    )}
                </div>

                <div className="mt-4">
                    <DropdownMenu>
                        <DropdownMenuTrigger asChild>
                            <Button variant="outline">{selectedModel}</Button>
                        </DropdownMenuTrigger>
                        <DropdownMenuContent>
                            {models.length > 0 ? (
                                models.map((model, index) => (
                                    <DropdownMenuItem
                                        key={index}
                                        onSelect={() => setSelectedModel(model)}
                                    >
                                        {model}
                                    </DropdownMenuItem>
                                ))
                            ) : (
                                <DropdownMenuItem disabled>
                                    No models available
                                </DropdownMenuItem>
                            )}
                        </DropdownMenuContent>
                    </DropdownMenu>
                </div>

                {/* Render MarkdownPane for each message */}
                {wsState.messages.map((msg, index) => (
                    <MarkdownPane key={index} message={msg} />
                ))}

                {/* WebSocket Status Panel */}
                <div className="mt-4 p-4 border rounded-lg">
                    <div className="flex items-center space-x-2">
                        <div className={`w-3 h-3 rounded-full ${wsState.isConnected ? 'bg-green-500' : 'bg-red-500'}`} />
                        <span>WebSocket Status: {wsState.isConnected ? 'Connected' : 'Disconnected'}</span>
                    </div>

                    {wsState.clientId && (
                        <div className="mt-2">
                            Client ID: {wsState.clientId}
                        </div>
                    )}

                    <div className="mt-4">
                        <h3 className="font-semibold">Messages:</h3>
                        <div className="mt-2 max-h-40 overflow-y-auto bg-gray-100 rounded p-2">
                            {wsState.messages.map((msg, index) => (
                                <div key={index} className="text-sm py-1">
                                    {msg}
                                </div>
                            ))}
                        </div>
                    </div>
                </div>
            </div>
        </>
    )
}

export default App