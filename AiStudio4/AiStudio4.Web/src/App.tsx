import { useState, useEffect } from "react"
import "./App.css"
import { Button } from "@/components/ui/button"
import * as ts from "typescript"
import { MarkdownPane } from "@/components/markdown-pane"
import { wsManager } from '@/services/websocket/WebSocketManager'
import { useWebSocketMessage } from '@/hooks/useWebSocketMessage'
import { InputBar } from '@/components/input-bar'

// tools in use: vite, shadcn, tailwind 3, react

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
    const [models, setModels] = useState<string[]>([])
    const [selectedModel, setSelectedModel] = useState<string>("Select Model")

    // WebSocket state
    const [wsState, setWsState] = useState<WebSocketState>({
        isConnected: false,
        clientId: null,
        messages: []
    });

    // Handle API calls
    const handleChatMessage = async (message: string) => {
        const clientId = wsManager.getClientId();
        if (!clientId) {
            console.error('No client ID available');
            return;
        }

        try {
            const response = await fetch('/api/chat', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({
                    clientId: clientId,
                    message: message
                })
            });

            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }

            // Parse the JSON response
            const data = await response.json();
            console.log('Response data:', data);
            return data; // Return the response data if needed

        } catch (error) {
            console.error('Error sending chat message:', error);
        }
    };

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

    return (
        <div className="flex flex-col min-h-screen">
            <div className="fixed top-0 left-0 right-0 bg-white border-b shadow-sm p-4 z-10">
                <div className="space-x-4">
                    <Button onClick={makeTestCall}>
                        Test Server Call
                    </Button>
                </div>
            </div>
            <div className="fixed top-16 left-0 right-0 bg-white border-b shadow-sm p-4 z-10">
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
            <div className="flex-1 p-4 mt-32 mb-[30vh]">




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
            <InputBar onSendMessage={handleChatMessage} />
        </div>
    )
}

export default App