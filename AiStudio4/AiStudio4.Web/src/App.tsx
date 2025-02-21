import { useState, useEffect, useRef, useCallback } from "react";
import "./App.css";
import { useMediaQuery } from '@/hooks/use-media-query';
import { Provider } from 'react-redux';
import { store } from './store/store';
import { AppHeader } from './components/AppHeader';
import { ChatContainer } from './components/ChatContainer';
import { InputBar } from './components/input-bar';
import { Sidebar } from './components/Sidebar';
import { useWebSocketState } from './hooks/useWebSocketState';
import { ChatService } from '@/services/ChatService';
import { cn } from '@/lib/utils';

function App() {
    const [models, setModels] = useState<string[]>([]);
    const [selectedModel, setSelectedModel] = useState<string>("Select Model");
    const [isSidebarOpen, setIsSidebarOpen] = useState(false);
    const isMobile = useMediaQuery("(max-width: 768px)");
    const { wsState, liveStreamContent } = useWebSocketState(selectedModel);

    useEffect(() => {
        const loadModels = async () => {
            try {
                const availableModels = await ChatService.fetchModels();
                setModels(availableModels);
            } catch (error) {
                console.error("Error fetching models:", error);
                setModels([]);
                // TODO: Add error handling/user feedback
            }
        };
        loadModels();
    }, []);



    return (
        <Provider store={store}>
            <div className="h-screen flex flex-col overflow-hidden">
                <Sidebar
                    isOpen={isSidebarOpen}
                    wsState={wsState}
                    onToggle={() => setIsSidebarOpen(!isSidebarOpen)}
                />

                <AppHeader
                    isMobile={isMobile}
                    selectedModel={selectedModel}
                    models={models}
                    onToggleSidebar={() => setIsSidebarOpen(!isSidebarOpen)}
                    onModelSelect={setSelectedModel}
                />

                <ChatContainer
                    liveStreamContent={liveStreamContent}
                    isMobile={isMobile}
                />

                <div className={cn("fixed bottom-0 left-0 right-0 bg-background border-t z-20", !isMobile && "ml-16")}>
                    <InputBar selectedModel={selectedModel} />
                </div>
            </div>
        </Provider>
    );
}

export default App;