
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
import { Settings, GitBranch, ExternalLink } from 'lucide-react';

interface AppHeaderProps {
    isMobile: boolean;
    selectedModel: string;
    secondaryModel: string;
    models: string[];
    onToggleSidebar: () => void;
    onModelSelect: (model: string) => void;
    onSecondaryModelSelect: (model: string) => void;
    onToggleConversationTree: () => void;
    onToggleSettings: () => void;
    onOpenNewWindow: () => void;
    headerRightOffset?: string; // Add new prop for dynamic positioning
}

export function AppHeader({
    isMobile,
    selectedModel,
    secondaryModel,
    models,
    onToggleSidebar,
    onModelSelect,
    onSecondaryModelSelect,
    onToggleConversationTree,
    onToggleSettings,
    onOpenNewWindow,
    headerRightOffset = 'right-4',
}: AppHeaderProps) {
    return (
        <div className="fixed top-0 left-0 right-0 bg-gradient-to-r from-gray-900 to-gray-800 border-b border-gray-700/50 shadow-xl backdrop-blur-sm p-4 z-20 flex items-center gap-2">
            <div className="absolute left-4">
                <Button
                    variant="ghost"
                    size="icon"
                    onClick={onToggleSidebar}
                    className="bg-gray-800/80 hover:bg-gray-700/80 text-gray-100 border-gray-600/50 rounded-lg transition-all duration-200 shadow-md hover:shadow-lg transform hover:-translate-y-0.5"
                >
                    <Menu className="h-6 w-6" />
                </Button>
            </div>
            <div className="flex-1 flex justify-center gap-4">
                {/* Primary AI Model Dropdown */}
                <DropdownMenu>
                    <DropdownMenuTrigger asChild>
                        <Button variant="outline" className="bg-gray-800/80 hover:bg-gray-700/80 text-gray-100 border-gray-600/50 rounded-lg transition-all duration-200 shadow-md hover:shadow-lg transform hover:-translate-y-0.5">
                            {selectedModel === "Select Model" ? "Primary AI: Select Model" : `Primary AI: ${selectedModel}`}
                        </Button>
                    </DropdownMenuTrigger>
                    <DropdownMenuContent className="bg-[#1f2937] border-gray-700 text-gray-100 max-h-[300px] overflow-y-auto">
                        {models.length > 0 ? (
                            models.map((model, index) => (
                                <DropdownMenuItem className="hover:bg-[#374151] focus:bg-[#374151] cursor-pointer"
                                    key={index}
                                    onSelect={() => {
                                        onModelSelect(model);
                                        // Save the selected model as default
                                        import('@/services/ChatService').then(({ ChatService }) => {
                                            ChatService.saveDefaultModel(model).catch(err => 
                                                console.error('Failed to save default model:', err));
                                        });
                                    }}
                                >
                                    {model}
                                </DropdownMenuItem>
                            ))
                        ) : (
                            <DropdownMenuItem disabled className="text-gray-500">
                                No models available
                            </DropdownMenuItem>
                        )}
                    </DropdownMenuContent>
                </DropdownMenu>
                
                {/* Secondary AI Model Dropdown */}
                <DropdownMenu>
                    <DropdownMenuTrigger asChild>
                        <Button variant="outline" className="bg-gray-800/80 hover:bg-gray-700/80 text-gray-100 border-gray-600/50 rounded-lg transition-all duration-200 shadow-md hover:shadow-lg transform hover:-translate-y-0.5">
                            {secondaryModel === "Select Model" ? "Secondary AI: Select Model" : `Secondary AI: ${secondaryModel}`}
                        </Button>
                    </DropdownMenuTrigger>
                    <DropdownMenuContent className="bg-[#1f2937] border-gray-700 text-gray-100 max-h-[300px] overflow-y-auto">
                        {models.length > 0 ? (
                            models.map((model, index) => (
                                <DropdownMenuItem className="hover:bg-[#374151] focus:bg-[#374151] cursor-pointer"
                                    key={index}
                                    onSelect={() => {
                                        onSecondaryModelSelect(model);
                                        // Save the selected secondary model as default
                                        import('@/services/ChatService').then(({ ChatService }) => {
                                            ChatService.saveSecondaryModel(model).catch(err => 
                                                console.error('Failed to save secondary model:', err));
                                        });
                                    }}
                                >
                                    {model}
                                </DropdownMenuItem>
                            ))
                        ) : (
                            <DropdownMenuItem disabled className="text-gray-500">
                                No models available
                            </DropdownMenuItem>
                        )}
                    </DropdownMenuContent>
                </DropdownMenu>
            </div>
            <div className={`absolute ${headerRightOffset} flex items-center space-x-2 transition-all duration-300 z-10`}>
                <Button
                    variant="ghost"
                    size="icon"
                    onClick={onToggleConversationTree}
                    className="bg-gray-800/80 hover:bg-gray-700/80 text-gray-100 border-gray-600/50 rounded-lg transition-all duration-200 shadow-md hover:shadow-lg transform hover:-translate-y-0.5"
                >
                    <GitBranch className="h-5 w-5" />
                </Button>
                <Button
                    variant="ghost"
                    size="icon"
                    onClick={onOpenNewWindow}
                    className="bg-gray-800/80 hover:bg-gray-700/80 text-gray-100 border-gray-600/50 rounded-lg transition-all duration-200 shadow-md hover:shadow-lg transform hover:-translate-y-0.5"
                >
                    <ExternalLink className="h-5 w-5" />
                </Button>
                <Button
                    variant="ghost"
                    size="icon"
                    onClick={onToggleSettings}
                    className="bg-gray-800/80 hover:bg-gray-700/80 text-gray-100 border-gray-600/50 rounded-lg transition-all duration-200 shadow-md hover:shadow-lg transform hover:-translate-y-0.5"
                >
                    <Settings className="h-5 w-5" />
                </Button>
            </div>
        </div>
    );
}