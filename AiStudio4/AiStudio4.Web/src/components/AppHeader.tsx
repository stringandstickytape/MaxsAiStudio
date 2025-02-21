import { Button } from '@/components/ui/button';
import { Menu } from 'lucide-react';
import { cn } from '@/lib/utils';
import { createConversation } from '@/store/conversationSlice';
import { store } from '@/store/store';
import {
    DropdownMenu,
    DropdownMenuContent,
    DropdownMenuItem,
    DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';

interface AppHeaderProps {
    isMobile: boolean;
    selectedModel: string;
    models: string[];
    onToggleSidebar: () => void;
    onModelSelect: (model: string) => void;
}

export function AppHeader({
    isMobile,
    selectedModel,
    models,
    onToggleSidebar,
    onModelSelect,
}: AppHeaderProps) {
    const handleNewChat = () => {
        store.dispatch(createConversation({
            rootMessage: {
                id: `msg_${Date.now()}`,
                content: '',
                source: 'system',
                timestamp: Date.now()
            }
        }));
    };

    return (
        <div className={cn("fixed top-0 left-0 right-0 bg-[#1f2937] border-b border-gray-700 shadow-lg p-4 z-20 flex items-center gap-2", !isMobile && "ml-16")}>
            {isMobile && (
                <Button
                    variant="ghost"
                    size="icon"
                    onClick={onToggleSidebar}
                >
                    <Menu className="h-6 w-6" />
                </Button>
            )}

            <Button onClick={handleNewChat}>New Chat</Button>
            
            <DropdownMenu>
                <DropdownMenuTrigger asChild>
                    <Button variant="outline">{selectedModel}</Button>
                </DropdownMenuTrigger>
                <DropdownMenuContent>
                    {models.length > 0 ? (
                        models.map((model, index) => (
                            <DropdownMenuItem
                                key={index}
                                onSelect={() => onModelSelect(model)}
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
    );
}