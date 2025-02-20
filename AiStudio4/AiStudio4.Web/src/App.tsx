import { useState, useEffect } from "react"
import "./App.css"
import { Button } from "@/components/ui/button"
import { Menu } from "lucide-react"
import * as ts from "typescript"
import { MarkdownPane } from "@/components/markdown-pane"
import { wsManager, LiveChatStreamToken } from '@/services/websocket/WebSocketManager'
import { useWebSocketMessage } from '@/hooks/useWebSocketMessage'
import { InputBar } from '@/components/input-bar'
import { Provider } from 'react-redux'
import { store } from './store/store'
import { ConversationView } from './components/ConversationView'
import { CachedConversationList } from './components/CachedConversationList'

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
    streamTokens: LiveChatStreamToken[];
}

function App() {
    const [models, setModels] = useState<string[]>([])
    const [selectedModel, setSelectedModel] = useState<string>("Select Model")
    const [liveStreamContent, setLiveStreamContent] = useState('');
    const [isSidebarOpen, setIsSidebarOpen] = useState(false);

    // WebSocket state
    const [wsState, setWsState] = useState<WebSocketState>({
        isConnected: false,
        clientId: null,
        messages: [],
        streamTokens: []
    });

    // Handle API calls
    const handleChatMessage = async (message: string) => {
        const clientId = wsManager.getClientId();
        const activeConversationId = store.getState().conversations.activeConversationId;

        if (!clientId) {
            console.error('No client ID available');
            return;
        }

        try {
            const response = await fetch('/api/chat', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'X-Client-Id': clientId,
                },
                body: JSON.stringify({
                    clientId: clientId,
                    message: message,
                    conversationId: activeConversationId
                })
            });

            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }

            const data = await response.json();
            console.log('Response data:', data);
            return data;

        } catch (error) {
            console.error('Error sending chat message:', error);
        }
    };

    const fetchModels = async () => {
        try {
            const response = await fetch("/api/getConfig", {
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
        console.log('Received client ID message:', clientId);
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

    const handleNewStreamToken = (token: string) => {
        setLiveStreamContent(wsManager.getStreamTokens());
        setWsState(prev => ({
            ...prev,
            streamTokens: [...prev.streamTokens, { token, timestamp: Date.now() }]
        }));
    };

    const handleEndStream = () => {
        setLiveStreamContent('');
        console.log('Stream ended - cleared live stream content');
    };

    // Use our custom hook to handle different message types
    useWebSocketMessage('clientId', handleClientId);
    useWebSocketMessage('message', handleGenericMessage);
    useWebSocketMessage('newStreamToken', handleNewStreamToken);
    useWebSocketMessage('endstream', handleEndStream);

    
    // Fetch models on component mount
    useEffect(() => {
        fetchModels();
    }, []);

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
                setLiveStreamContent('');
            };
        }
    }, [selectedModel]);

    return (
        <Provider store={store}>
            <div className="flex flex-col min-h-screen relative">
                {/* Sidebar */}
                <div className={`fixed left-0 top-0 h-full w-80 bg-[#1f2937] transform transition-transform duration-300 ease-in-out ${isSidebarOpen ? 'translate-x-0' : '-translate-x-full'} z-50`}>
                    {/* Sidebar content goes here */}
                    <div className="flex flex-col h-full">
                        <div className="p-4 flex-grow overflow-hidden flex flex-col">
                            <h2 className="text-white text-xl font-bold mb-4">Sidebar</h2>
                            <div className="overflow-y-auto flex-1">
                                <CachedConversationList />
                            </div>
                        </div>
                        {/* WebSocket Status Panel */}
                        <div className="p-3 border-t border-gray-700 text-sm">
                            <div className="flex items-center space-x-2">
                                <div className={`w-2 h-2 rounded-full ${wsState.isConnected ? 'bg-green-500' : 'bg-red-500'}`} />
                                <span className="text-white text-xs">WebSocket: {wsState.isConnected ? 'Connected' : 'Disconnected'}</span>
                            </div>
                            {wsState.clientId && (
                                <div className="mt-1 text-gray-400 text-xs truncate">
                                    ID: {wsState.clientId}
                                </div>
                            )}
                        </div>
                    </div>
                </div>

                {/* Menu Toggle Button */}
                <button
                    onClick={() => setIsSidebarOpen(!isSidebarOpen)}
                    className="fixed top-5 left-5 z-50 p-2 rounded-full bg-[#1f2937] hover:bg-[#374151] transition-colors duration-200"
                >
                    <Menu className="w-6 h-6 text-white" />
                </button>

            <div className="fixed top-0 left-0 right-0 bg-[#1f2937] border-b border-gray-700 shadow-lg p-4 z-10">
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

            <div className="flex-1 p-4 mt-4 mb-[30vh]">
                {/* Conversation View */}
                <ConversationView />
                
                {/* Live Stream Markdown Pane */}
                {liveStreamContent && (
                    <MarkdownPane message={JSON.stringify(liveStreamContent)} />
                )}
            </div>
            <InputBar onSendMessage={handleChatMessage} />
            </div>
        </Provider>
    );
}

export default App;