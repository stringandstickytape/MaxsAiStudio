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


    return (
        <div className={cn("fixed top-0 left-0 right-0 bg-[#1f2937] border-b border-gray-700 shadow-lg p-4 z-20 flex items-center gap-2", !isMobile && "ml-16")}>
            <div className="absolute left-4">
                {isMobile && (
                    <Button
                        variant="ghost"
                        size="icon"
                        onClick={onToggleSidebar}
                    >
                        <Menu className="h-6 w-6" />
                    </Button>
                )}
            </div>
            <div className="flex-1 flex justify-center">


            <DropdownMenu>
                <DropdownMenuTrigger asChild>
                    <Button variant="outline" className="bg-[#374151] hover:bg-[#4B5563] text-gray-100 border-gray-600">{selectedModel}</Button>
                </DropdownMenuTrigger>
                <DropdownMenuContent className="bg-[#1f2937] border-gray-700 text-gray-100 max-h-[300px] overflow-y-auto">
                    {models.length > 0 ? (
                        models.map((model, index) => (
                            <DropdownMenuItem className="hover:bg-[#374151] focus:bg-[#374151] cursor-pointer"
                                key={index}
                                onSelect={() => onModelSelect(model)}
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
        </div>
    );
}