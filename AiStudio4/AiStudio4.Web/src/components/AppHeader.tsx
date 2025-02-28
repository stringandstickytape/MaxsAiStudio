import { Button } from '@/components/ui/button';
import { Menu, Settings, GitBranch, ExternalLink } from 'lucide-react';
import { cn } from '@/lib/utils';
import { ModelSelector } from '@/components/ModelSelector'; // Import the new component

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
                {/* Primary AI Model Selector */}
                <ModelSelector
                    label="Primary AI"
                    selectedModel={selectedModel}
                    models={models}
                    modelType="primary"
                    onModelSelect={onModelSelect}
                />

                {/* Secondary AI Model Selector */}
                <ModelSelector
                    label="Secondary AI"
                    selectedModel={secondaryModel}
                    models={models}
                    modelType="secondary"
                    onModelSelect={onSecondaryModelSelect}
                />
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